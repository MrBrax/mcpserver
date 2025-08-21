using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;

namespace Braxnet.Commands.Tools;

[MCPTool( "get_logs", "Get Logs",
	"Get the logs from the editor" )]
public class GetLogsTool : IMCPTool
{
	public string Name => "get_logs";
	public string Title => "Get Logs";
	public string Description => "Get the logs from the editor";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object", properties = new { }, required = Array.Empty<string>()
	} );

	public JsonElement OutputSchema => default;

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		try
		{
			var currentLogPath = Path.Combine( FileSystem.Root.GetFullPath( "logs" ), "sbox-dev.log" );
			if ( !File.Exists( currentLogPath ) )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "Log file not found" } );
				return result;
			}

			var logContent = await File.ReadAllTextAsync( currentLogPath );
			if ( string.IsNullOrEmpty( logContent ) )
			{
				result.Content.Add( new TextContent { Text = "Log file is empty" } );
			}
			else
			{
				result.Content.Add( new TextContent { Text = logContent } );
			}
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error retrieving logs: {ex.Message}" } );
		}

		return result;
	}
}
