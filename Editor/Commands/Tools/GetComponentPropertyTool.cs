using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "get_component_property", "Get Component Property",
	"Get a property of a component on a GameObject" )]
public class GetComponentPropertyTool : IMCPTool
{
	public string Name => "get_component_property";
	public string Title => "Get Component Property";
	public string Description => "Get a property of a component on a GameObject";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			gameObjectId = new { type = "string", description = "ID of the GameObject" },
			componentId = new { type = "string", description = "ID of the component" },
			propertyName = new { type = "string", description = "Name of the property to get" }
		},
		required = new[] { "gameObjectId", "componentId", "propertyName" }
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
		var propertyName = arguments.GetValueOrDefault( "propertyName" )?.ToString();

		if ( string.IsNullOrEmpty( gameObjectIdStr ) || string.IsNullOrEmpty( componentIdStr ) ||
		     string.IsNullOrEmpty( propertyName ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "All fields are required" } );
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

			var property = EditorTypeLibrary.GetPropertyDescriptions( component )
				.FirstOrDefault( p => p.Name.Equals( propertyName, StringComparison.OrdinalIgnoreCase ) );

			if ( property == null )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = $"Property not found: {propertyName}" } );
				return result;
			}

			object value;
			try
			{
				// Attempt to get the property value
				value = property.GetValue( component );
			}
			catch ( Exception ex )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = $"Error getting property: {ex.Message}" } );
				return result;
			}

			if ( value == null )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = $"Property '{propertyName}' is null" } );
				return result;
			}

			result.Content.Add( new TextContent { Text = $"Successfully retrieved property '{propertyName}'" } );
			result.StructuredContent = new
			{
				gameObjectId = gameObject.Id,
				componentId = component.Id,
				propertyName = property.Name,
				propertyValue = value.ToString()
			};
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error getting component property: {ex.Message}" } );
			return result;
		}

		return result;
	}
}
