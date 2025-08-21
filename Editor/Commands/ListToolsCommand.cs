using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Braxnet.Commands;

namespace Braxnet.Commands;

[MCPCommand( "tools/list" )]
public class ListToolsCommand : IMCPCommand
{
	public string Name => "tools/list";

	public async Task<object> ExecuteAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		// Log.Info( $"Executing {Name} command for session {sessionId} with protocol version {protocolVersion}" );
		List<Tool> tools = new();

		/*tools = MCPCommandRegistry.GetAllTools().Select( tool => new Tool
		{
			Name = tool.Name,
			Title = tool.Title,
			Description = tool.Description,
			InputSchema = tool.InputSchema,
			OutputSchema = tool.OutputSchema
		} ).ToList();*/

		foreach ( var tool in MCPCommandRegistry.GetAllTools() )
		{
			// Log.Info( $"Found tool: {tool.Name} - {tool.Title}" );
			tools.Add( new Tool
			{
				Name = tool.Name, Title = tool.Title, Description = tool.Description, InputSchema = tool.InputSchema,
				// OutputSchema = tool.OutputSchema
			} );
		}

		// Log.Info( $"Returning {tools.Count} tools for session {sessionId}" );

		return new ListToolsResult { Tools = tools };
	}
}
