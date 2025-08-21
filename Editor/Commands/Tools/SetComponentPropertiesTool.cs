using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "set_component_properties", "Set Component Properties",
	"Set one or more properties of a component on a GameObject" )]
public class SetComponentPropertiesTool : IMCPTool
{
	public string Name => "set_component_properties";
	public string Title => "Set Component Properties";
	public string Description => "Set one or more properties of a component on a GameObject";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			gameObjectId = new { type = "string", description = "ID of the GameObject" },
			componentId = new { type = "string", description = "ID of the component" },
			properties = new
			{
				type = "array",
				description = "Array of properties to set",
				items = new
				{
					type = "object",
					properties = new
					{
						name = new { type = "string", description = "Name of the property to set" },
						value =
							new { type = "string", description = "Value to set the property to" }
					},
					required = new[] { "name", "value" }
				},
				minItems = 1
			}
		},
		required = new[] { "gameObjectId", "componentId", "properties" }
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
		var propertiesArg = arguments.GetValueOrDefault( "properties" );

		if ( string.IsNullOrEmpty( gameObjectIdStr ) || string.IsNullOrEmpty( componentIdStr ) ||
		     propertiesArg == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "gameObjectId, componentId, and properties are required" } );
			return result;
		}

		// Parse properties array
		List<(string name, string value)> propertiesToSet;
		try
		{
			var propertiesJson = JsonSerializer.Serialize( propertiesArg );
			var propertiesArray = JsonSerializer.Deserialize<JsonElement[]>( propertiesJson );

			propertiesToSet = propertiesArray.Select( prop =>
			{
				var name = prop.GetProperty( "name" ).GetString();
				var value = prop.GetProperty( "value" ).GetString();
				return (name, value);
			} ).ToList();

			if ( !propertiesToSet.Any() )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "At least one property must be specified" } );
				return result;
			}
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Invalid properties format: {ex.Message}" } );
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

			var componentProperties = EditorTypeLibrary.GetPropertyDescriptions( component ).ToList();
			var serializedObject = component.GetSerialized();
			var successfulProperties = new List<(string name, string value)>();
			var failedProperties = new List<(string name, string value, string error)>();

			using ( SceneEditorSession.Active.UndoScope( "Set Properties" )
				       .WithGameObjectChanges( gameObject, GameObjectUndoFlags.All ).Push() )
			{
				foreach ( var (propertyName, propertyValue) in propertiesToSet )
				{
					try
					{
						var property = componentProperties
							.FirstOrDefault( p => p.Name.Equals( propertyName, StringComparison.OrdinalIgnoreCase ) );

						if ( property == null )
						{
							failedProperties.Add( (propertyName, propertyValue,
								$"Property not found: {propertyName}") );
							continue;
						}

						if ( !serializedObject.TryGetProperty( propertyName, out var serializedProperty ) )
						{
							failedProperties.Add( (propertyName, propertyValue,
								$"Property '{propertyName}' not found as a SerializedProperty") );
							continue;
						}

						Log.Info(
							$"Setting property '{propertyName}' on component {component.GetType().Name} with value '{propertyValue}'" );

						serializedObject.NoteStartEdit( serializedProperty );

						object newValue = propertyValue;

						if ( property.PropertyType.IsAssignableFrom( typeof(Sandbox.Resource) ) ||
						     property.PropertyType.IsAssignableTo( typeof(Sandbox.Resource) ) )
						{
							var asset = AssetSystem.FindByPath( propertyValue );

							if ( asset != null )
							{
								Log.Info(
									$"Found asset for property '{propertyName}': {asset.RelativePath} (type: {asset.AssetType})" );
								newValue = asset.LoadResource();
							}
							else
							{
								Log.Info( $"No asset found for property '{propertyName}': {propertyValue}" );
							}
						}
						else
						{
							Log.Info( "Property is not a Resource type, using raw value" );
						}

						serializedProperty.SetValue( newValue );
						serializedObject.NoteFinishEdit( serializedProperty );
						serializedObject.NoteChanged( serializedProperty );
						serializedObject.OnPropertyChanged?.Invoke( serializedProperty );

						successfulProperties.Add( (propertyName, propertyValue) );
						Log.Info(
							$"Property '{propertyName}' set to '{propertyValue}' on component {component.GetType().Name}" );
					}
					catch ( Exception ex )
					{
						failedProperties.Add( (propertyName, propertyValue, $"Error setting property: {ex.Message}") );
						Log.Error(
							$"Error setting property '{propertyName}' on component {component.GetType().Name}: {ex.Message}" );
					}
				}
			}

			// Build result message
			var messages = new List<string>();

			if ( successfulProperties.Any() )
			{
				messages.Add(
					$"Successfully set {successfulProperties.Count} properties: {string.Join( ", ", successfulProperties.Select( p => p.name ) )}" );
			}

			if ( failedProperties.Any() )
			{
				messages.Add( $"Failed to set {failedProperties.Count} properties:" );
				foreach ( var (name, _, error) in failedProperties )
				{
					messages.Add( $"  - {name}: {error}" );
				}
			}

			if ( failedProperties.Any() && !successfulProperties.Any() )
			{
				result.IsError = true;
			}

			result.Content.Add( new TextContent { Text = string.Join( "\n", messages ) } );
			result.StructuredContent = new
			{
				gameObjectId = gameObject.Id,
				componentId = component.Id,
				successful = successfulProperties.Select( p => new { p.name, p.value } ).ToArray(),
				failed = failedProperties.Select( p => new { p.name, p.value, p.error } ).ToArray()
			};
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error setting component properties: {ex.Message}" } );
			return result;
		}

		return result;
	}
}
