using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "remove_component", "Remove Component",
	"Remove a component from a GameObject" )]
public class RemoveComponentTool : IMCPTool
{
	public string Name => "remove_component";
	public string Title => "Remove Component";
	public string Description => "Remove a component from a GameObject";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			gameObjectId = new { type = "string", description = "ID of the GameObject" },
			componentId = new { type = "string", description = "ID of the component to remove" }
		},
		required = new[] { "gameObjectId", "componentId" }
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

		var gameObjectIdStr = arguments.GetValueOrDefault( "gameObjectId" )?.ToString();
		var componentIdStr = arguments.GetValueOrDefault( "componentId" )?.ToString();

		if ( string.IsNullOrEmpty( gameObjectIdStr ) || string.IsNullOrEmpty( componentIdStr ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "GameObject ID and Component ID are required" } );
			return result;
		}

		try
		{
			if ( !Guid.TryParse( gameObjectIdStr, out var gameObjectId ) ||
			     !Guid.TryParse( componentIdStr, out var componentId ) )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "Invalid GameObject or Component ID format" } );
				return result;
			}

			await GameTask.MainThread(); // Ensure this runs on the main thread

			var gameObject = SceneEditorSession.Active.Scene.Directory.FindByGuid( gameObjectId );
			if ( gameObject == null )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = $"GameObject not found: {gameObjectId}" } );
				return result;
			}

			var component = gameObject.Components.FirstOrDefault( c => c.Id == componentId );
			if ( component == null )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = $"Component not found: {componentId}" } );
				return result;
			}

			using ( SceneEditorSession.Active.UndoScope( $"Remove {component.GetType().Name} from {gameObject.Name}" )
				       .WithComponentDestructions( component ).Push() )
			{
				component.Destroy();
			}

			result.Content.Add(
				new TextContent { Text = $"Successfully removed component: {component.GetType().Name}" } );
			result.StructuredContent = new
			{
				gameObjectId = gameObject.Id,
				componentId = component.Id,
				componentType = component.GetType().FullName
			};
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error removing component: {ex.Message}" } );
			return result;
		}

		return result;
	}
}
