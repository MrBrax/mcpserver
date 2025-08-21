using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "add_component", "Add Component", "Add a component to a GameObject" )]
public class AddComponentTool : IMCPTool
{
	public string Name => "add_component";
	public string Title => "Add Component";
	public string Description => "Add a component to a GameObject";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			gameObjectId = new { type = "string", description = "ID of the GameObject" },
			componentType = new
			{
				type = "string",
				description = "Type of the component to add (e.g., 'ModelRenderer', 'RigidBody')"
			}
		},
		required = new[] { "gameObjectId", "componentType" }
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
		var componentType = arguments.GetValueOrDefault( "componentType" )?.ToString();

		if ( string.IsNullOrEmpty( gameObjectIdStr ) || string.IsNullOrEmpty( componentType ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "GameObject ID and component type are required" } );
			return result;
		}

		try
		{
			if ( !Guid.TryParse( gameObjectIdStr, out var gameObjectId ) )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "Invalid GameObject ID format" } );
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

			var componentTypeInfo = EditorTypeLibrary.GetType( componentType );
			if ( componentTypeInfo == null /*|| !typeof( Component ).IsAssignableFrom( componentTypeInfo )*/ )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = $"Invalid component type: {componentType}" } );
				return result;
			}

			Component component = null;

			using ( SceneEditorSession.Active.UndoScope( $"Add {componentType} to {gameObject.Name}" )
				       .WithComponentCreations().Push() )
			{
				component = gameObject.Components.Create( componentTypeInfo );
			}

			result.Content.Add( new TextContent { Text = $"Successfully added {componentType} to GameObject" } );
			result.StructuredContent = new
			{
				gameObjectId = gameObject.Id, componentId = component.Id, componentType = componentType
			};
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error adding component: {ex.Message}" } );
		}

		return result;
	}
}
