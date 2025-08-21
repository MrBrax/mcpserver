using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Editor;
using Sandbox;
using FileSystem = Editor.FileSystem;
using Braxnet.Commands;

namespace Braxnet;

public class MCPSession
{
	public string SessionId { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool Initialized { get; set; }
	public Stream SseStream { get; set; }
	public bool SseConnected { get; set; }
	public CancellationTokenSource SseCancellationTokenSource { get; set; }
	public Queue<JsonRpcNotification> PendingNotifications { get; set; } = new();
}

/// <summary>
/// Model Context Protocol (MCP) Server implementation for S&amp;Box.
/// 
/// This server implements the MCP lifecycle including:
/// - Proper initialization handshake with protocol version negotiation
/// - Session management with lifecycle validation
/// - Notification handling (including the required notifications/initialized)
/// - Request/response processing with error handling
/// - Tools and commands registry
/// 
/// The server follows MCP specification version 2025-06-18.
/// </summary>
public class MCPServer
{
	[ConVar( "mcp.port", ConVarFlags.Saved )]
	public static int Port { get; set; } = 8098;

	private static HttpListener _httpListener;
	private static CancellationTokenSource _cancellationTokenSource;

	// private static readonly ConcurrentDictionary<string, string> _sessions = new();
	// private static readonly ConcurrentDictionary<string, bool> _sessionInitialized = new();

	private static readonly ConcurrentDictionary<string, MCPSession> Sessions = new();

	// Thread-local storage for the current initialize session ID
	private static readonly ThreadLocal<string> CurrentInitializeSessionId = new();

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	};

	// Public accessor for commands to use
	public static JsonSerializerOptions JsonOptions => _jsonOptions;

	// Method to set the current initialize session ID
	public static void SetCurrentInitializeSessionId( string sessionId )
	{
		CurrentInitializeSessionId.Value = sessionId;
	}

	// Method to get and clear the current initialize session ID
	private static string GetAndClearInitializeSessionId()
	{
		var sessionId = CurrentInitializeSessionId.Value;
		CurrentInitializeSessionId.Value = null;
		return sessionId;
	}

