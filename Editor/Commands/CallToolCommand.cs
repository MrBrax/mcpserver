using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Braxnet.Commands;

[MCPCommand( "tools/call" )]
public class CallToolCommand : IMCPCommand
{
	public string Name => "tools/call";

	public async Task<object> ExecuteAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		var result = new CallToolResult();

		if ( request.Params.HasValue )
		{
			var paramsObj = JsonSerializer.Deserialize<Dictionary<string, object>>(
				request.Params.Value.GetRawText(), MCPServer.JsonOptions );

			var toolName = paramsObj?.GetValueOrDefault( "name" )?.ToString();
			var argumentsElement = paramsObj?.GetValueOrDefault( "arguments" );

			Dictionary<string, object> arguments = null;
			if ( argumentsElement is JsonElement argsJson )
			{
				arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(
					argsJson.GetRawText(), MCPServer.JsonOptions );
			}

			try
			{
				var toolResult = await MCPCommandRegistry.ExecuteToolAsync( toolName, arguments, sessionId );
				if ( toolResult != null )
				{
					return toolResult;
				}
				else
				{
					result.IsError = true;
					result.Content.Add( new TextContent { Text = $"Unknown tool: {toolName}" } );
				}
			}
			catch ( System.Exception ex )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = $"Tool execution error: {ex.Message}" } );
			}
		}
		else
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "No parameters provided" } );
		}

		return result;
	}
}
