using System.Threading.Tasks;
using System.Text.Json;
using System.Net;
using System;
using System.Linq;
using Sandbox;

namespace Braxnet.Commands;

[MCPCommand( "initialize" )]
public class InitializeCommand : IMCPCommand
{
	public string Name => "initialize";

	// Supported protocol versions in order of preference (latest first)
	private static readonly string[] SupportedVersions = new[] { "2025-06-18", "2024-11-05" };

	public async Task<object> ExecuteAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		InitializeRequest initRequest = null;
		if ( request.Params.HasValue )
		{
			initRequest =
				JsonSerializer.Deserialize<InitializeRequest>( request.Params.Value.GetRawText(),
					MCPServer.JsonOptions );
		}

		// Protocol version negotiation
		string negotiatedVersion = protocolVersion;

		if ( initRequest?.ProtocolVersion != null )
		{
			// Client requested a specific version
			var clientVersion = initRequest.ProtocolVersion;

			if ( SupportedVersions.Contains( clientVersion ) )
			{
				// Use the client's requested version if we support it
				negotiatedVersion = clientVersion;
				Log.Info( $"Using client-requested protocol version: {negotiatedVersion}" );
			}
			else
			{
				// Use our latest supported version
				negotiatedVersion = SupportedVersions[0];
				Log.Info( $"Client requested unsupported version {clientVersion}, using {negotiatedVersion}" );
			}
		}
		else
		{
			// Use the latest supported version
			negotiatedVersion = SupportedVersions[0];
			Log.Info( $"No client version specified, using latest: {negotiatedVersion}" );
		}

		var newSessionId = Guid.NewGuid().ToString();
		MCPServer.RegisterSession( newSessionId );

		Log.Info( $"Initializing session {newSessionId} with protocol version {negotiatedVersion}" );
		Log.Info( $"Client info: {initRequest?.ClientInfo?.Name} {initRequest?.ClientInfo?.Version}" );

		var result = new InitializeResult
		{
			ProtocolVersion = negotiatedVersion,
			ServerInfo =
				new Implementation { Name = "sbox-mcp-server", Version = "1.0.0", Title = "S&box MCP Server" },
			Capabilities = new ServerCapabilities
			{
				Resources = new ResourcesCapability { Subscribe = false, ListChanged = false },
				Tools = new ToolsCapability { ListChanged = false },
				Logging = new LoggingCapability()
			},
			Instructions =
				"MCP server for S&box development. Provides access to project files and development tools."
		};

		// Store the session ID for the response handler to access
		MCPServer.SetCurrentInitializeSessionId( newSessionId );

		await Task.CompletedTask;
		return result;
	}
}
