using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "get_component_types", "Get Component Types",
	"Get all available component types in the editor" )]
public class GetComponentTypesTool : IMCPTool
{
	public string Name => "get_component_types";
	public string Title => "Get Component Types";
	public string Description => "Get all available component types in the editor";

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
			var componentTypes = EditorTypeLibrary.GetTypes()
				.Where( t => typeof(Component).IsAssignableFrom( t.TargetType ) && !t.IsAbstract )
				.Select( t => new { name = t.Name, fullName = t.FullName } )
				.ToList();

			if ( componentTypes.Count == 0 )
			{
				result.Content.Add( new TextContent { Text = "No component types found" } );
			}
			else
			{
				result.Content.Add( new TextContent { Text = $"Found {componentTypes.Count} component types" } );
			}

			result.StructuredContent = new { componentTypes };
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error retrieving component types: {ex.Message}" } );
		}

		return result;
	}
}
