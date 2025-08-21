using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "set_gameobject_position", "Set GameObject Position",
	"Set the position of a GameObject in the scene" )]
public class SetGameObjectPositionTool : IMCPTool
{
	public string Name => "set_gameobject_position";
	public string Title => "Set GameObject Position";
	public string Description => "Set the position of a GameObject in the scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			gameObjectId = new { type = "string", description = "ID of the GameObject" },
			position = new
			{
				type = "array",
				items = new { type = "number" },
				description = "New position as an array [x, y, z]"
			}
		},
		required = new[] { "gameObjectId", "position" }
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
		var positionArray = arguments.GetValueOrDefault( "position" ) as JsonElement?;

		if ( string.IsNullOrEmpty( gameObjectIdStr ) || !positionArray.HasValue ||
		     positionArray.Value.ValueKind != JsonValueKind.Array )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "GameObject ID and position are required" } );
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

			var posArray = positionArray.Value.EnumerateArray().Select( x => x.GetSingle() ).ToArray();
			if ( posArray.Length < 3 )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "Position must be an array of at least 3 numbers" } );
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

			using ( SceneEditorSession.Active.UndoScope( "Set Position" )
				       .WithGameObjectChanges( gameObject, GameObjectUndoFlags.All ).Push() )
			{
				gameObject.WorldPosition = new Vector3( posArray[0], posArray[1], posArray[2] );
			}

			result.Content.Add(
				new TextContent { Text = $"Successfully set position of GameObject: {gameObject.Name}" } );

			result.StructuredContent = new
			{
				gameObjectId = gameObject.Id,
				position = new[]
				{
					gameObject.WorldPosition.x, gameObject.WorldPosition.y, gameObject.WorldPosition.z
				}
			};
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error setting GameObject position: {ex.Message}" } );
		}

		return result;
	}
}