	[Menu( "Editor", "MCP/Start MCP Server" )]
	public static void StartMCPServer()
	{
		if ( _httpListener != null && _httpListener.IsListening )
		{
			Log.Info( "MCP Server is already running" );
			return;
		}

		try
		{
			// Initialize command registry
			MCPCommandRegistry.Initialize();

			_httpListener = new HttpListener();
			_httpListener.Prefixes.Add( $"http://127.0.0.1:{Port}/" );
			_httpListener.Start();

			_cancellationTokenSource = new CancellationTokenSource();

			Task.Run( () => HandleRequests( _cancellationTokenSource.Token ) );

			Log.Info( $"MCP Server started on port {Port}" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Failed to start MCP Server: {ex.Message}" );
		}
	}

	[Menu( "Editor", "MCP/Stop MCP Server" )]
	public static void StopMCPServer()
	{
		try
		{
			// Cancel all SSE connections
			foreach ( var session in Sessions.Values )
			{
				if ( session.SseConnected )
				{
					session.SseCancellationTokenSource?.Cancel();
				}
			}

			_cancellationTokenSource?.Cancel();
			_httpListener?.Stop();
			_httpListener?.Close();
			_httpListener = null;
			// _sessions.Clear();
			// _sessionInitialized.Clear();
			Sessions.Clear();

			Log.Info( "MCP Server stopped" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error stopping MCP Server: {ex.Message}" );
		}
	}

	[Menu( "Editor", "MCP/Server Status" )]
	public static void ShowServerStatus()
	{
		var isRunning = _httpListener != null && _httpListener.IsListening;
		var activeSessionCount = GetActiveSessionCount();
		var initializedSessionCount = GetInitializedSessionCount();
		var sseConnectedCount = GetSseConnectedSessionCount();

		Log.Info( $"MCP Server Status:" );
		Log.Info( $"  Running: {isRunning}" );
		Log.Info( $"  Port: {Port}" );
		Log.Info( $"  Active Sessions: {activeSessionCount}" );
		Log.Info( $"  Initialized Sessions: {initializedSessionCount}" );
		Log.Info( $"  SSE Connected Sessions: {sseConnectedCount}" );

		if ( activeSessionCount > 0 )
		{
			Log.Info( $"  Sessions:" );
			foreach ( var session in Sessions )
			{
				// var initialized = IsSessionInitialized( session.Key );
				// Log.Info(
				// 	$"    {session.Key}: {(initialized ? "Initialized" : "Pending")} (Created: {session.Value})" );
				var sseStatus = session.Value.SseConnected ? "SSE Connected" : "No SSE";
				var pendingCount = session.Value.PendingNotifications?.Count ?? 0;
				Log.Info(
					$"    {session.Key}: {(session.Value.Initialized ? "Initialized" : "Pending")} | {sseStatus} | {pendingCount} pending notifications (Created: {session.Value.CreatedAt})" );
			}
		}
	}

	[Menu( "Editor", "MCP/Cleanup Expired Sessions" )]
	public static void MenuCleanupExpiredSessions()
	{
		CleanupExpiredSessions();
	}

	[Menu( "Editor", "MCP/Test Notifications/Send Resource List Changed" )]
	public static async void TestSendResourceListChanged()
	{
		await SendResourceListChangedNotification();
		Log.Info( "Sent resource list changed notification to all sessions" );
	}

	[Menu( "Editor", "MCP/Test Notifications/Send Tool List Changed" )]
	public static async void TestSendToolListChanged()
	{
		await SendToolListChangedNotification();
		Log.Info( "Sent tool list changed notification to all sessions" );
	}

	[Menu( "Editor", "MCP/Test Notifications/Send Progress Notification" )]
	public static async void TestSendProgressNotification()
	{
		await SendProgressNotification( "test-progress-token", 0.5, 1.0 );
		Log.Info( "Sent progress notification to all sessions" );
	}

	private static async Task HandleRequests( CancellationToken cancellationToken )
	{
		while ( !cancellationToken.IsCancellationRequested && _httpListener != null && _httpListener.IsListening )
		{
			try
			{
				var context = await _httpListener.GetContextAsync();
				_ = Task.Run( () => ProcessRequest( context ), cancellationToken );
			}
			catch ( ObjectDisposedException )
			{
				break;
			}
			catch ( Exception ex )
			{
				Log.Error( $"Error handling HTTP request: {ex.Message}" );
			}
		}
	}

	private static async Task ProcessRequest( HttpListenerContext context )
	{
		var request = context.Request;
		var response = context.Response;

		// Set CORS headers
		response.Headers.Add( "Access-Control-Allow-Origin", "*" );
		response.Headers.Add( "Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS" );
		response.Headers.Add( "Access-Control-Allow-Headers",
			"Content-Type, Accept, MCP-Protocol-Version, MCP-Session-Id" );

		if ( request.HttpMethod == "OPTIONS" )
		{
			Log.Info( "Received CORS preflight request" );
			response.StatusCode = 200;
			response.Close();
			return;
		}

		try
		{
			// Validate Origin header for security
			var origin = request.Headers["Origin"];
			if ( !string.IsNullOrEmpty( origin ) && !IsValidOrigin( origin ) )
			{
				Log.Warning( $"Invalid origin: {origin}" );
				response.StatusCode = 403;
				response.Close();
				return;
			}

			/*foreach ( var header in request.Headers.AllKeys )
			{
				Log.Info( $"Request Header: {header} = {request.Headers[header]}" );
			}*/

			var sessionId = request.Headers["MCP-Session-Id"];
			var protocolVersion = request.Headers["MCP-Protocol-Version"] ?? "2025-03-26";

			if ( request.HttpMethod == "POST" )
			{
				await HandlePostRequest( request, response, sessionId, protocolVersion );
			}
			else if ( request.HttpMethod == "GET" )
			{
				await HandleGetRequest( request, response, sessionId, protocolVersion );
			}
			else if ( request.HttpMethod == "DELETE" )
			{
				await HandleDeleteRequest( request, response, sessionId );
			}
			else
			{
				Log.Warning( $"Unsupported HTTP method: {request.HttpMethod}" );
				response.StatusCode = 405;
				response.Close();
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error processing request: {ex.Message}" );
			Log.Error( ex );
			await SendErrorResponse( response, -32603, "Internal error", ex.Message );
		}
	}

	private static bool IsValidOrigin( string origin )
	{
		// Allow localhost origins for development
		return origin.StartsWith( "http://localhost:" ) ||
		       origin.StartsWith( "http://127.0.0.1:" ) ||
		       origin.StartsWith( "https://localhost:" ) ||
		       origin.StartsWith( "https://127.0.0.1:" );
	}

	private static async Task HandlePostRequest( HttpListenerRequest request, HttpListenerResponse response,
		string sessionId, string protocolVersion )
	{
		using var reader = new StreamReader( request.InputStream, Encoding.UTF8 );
		var body = await reader.ReadToEndAsync();

		JsonRpcRequest jsonRpcRequest;
		try
		{
			jsonRpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>( body, _jsonOptions );
		}
		catch ( JsonException ex )
		{
			await SendErrorResponse( response, -32700, "Parse error", ex.Message );
			return;
		}

		if ( jsonRpcRequest == null )
		{
			await SendErrorResponse( response, -32600, "Invalid Request" );
			return;
		}

		// Handle different message types
		if ( jsonRpcRequest.Id != null )
		{
			Log.Info( $"Received JSON-RPC request: {jsonRpcRequest.Method} (ID: {jsonRpcRequest.Id})" );

			// This is a request
			await HandleJsonRpcRequest( jsonRpcRequest, response, sessionId, protocolVersion );
		}
		else
		{
			// This is a notification
			Log.Info( $"Received notification: {jsonRpcRequest.Method}" );

			// Handle notification through the registry
			var handled = await MCPCommandRegistry.HandleNotificationAsync(
				jsonRpcRequest.Method, jsonRpcRequest, sessionId, protocolVersion );

			if ( !handled )
			{
				Log.Warning( $"No handler found for notification: {jsonRpcRequest.Method}" );
			}

			response.StatusCode = 202; // Accepted
			response.Close();
		}
	}

	private static async Task HandleGetRequest( HttpListenerRequest request, HttpListenerResponse response,
		string sessionId, string protocolVersion )
	{
		// Handle SSE streams for server-initiated messages
		var acceptHeader = request.Headers["Accept"];
		if ( acceptHeader?.Contains( "text/event-stream" ) == true )
		{
			// EventSource doesn't support custom headers, so also check URL parameters for session ID
			if ( string.IsNullOrEmpty( sessionId ) )
			{
				var queryString = request.Url?.Query;
				if ( !string.IsNullOrEmpty( queryString ) )
				{
					// Simple query parameter parsing for session ID
					var pairs = queryString.TrimStart( '?' ).Split( '&' );
					foreach ( var pair in pairs )
					{
						var parts = pair.Split( '=' );
						if ( parts.Length == 2 )
						{
							var key = Uri.UnescapeDataString( parts[0] );
							if ( key == "session" || key == "sessionId" || key == "MCP-Session-Id" )
							{
								sessionId = Uri.UnescapeDataString( parts[1] );
								break;
							}
						}
					}
				}
			}

			// Log.Info( $"Starting SSE stream for session {sessionId}" );
			await StartServerSentEvents( response, sessionId );
		}
		else
		{
			Log.Warning( $"Unsupported Accept header: {acceptHeader}" );
			response.StatusCode = 405;
			response.Close();
		}
	}

	private static async Task HandleDeleteRequest( HttpListenerRequest request, HttpListenerResponse response,
		string sessionId )
	{
		if ( !string.IsNullOrEmpty( sessionId ) && Sessions.TryGetValue( sessionId, out var session ) )
		{
			// Clean up SSE connection if active
			if ( session.SseConnected )
			{
				session.SseCancellationTokenSource?.Cancel();
				session.SseConnected = false;
			}

			Sessions.TryRemove( sessionId, out _ );
			// _sessionInitialized.TryRemove( sessionId, out _ );
			response.StatusCode = 200;
		}
		else
		{
			response.StatusCode = 404;
		}

		Log.Info( $"Session {sessionId} deleted" );
		response.Close();

		await Task.CompletedTask;
	}

	private static async Task StartServerSentEvents( HttpListenerResponse response, string sessionId )
	{
		response.ContentType = "text/event-stream";
		response.Headers.Add( "Cache-Control", "no-cache" );
		response.Headers.Add( "Connection", "keep-alive" );
		response.Headers.Add( "Access-Control-Allow-Origin", "*" );

		if ( string.IsNullOrEmpty( sessionId ) )
		{
			Log.Warning( "Cannot start SSE stream without session ID" );
			response.StatusCode = 400;
			response.Close();
			return;
		}

		if ( !Sessions.TryGetValue( sessionId, out var session ) )
		{
			Log.Warning( $"Session {sessionId} not found for SSE stream" );
			response.StatusCode = 404;
			response.Close();
			return;
		}

		Log.Info( $"Starting SSE stream for session {sessionId}" );

		try
		{
			var stream = response.OutputStream;

			// Update session with SSE connection info
			session.SseStream = stream;
			session.SseConnected = true;
			session.SseCancellationTokenSource = new CancellationTokenSource();

			// Send initial connection event
			await SendSseEvent( stream, "connected", $"SSE stream established for session {sessionId}" );

			// Send any pending notifications
			while ( session.PendingNotifications.Count > 0 )
			{
				var notification = session.PendingNotifications.Dequeue();
				await SendSseNotification( stream, notification );
			}

			// Keep connection alive and handle incoming notifications
			var cancellationToken = session.SseCancellationTokenSource.Token;

			try
			{
				// Send periodic heartbeat to keep connection alive
				while ( !cancellationToken.IsCancellationRequested && session.SseConnected )
				{
					await Task.Delay( 30000, cancellationToken ); // 30 second heartbeat

					if ( session.SseConnected && stream.CanWrite )
					{
						await SendSseEvent( stream, "heartbeat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() );
					}
					else
					{
						break;
					}
				}
			}
			catch ( OperationCanceledException )
			{
				Log.Info( $"SSE stream for session {sessionId} was cancelled" );
			}
			catch ( Exception ex )
			{
				Log.Error( $"Error in SSE heartbeat loop for session {sessionId}: {ex.Message}" );
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error in SSE stream for session {sessionId}: {ex.Message}" );
		}
		finally
		{
			// Clean up session SSE state
			if ( Sessions.TryGetValue( sessionId, out var cleanupSession ) )
			{
				cleanupSession.SseConnected = false;
				cleanupSession.SseStream = null;
				cleanupSession.SseCancellationTokenSource?.Dispose();
				cleanupSession.SseCancellationTokenSource = null;
			}

			Log.Info( $"SSE stream closed for session {sessionId}" );
		}
	}

	/// <summary>
	/// Send a Server-Sent Event message
	/// </summary>
	private static async Task SendSseEvent( Stream stream, string eventType, string data )
	{
		try
		{
			if ( !stream.CanWrite )
				return;

			var message = $"event: {eventType}\ndata: {data}\n\n";
			var buffer = Encoding.UTF8.GetBytes( message );
			await stream.WriteAsync( buffer, 0, buffer.Length );
			await stream.FlushAsync();
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error sending SSE event '{eventType}': {ex.Message}" );
		}
	}

	/// <summary>
	/// Send a JSON-RPC notification via SSE
	/// </summary>
	private static async Task SendSseNotification( Stream stream, JsonRpcNotification notification )
	{
		try
		{
			if ( !stream.CanWrite )
				return;

			var json = JsonSerializer.Serialize( notification, _jsonOptions );
			var message = $"event: notification\ndata: {json}\n\n";
			var buffer = Encoding.UTF8.GetBytes( message );
			await stream.WriteAsync( buffer, 0, buffer.Length );
			await stream.FlushAsync();
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error sending SSE notification '{notification.Method}': {ex.Message}" );
		}
	}

	/// <summary>
	/// Queue a notification to be sent to a specific session
	/// </summary>
	public static async Task QueueNotificationForSession( string sessionId, JsonRpcNotification notification )
	{
		if ( string.IsNullOrEmpty( sessionId ) || notification == null )
			return;

		if ( !Sessions.TryGetValue( sessionId, out var session ) )
		{
			Log.Warning( $"Cannot queue notification for unknown session: {sessionId}" );
			return;
		}

		// If SSE is connected, send immediately
		if ( session.SseConnected && session.SseStream != null )
		{
			await SendSseNotification( session.SseStream, notification );
		}
		else
		{
			// Queue for later delivery
			session.PendingNotifications.Enqueue( notification );
			Log.Info( $"Queued notification '{notification.Method}' for session {sessionId}" );
		}
	}

	/// <summary>
	/// Broadcast a notification to all connected sessions
	/// </summary>
	public static async Task BroadcastNotification( JsonRpcNotification notification )
	{
		if ( notification == null )
			return;

		var tasks = new List<Task>();

		foreach ( var session in Sessions.Values )
		{
			if ( session.SseConnected )
			{
				tasks.Add( QueueNotificationForSession( session.SessionId, notification ) );
			}
			else
			{
				Log.Warning( $"Session {session.SessionId} is not connected for SSE" );
			}
		}

		if ( tasks.Count > 0 )
		{
			await Task.WhenAll( tasks );
			Log.Info( $"Broadcasted notification '{notification.Method}' to {tasks.Count} sessions" );
		}
	}

	/// <summary>
	/// Send a server-initiated notification for resources/list changed
	/// </summary>
	public static async Task SendResourceListChangedNotification( string sessionId = null )
	{
		var notification = new JsonRpcNotification { Method = "notifications/resources/list_changed" };

		if ( !string.IsNullOrEmpty( sessionId ) )
		{
			await QueueNotificationForSession( sessionId, notification );
		}
		else
		{
			await BroadcastNotification( notification );
		}
	}

	/// <summary>
	/// Send a server-initiated notification for tools/list changed
	/// </summary>
	public static async Task SendToolListChangedNotification( string sessionId = null )
	{
		var notification = new JsonRpcNotification { Method = "notifications/tools/list_changed" };

		if ( !string.IsNullOrEmpty( sessionId ) )
		{
			await QueueNotificationForSession( sessionId, notification );
		}
		else
		{
			await BroadcastNotification( notification );
		}
	}

	/// <summary>
	/// Send a server-initiated notification for prompts/list changed
	/// </summary>
	public static async Task SendPromptListChangedNotification( string sessionId = null )
	{
		var notification = new JsonRpcNotification { Method = "notifications/prompts/list_changed" };

		if ( !string.IsNullOrEmpty( sessionId ) )
		{
			await QueueNotificationForSession( sessionId, notification );
		}
		else
		{
			await BroadcastNotification( notification );
		}
	}

	/// <summary>
	/// Send a server-initiated notification for resource updated
	/// </summary>
	public static async Task SendResourceUpdatedNotification( string uri, string sessionId = null )
	{
		var notification = new JsonRpcNotification { Method = "notifications/resources/updated", Params = new { uri } };

		if ( !string.IsNullOrEmpty( sessionId ) )
		{
			await QueueNotificationForSession( sessionId, notification );
		}
		else
		{
			await BroadcastNotification( notification );
		}
	}

	/// <summary>
	/// Send a progress notification
	/// </summary>
	public static async Task SendProgressNotification( object progressToken, double progress, double? total = null,
		string sessionId = null )
	{
		var notification = new JsonRpcNotification
		{
			Method = "notifications/progress", Params = new { progressToken, progress, total }
		};

		if ( !string.IsNullOrEmpty( sessionId ) )
		{
			await QueueNotificationForSession( sessionId, notification );
		}
		else
		{
			await BroadcastNotification( notification );
		}
	}

	private static async Task HandleJsonRpcRequest( JsonRpcRequest request, HttpListenerResponse response,
		string sessionId, string protocolVersion )
	{
		try
		{
			// Check if response is still valid
			/*if ( response.OutputStream == null )
			{
				Log.Warning( $"Response stream is null for request {request.Method}" );
				return;
			}*/

			object result = null;
			JsonRpcError error = null;

			// Log.Info( $"Handling JSON-RPC request: {request.Method} (ID: {request.Id})" );

			// Validate lifecycle - only allow initialize and ping before initialization is complete
			if ( !string.IsNullOrEmpty( sessionId ) && !IsSessionInitialized( sessionId ) )
			{
				if ( request.Method != "initialize" && request.Method != "ping" )
				{
					Log.Warning( $"Request {request.Method} received before session {sessionId} was initialized" );
					error = new JsonRpcError
					{
						Code = -32002,
						Message = "Server not initialized",
						Data =
							"Must send initialize request and receive initialized notification before other requests"
					};
				}
			}

			if ( error == null )
			{
				// Use command registry instead of switch statement
				result = await MCPCommandRegistry.ExecuteCommandAsync( request.Method, request, sessionId,
					protocolVersion );

				// Log.Info( $"Command execution result for {request.Method}: {result}" );

				if ( result == null )
				{
					// Log.Warning( $"No result returned for command {request.Method}" );
					error = new JsonRpcError { Code = -32601, Message = "Method not found", Data = request.Method };
				}
			}

			Log.Info(
				$"JSON-RPC request {request.Method} processed successfully, generate response with result type {result?.GetType().Name ?? "null"}" );

			var jsonRpcResponse = new JsonRpcResponse { Id = request.Id, Result = result, Error = error };

			// If this is an initialize request, set the Mcp-Session-Id header with the new session ID
			if ( request.Method == "initialize" && error == null )
			{
				var initializeSessionId = GetAndClearInitializeSessionId();
				if ( !string.IsNullOrEmpty( initializeSessionId ) )
				{
					response.Headers.Add( "Mcp-Session-Id", initializeSessionId );
					Log.Info( $"Set Mcp-Session-Id header to {initializeSessionId} for initialize response" );
				}
			}

			// Log.Info( $"Generated JSON-RPC response: {jsonRpcResponse}" );
			// Log.Info( $"Id: {jsonRpcResponse.Id}, Result: {jsonRpcResponse.Result}, Error: {jsonRpcResponse.Error}" );

			// Log.Info(
			// 	$"JSON-RPC response for {request.Method}: {JsonSerializer.Serialize( jsonRpcResponse, _jsonOptions )}" );

			await SendJsonResponse( response, jsonRpcResponse );
		}
		catch ( ObjectDisposedException )
		{
			Log.Warning( $"Response was disposed while handling request {request.Method}" );
		}
		catch ( InvalidOperationException ex )
		{
			Log.Error( $"Invalid operation while handling JSON-RPC request {request.Method}: {ex.Message}" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error handling JSON-RPC request {request.Method}: {ex.Message}" );
			try
			{
				await SendErrorResponse( response, -32603, "Internal error", ex.Message, request.Id );
			}
			catch ( Exception errorEx )
			{
				Log.Warning( $"Could not send error response for {request.Method}: {errorEx.Message}" );
			}
		}
	}

	// Helper methods for commands
	public static void RegisterSession( string sessionId )
	{
		// _sessions[sessionId] = DateTime.UtcNow.ToString();
		// _sessionInitialized[sessionId] = false;
		Sessions[sessionId] = new MCPSession
		{
			SessionId = sessionId,
			CreatedAt = DateTime.UtcNow,
			Initialized = false,
			SseConnected = false,
			PendingNotifications = new Queue<JsonRpcNotification>()
		};
	}

	public static void MarkSessionInitialized( string sessionId )
	{
		if ( string.IsNullOrEmpty( sessionId ) )
		{
			Log.Error( "Cannot mark session as initialized: sessionId is null or empty" );
			return;
		}

		// _sessionInitialized[sessionId] = true;

		if ( Sessions.TryGetValue( sessionId, out var session ) )
		{
			session.Initialized = true;
		}
		else
		{
			Log.Warning( $"Session {sessionId} not found when marking as initialized" );
			return;
		}

		Log.Info( $"Session {sessionId} marked as initialized" );
	}

	public static bool IsSessionInitialized( string sessionId )
	{
		// return _sessionInitialized.TryGetValue( sessionId, out var initialized ) && initialized;
		if ( Sessions.TryGetValue( sessionId, out var session ) )
		{
			return session.Initialized;
		}

		Log.Warning( $"Session {sessionId} not found when checking initialization" );
		return false;
	}

	public static void CleanupExpiredSessions()
	{
		/*// Optional: Add logic to clean up old sessions
		var expiredSessions = _sessions.Where( kvp =>
		{
			if ( DateTime.TryParse( kvp.Value, out var created ) )
			{
				return DateTime.UtcNow.Subtract( created ).TotalHours > 24; // 24 hour expiry
			}

			return true; // Remove sessions with invalid timestamps
		} ).Select( kvp => kvp.Key ).ToList();

		foreach ( var sessionId in expiredSessions )
		{
			_sessions.TryRemove( sessionId, out _ );
			_sessionInitialized.TryRemove( sessionId, out _ );
			Log.Info( $"Cleaned up expired session: {sessionId}" );
		}*/

		var expiredSessions = Sessions.Where( kvp => DateTime.UtcNow.Subtract( kvp.Value.CreatedAt ).TotalHours > 24 )
			.Select( kvp => kvp.Key ).ToList();

		foreach ( var sessionId in expiredSessions )
		{
			if ( Sessions.TryRemove( sessionId, out var removedSession ) )
			{
				// Clean up SSE connection if active
				if ( removedSession.SseConnected )
				{
					removedSession.SseCancellationTokenSource?.Cancel();
				}

				Log.Info( $"Cleaned up expired session: {sessionId} (Created at {removedSession.CreatedAt})" );
			}
			else
			{
				Log.Warning( $"Failed to remove expired session: {sessionId}" );
			}
		}

		Log.Info( $"Cleaned up {expiredSessions.Count} expired sessions" );
	}

	public static int GetActiveSessionCount() => Sessions.Count;
	public static int GetInitializedSessionCount() => Sessions.Count( kvp => kvp.Value.Initialized );
	public static int GetSseConnectedSessionCount() => Sessions.Count( kvp => kvp.Value.SseConnected );

	public static string GetMimeType( string filePath )
	{
		var extension = Path.GetExtension( filePath ).ToLowerInvariant();
		return extension switch
		{
			".cs" => "text/x-csharp",
			".json" => "application/json",
			".txt" => "text/plain",
			".md" => "text/markdown",
			".xml" => "application/xml",
			".html" => "text/html",
			".css" => "text/css",
			".js" => "text/javascript",
			".ts" => "text/typescript",
			_ => "text/plain"
		};
	}

	private static async Task SendJsonResponse( HttpListenerResponse response, JsonRpcResponse jsonRpcResponse )
	{
		Log.Info( $"Sending JSON-RPC response for request ID {jsonRpcResponse.Id}" );

		try
		{
			// Check if response has already been closed
			if ( response.OutputStream == null || !response.OutputStream.CanWrite )
			{
				Log.Warning( $"Response stream for {jsonRpcResponse.Id} is not writable - already closed" );
				return;
			}

			try
			{
				JsonSerializer.Serialize( jsonRpcResponse.Result, _jsonOptions );
			}
			catch ( JsonException ex )
			{
				Log.Error( $"Failed to serialize result for request ID {jsonRpcResponse.Id}: {ex.Message}" );
				Log.Error( ex );
				await SendErrorResponse( response, -32603, "Internal error", ex.Message, jsonRpcResponse.Id );
				return;
			}
			catch ( InvalidOperationException ex )
			{
				Log.Error(
					$"Invalid operation while serializing result for request ID {jsonRpcResponse.Id}: {ex.Message}" );
				Log.Error( ex );
				await SendErrorResponse( response, -32603, "Internal error", ex.Message, jsonRpcResponse.Id );
				return;
			}

			string json;

			try
			{
				json = JsonSerializer.Serialize( jsonRpcResponse,
					new JsonSerializerOptions()
					{
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
						DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
						WriteIndented = false
					} );
			}
			catch ( JsonException ex )
			{
				Log.Error( $"Failed to serialize JSON-RPC response: {ex.Message}" );
				Log.Error( ex );
				await SendErrorResponse( response, -32603, "Internal error", ex.Message, jsonRpcResponse.Id );
				return;
			}
			catch ( InvalidOperationException ex )
			{
				Log.Error( $"Invalid operation while serializing JSON-RPC response: {ex.Message}" );
				Log.Error( ex );
				await SendErrorResponse( response, -32603, "Internal error", ex.Message, jsonRpcResponse.Id );
				return;
			}
			catch ( Exception ex )
			{
				Log.Error( $"Unexpected error while serializing JSON-RPC response: {ex.Message}" );
				Log.Error( ex );
				await SendErrorResponse( response, -32603, "Internal error", ex.Message, jsonRpcResponse.Id );
				return;
			}

			var buffer = Encoding.UTF8.GetBytes( json );

			response.ContentType = "application/json";
			response.ContentLength64 = buffer.Length;
			response.StatusCode = 200; // OK

			await response.OutputStream.WriteAsync( buffer, 0, buffer.Length );
			response.OutputStream.Close();

			Log.Info( $"Sent JSON-RPC response, closed response stream for request ID {jsonRpcResponse.Id}" );
		}
		catch ( ObjectDisposedException ex )
		{
			Log.Warning(
				$"Response for response {jsonRpcResponse.Id} was disposed during SendJsonResponse: {ex.Message}" );
		}
		catch ( InvalidOperationException ex )
		{
			Log.Warning(
				$"Response for response {jsonRpcResponse.Id} in invalid state during SendJsonResponse: {ex.Message}" );
		}
		catch ( HttpListenerException ex )
		{
			Log.Warning( $"HttpListener error for response {jsonRpcResponse.Id}: {ex.Message}" );
		}
	}

	private static async Task SendErrorResponse( HttpListenerResponse response, int code, string message,
		string data = null, object id = null )
	{
		Log.Error( $"Sending error response: Code={code}, Message={message}, Data={data}, Id={id}" );
		try
		{
			if ( response.OutputStream == null || !response.OutputStream.CanWrite )
			{
				Log.Warning( "Response stream is not writable - already closed" );
				return;
			}

			var error = new JsonRpcError { Code = code, Message = message, Data = data };
			var errorResponse = new JsonRpcResponse { Id = id, Error = error };
			await SendJsonResponse( response, errorResponse );
		}
		catch ( ObjectDisposedException ex )
		{
			Log.Warning( $"Response was disposed during SendErrorResponse: {ex.Message}" );
		}
		catch ( InvalidOperationException ex )
		{
			Log.Warning( $"Response in invalid state during SendErrorResponse{ex.Message}" );
		}
		catch ( HttpListenerException ex )
		{
			Log.Warning( $"HttpListener error during SendErrorResponse: {ex.Message}" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Unexpected error in SendErrorResponse: {ex.Message}" );
		}
	}
}
