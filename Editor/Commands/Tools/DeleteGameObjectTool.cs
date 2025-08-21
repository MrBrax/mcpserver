using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "delete_game_object", "Delete Game Object", "Delete a GameObject from the current scene" )]
public class DeleteGameObjectTool : IMCPTool
{
	public string Name => "delete_game_object";
	public string Title => "Delete Game Object";
	public string Description => "Delete a GameObject from the current scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new { id = new { type = "string", description = "The ID of the GameObject to delete" } },
		required = new[] { "id" }
	} );

	public JsonElement OutputSchema => default;

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		if ( !arguments.TryGetValue( "id", out var idObj ) || idObj is not string id )
		{
			result.Content.Add( new TextContent { Text = "Invalid or missing 'id' argument." } );
			return result;
		}

		if ( string.IsNullOrEmpty( id ) )
		{
			result.Content.Add( new TextContent { Text = "GameObject ID cannot be empty." } );
			return result;
		}

		if ( !Guid.TryParse( id, out var guid ) )
		{
			result.Content.Add( new TextContent { Text = "Invalid GameObject ID format." } );
			return result;
		}

		var gameObject = Game.ActiveScene.Directory.FindByGuid( guid );
		if ( gameObject == null )
		{
			result.Content.Add( new TextContent { Text = $"GameObject with ID '{id}' not found." } );
			return result;
		}

		await GameTask.MainThread(); // Ensure this runs on the main thread

		using ( SceneEditorSession.Active.UndoScope( "Delete GameObject" )
			       .WithGameObjectDestructions( gameObject ).Push() )
		{
			gameObject.Destroy();
		}

		result.Content.Add( new TextContent { Text = $"GameObject with ID '{id}' has been deleted." } );

		return result;
	}
}
