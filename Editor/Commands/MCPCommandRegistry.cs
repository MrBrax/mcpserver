using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using Sandbox;

namespace Braxnet.Commands;

public static class MCPCommandRegistry
{
	private static readonly Dictionary<string, IMCPCommand> _commands = new();
	private static readonly Dictionary<string, IMCPTool> _tools = new();
	private static readonly Dictionary<string, IMCPNotification> _notifications = new();
	private static bool _initialized = false;

	[ConVar( "mcp.append_structured_content_to_text", ConVarFlags.Saved )]
	public static bool AppendStructuredContentToText { get; set; } = false;

	public static void Initialize()
	{
		if ( _initialized )
		{
			_commands.Clear();
			_tools.Clear();
			_notifications.Clear();
			Log.Info( "MCP Command Registry re-initialized" );
		}

		try
		{
			RegisterCommands();
			RegisterTools();
			RegisterNotifications();
			_initialized = true;
			Log.Info( $"MCP Command Registry initialized with {_commands.Count} commands, {_tools.Count} tools, and {_notifications.Count} notifications" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Failed to initialize MCP Command Registry: {ex.Message}" );
		}
	}

	private static void RegisterCommands()
	{
		var commandTypes = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where( t => t.GetInterfaces().Contains( typeof( IMCPCommand ) ) && !t.IsAbstract );

		foreach ( var type in commandTypes )
		{
			try
			{
				var attribute = type.GetCustomAttribute<MCPCommandAttribute>();
				if ( attribute != null && Activator.CreateInstance( type ) is IMCPCommand command )
				{
					_commands[attribute.Name] = command;
					Log.Info( $"Registered MCP command: {attribute.Name}" );
				}
			}
			catch ( Exception ex )
			{
				Log.Error( $"Failed to register command {type.Name}: {ex.Message}" );
			}
		}
	}

	private static void RegisterTools()
	{
		var toolTypes = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where( t => t.GetInterfaces().Contains( typeof( IMCPTool ) ) && !t.IsAbstract );

		foreach ( var type in toolTypes )
		{
			try
			{
				var attribute = type.GetCustomAttribute<MCPToolAttribute>();
				if ( attribute != null && Activator.CreateInstance( type ) is IMCPTool tool )
				{
					_tools[attribute.Name] = tool;
					Log.Info( $"Registered MCP tool: {attribute.Name}" );
				}
			}
			catch ( Exception ex )
			{
				Log.Error( $"Failed to register tool {type.Name}: {ex.Message}" );
			}
		}
	}

	private static void RegisterNotifications()
	{
		var notificationTypes = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where( t => t.GetInterfaces().Contains( typeof( IMCPNotification ) ) && !t.IsAbstract );

		foreach ( var type in notificationTypes )
		{
			try
			{
				var attribute = type.GetCustomAttribute<MCPNotificationAttribute>();
				if ( attribute != null && Activator.CreateInstance( type ) is IMCPNotification notification )
				{
					_notifications[attribute.Name] = notification;
					Log.Info( $"Registered MCP notification: {attribute.Name}" );
				}
			}
			catch ( Exception ex )
			{
				Log.Error( $"Failed to register notification {type.Name}: {ex.Message}" );
			}
		}
	}

	public static async Task<object> ExecuteCommandAsync( string methodName, JsonRpcRequest request, string sessionId,
		string protocolVersion )
	{
		if ( _commands.TryGetValue( methodName, out var command ) )
		{
			Log.Info( $"Executing MCP command: {methodName}" );
			return await command.ExecuteAsync( request, sessionId, protocolVersion );
		}

		Log.Warning( $"MCP command not found: {methodName}" );

		return null;
	}

	public static async Task<CallToolResult> ExecuteToolAsync( string toolName, Dictionary<string, object> arguments,
		string sessionId )
	{
		if ( _tools.TryGetValue( toolName, out var tool ) )
		{
			Log.Info( $"Executing MCP tool: {toolName}" );
			var toolResult = await tool.ExecuteAsync( arguments, sessionId );

			if ( toolResult != null )
			{
				if ( AppendStructuredContentToText && toolResult.StructuredContent != null )
				{
					toolResult.Content.Add( new TextContent
					{
						Text = JsonSerializer.Serialize( toolResult.StructuredContent )
					} );
				}
			}

			return toolResult;
		}

		Log.Warning( $"MCP tool not found: {toolName}" );

		return null;
	}

	public static async Task<bool> HandleNotificationAsync( string methodName, JsonRpcRequest request, string sessionId,
		string protocolVersion )
	{
		if ( _notifications.TryGetValue( methodName, out var notification ) )
		{
			Log.Info( $"Handling MCP notification: {methodName}" );
			await notification.HandleAsync( request, sessionId, protocolVersion );
			return true;
		}

		Log.Warning( $"MCP notification handler not found: {methodName}" );
		return false;
	}

	public static IEnumerable<IMCPCommand> GetAllCommands() => _commands.Values;
	public static IEnumerable<IMCPTool> GetAllTools() => _tools.Values;
	public static IEnumerable<IMCPNotification> GetAllNotifications() => _notifications.Values;
	public static bool HasCommand( string name ) => _commands.ContainsKey( name );
	public static bool HasTool( string name ) => _tools.ContainsKey( name );
	public static bool HasNotification( string name ) => _notifications.ContainsKey( name );
}
