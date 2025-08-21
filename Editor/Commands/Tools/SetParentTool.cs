using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "set_parent", "Set Parent Tool", "Set the parent of a GameObject in the current scene" )]
public class SetParentTool : IMCPTool
{
	public string Name => "set_parent";
	public string Title => "Set Parent Tool";
	public string Description => "Set the parent of a GameObject in the current scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			id = new { type = "string", description = "The ID of the GameObject to set the parent for" },
			parentId = new { type = "string", description = "The ID of the new parent GameObject" }
		},
		required = new[] { "id", "parentId" }
	} );

	public JsonElement OutputSchema => default;

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		if ( !arguments.TryGetValue( "id", out var idObj ) || idObj is not string id ||
		     !arguments.TryGetValue( "parentId", out var parentIdObj ) || parentIdObj is not string parentId )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Invalid or missing 'id' or 'parentId' argument." } );
			return result;
		}

		if ( string.IsNullOrEmpty( id ) || string.IsNullOrEmpty( parentId ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "GameObject ID and Parent ID cannot be empty." } );
			return result;
		}

		if ( !Guid.TryParse( id, out var guid ) || !Guid.TryParse( parentId, out var parentGuid ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Invalid GameObject ID or Parent ID format." } );
			return result;
		}

		var gameObject = Game.ActiveScene.Directory.FindByGuid( guid );
		var parentGameObject = Game.ActiveScene.Directory.FindByGuid( parentGuid );

		if ( gameObject == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"GameObject with ID '{id}' not found." } );
			return result;
		}

		if ( parentGameObject == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Parent GameObject with ID '{parentId}' not found." } );
			return result;
		}

		await GameTask.MainThread(); // Ensure this runs on the main thread

		using ( SceneEditorSession.Active.UndoScope( "Set Parent" )
			       .WithGameObjectChanges( gameObject, GameObjectUndoFlags.All ).Push() )
		{
			gameObject.SetParent( parentGameObject );
		}

		result.Content.Add( new TextContent
		{
			Text = $"GameObject with ID '{id}' has been set to have parent '{parentId}'."
		} );

		return result;
	}
}
