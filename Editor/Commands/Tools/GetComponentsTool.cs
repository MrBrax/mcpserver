using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "get_components", "Get Components",
	"Get all components of a GameObject" )]
public class GetComponentsTool : IMCPTool
{
	public string Name => "get_components";
	public string Title => "Get Components";
	public string Description => "Get all components of a GameObject";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new { gameObjectId = new { type = "string", description = "ID of the GameObject" }, },
		required = new[] { "gameObjectId" }
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

		if ( string.IsNullOrEmpty( gameObjectIdStr ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "GameObject ID is required" } );
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

			// await GameTask.MainThread(); // Ensure this runs on the main thread

			var gameObject = SceneEditorSession.Active.Scene.Directory.FindByGuid( gameObjectId );
			if ( gameObject == null )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = $"GameObject not found: {gameObjectId}" } );
				return result;
			}

			var components = gameObject.Components.GetAll<Component>( FindMode.EverythingInSelfAndDescendants );

			/*var componentList = components.Select( c => new { id = c.Id, type = c.GetType().FullName, name = c. } )
				.ToList();*/

			var componentList = new List<ComponentInfo>();
			foreach ( var component in components )
			{
				var typeDescription = EditorTypeLibrary.GetType( component.GetType() );
				if ( typeDescription == null )
				{
					result.IsError = true;
					result.Content.Add( new TextContent { Text = $"Component type not found: {component.GetType()}" } );
					return result;
				}

				var name = typeDescription?.Name ?? component.GetType().Name;

				var properties = new Dictionary<string, string>();

				var propertyAttributes = TypeLibrary.GetPropertyDescriptions( component );
				foreach ( var p in propertyAttributes )
				{
					if ( !p.HasAttribute<PropertyAttribute>() ) continue;

					try
					{
						var value = p.GetValue( component );
						if ( value != null )
						{
							properties[p.Name] = value.ToString();
						}
					}
					catch ( Exception ex )
					{
						properties[p.Name] = $"Error getting value: {ex.Message}";
					}
				}

				var componentInfo = new ComponentInfo
				{
					Id = component.Id, Type = component.GetType().FullName, Name = name, Properties = properties
				};

				componentList.Add( componentInfo );
			}

			result.Content.Add( new TextContent { Text = $"Found {componentList.Count} components" } );
			result.StructuredContent = new { gameObjectId = gameObject.Id, components = componentList };
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error getting components: {ex.Message}" } );
		}

		return result;
	}

	public class ComponentInfo
	{
		public Guid Id { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }
		public Dictionary<string, string> Properties { get; set; } = new();
	}
}
