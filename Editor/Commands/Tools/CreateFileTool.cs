using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "create_file", "Create File", "Create a new file in the project" )]
public class CreateFileTool : IMCPTool
{
	public string Name => "create_file";
	public string Title => "Create File";
	public string Description => "Create a new file in the project";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			path = new { type = "string", description = "File path relative to project root" },
			content = new { type = "string", description = "File content" }
		},
		required = new[] { "path", "content" }
	} );

	public JsonElement OutputSchema => default;

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		if ( arguments == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "No arguments provided" } );
			return result;
		}

		var path = arguments.GetValueOrDefault( "path" )?.ToString();
		var content = arguments.GetValueOrDefault( "content" )?.ToString();

		if ( string.IsNullOrEmpty( path ) || content == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Path and content are required" } );
			return result;
		}

		try
		{
			var fullPath = Path.Combine( Directory.GetCurrentDirectory(), path );
			var directory = Path.GetDirectoryName( fullPath );

			if ( !string.IsNullOrEmpty( directory ) )
			{
				Directory.CreateDirectory( directory );
			}

			await File.WriteAllTextAsync( fullPath, content );
			result.Content.Add( new TextContent { Text = $"Successfully created file: {path}" } );
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error creating file: {ex.Message}" } );
		}

		return result;
	}
}

/*
[MCPTool( "execute_command", "Execute Command", "Execute a command in the project directory" )]
public class ExecuteCommandTool : IMCPTool
{
	public string Name => "execute_command";
	public string Title => "Execute Command";
	public string Description => "Execute a command in the project directory";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new { command = new { type = "string", description = "Command to execute" } },
		required = new[] { "command" }
	} );

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object>? arguments, string? sessionId )
	{
		var result = new CallToolResult();

		if ( arguments == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "No arguments provided" } );
			return result;
		}

		var command = arguments.GetValueOrDefault( "command" )?.ToString();

		if ( string.IsNullOrEmpty( command ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Command is required" } );
			return result;
		}

		try
		{
			// For security, limit to safe commands
			var safeCommands = new[] { "dotnet", "git", "npm", "yarn", "ls", "dir", "echo" };
			var commandParts = command.Split( ' ', StringSplitOptions.RemoveEmptyEntries );

			if ( commandParts.Length == 0 || !safeCommands.Contains( commandParts[0].ToLower() ) )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "Command not allowed for security reasons" } );
				return result;
			}

			// Execute command (simplified - in a real implementation you'd use Process.Start)
			result.Content.Add( new TextContent { Text = $"Would execute command: {command}" } );
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error executing command: {ex.Message}" } );
		}

		return result;
	}
}*/
