using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "get_children", "Get Children Tool",
	"Get the children of a GameObject in the current scene" )]
public class GetChildrenTool : IMCPTool
{
	public string Name => "get_children";
	public string Title => "Get Children Tool";
	public string Description => "Get the children of a GameObject in the current scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties =
			new { id = new { type = "string", description = "The ID of the GameObject to get children for" } },
		required = new[] { "id" }
	} );

	public JsonElement OutputSchema => default;

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		if ( !arguments.TryGetValue( "id", out var idObj ) || idObj is not string id )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Invalid or missing 'id' argument." } );
			return result;
		}

		if ( string.IsNullOrEmpty( id ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "GameObject ID cannot be empty." } );
			return result;
		}

		if ( !Guid.TryParse( id, out var guid ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Invalid GameObject ID format." } );
			return result;
		}

		var gameObject = Game.ActiveScene.Directory.FindByGuid( guid );
		if ( gameObject == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"GameObject with ID '{id}' not found." } );
			return result;
		}

		await GameTask.MainThread(); // Ensure this runs on the main thread

		var childrenList = gameObject.Children.Select( child => new
		{
			Id = child.Id,
			Name = child.Name,
			LocalPosition = child.LocalPosition,
			LocalRotation = child.LocalRotation,
			LocalScale = child.LocalScale,
			Components = child.Components.Count,
			ChildrenCount = child.Children.Count,
			Enabled = child.Enabled,
			Tags = child.Tags.ToList(),
		} ).ToList();

		result.StructuredContent = new { Children = childrenList };

		return result;
	}
}
