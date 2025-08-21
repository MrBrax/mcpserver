using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Sandbox;
using Braxnet;
using Braxnet.Commands;

namespace Braxnet.Commands.Tools;

/// <summary>
/// Example test tool that demonstrates server-sent events notification functionality
/// </summary>
[MCPTool( "test_sse_notifications", "Test SSE Notifications", "Test tool for demonstrating server-sent events notifications" )]
public class TestSseNotificationTool : IMCPTool
{
	public string Name => "test_sse_notifications";
	public string Title => "Test SSE Notifications";
	public string Description => "Test tool for demonstrating server-sent events notifications";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		description = "Test tool for demonstrating server-sent events notifications",
		properties = new
		{
			notification_type = new
			{
				type = "string",
				description = "Type of notification to send",
				@enum = new[] { "resource_list_changed", "tool_list_changed", "prompt_list_changed", "progress", "resource_updated" }
			},
			session_id = new
			{
				type = "string",
				description = "Session ID to send notification to (optional, broadcasts to all if not provided)"
			},
			uri = new
			{
				type = "string", 
				description = "URI for resource updated notification"
			},
			progress = new
			{
				type = "number",
				description = "Progress value (0.0 to 1.0) for progress notification"
			}
		},
		required = new[] { "notification_type" }
	} );

	public JsonElement OutputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			message = new { type = "string" },
			type = new { type = "string" },
			target = new { type = "string" },
			uri = new { type = "string" },
			progress = new { type = "number" },
			error = new { type = "string" }
		}
	} );

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		try
		{
			if ( !arguments.TryGetValue( "notification_type", out var notificationTypeObj ) )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "Missing required parameter: notification_type" } );
				return result;
			}

			var notificationType = notificationTypeObj.ToString();
			arguments.TryGetValue( "session_id", out var targetSessionIdObj );
			var targetSessionId = targetSessionIdObj?.ToString();

			switch ( notificationType )
			{
				case "resource_list_changed":
					await MCPServer.SendResourceListChangedNotification( targetSessionId );
					result.Content.Add( new TextContent { Text = $"Resource list changed notification sent to {targetSessionId ?? "all sessions"}" } );
					break;

				case "tool_list_changed":
					await MCPServer.SendToolListChangedNotification( targetSessionId );
					result.Content.Add( new TextContent { Text = $"Tool list changed notification sent to {targetSessionId ?? "all sessions"}" } );
					break;

				case "prompt_list_changed":
					await MCPServer.SendPromptListChangedNotification( targetSessionId );
					result.Content.Add( new TextContent { Text = $"Prompt list changed notification sent to {targetSessionId ?? "all sessions"}" } );
					break;

				case "resource_updated":
					arguments.TryGetValue( "uri", out var uriObj );
					var uri = uriObj?.ToString() ?? "file:///example/resource.txt";
					await MCPServer.SendResourceUpdatedNotification( uri, targetSessionId );
					result.Content.Add( new TextContent { Text = $"Resource updated notification sent for URI: {uri} to {targetSessionId ?? "all sessions"}" } );
					break;

				case "progress":
					arguments.TryGetValue( "progress", out var progressObj );
					var progress = 0.5;
					if ( progressObj != null && double.TryParse( progressObj.ToString(), out var parsedProgress ) )
					{
						progress = parsedProgress;
					}
					await MCPServer.SendProgressNotification( "test-progress-token", progress, 1.0, targetSessionId );
					result.Content.Add( new TextContent { Text = $"Progress notification sent with progress: {progress} to {targetSessionId ?? "all sessions"}" } );
					break;

				default:
					result.IsError = true;
					result.Content.Add( new TextContent { Text = $"Unknown notification type: {notificationType}" } );
					break;
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error in TestSseNotificationTool: {ex.Message}" );
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error: {ex.Message}" } );
		}

		return result;
	}
}
