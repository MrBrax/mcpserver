using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "set_gameobject_rotation", "Set GameObject Rotation",
	"Set the rotation of a GameObject in the scene" )]
public class SetGameObjectRotationTool : IMCPTool
{
	public string Name => "set_gameobject_rotation";
	public string Title => "Set GameObject Rotation";
	public string Description => "Set the rotation of a GameObject in the scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			gameObjectId = new { type = "string", description = "ID of the GameObject" },
			rotation = new
			{
				type = "array",
				items = new { type = "number" },
				description = "New rotation as an array [pitch, yaw, roll]"
			}
		},
		required = new[] { "gameObjectId", "rotation" }
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
		var rotationArray = arguments.GetValueOrDefault( "rotation" ) as JsonElement?;

		if ( string.IsNullOrEmpty( gameObjectIdStr ) || !rotationArray.HasValue ||
		     rotationArray.Value.ValueKind != JsonValueKind.Array )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "GameObject ID and rotation are required" } );
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

			var rotArray = rotationArray.Value.EnumerateArray().Select( x => x.GetSingle() ).ToArray();
			if ( rotArray.Length < 3 )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "Rotation must be an array of at least 3 numbers" } );
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

			using ( SceneEditorSession.Active.UndoScope( "Set Rotation" )
				       .WithGameObjectChanges( gameObject, GameObjectUndoFlags.All ).Push() )
			{
				gameObject.WorldRotation = new Angles( rotArray[0], rotArray[1], rotArray[2] );
			}

			result.Content.Add(
				new TextContent { Text = $"Successfully set rotation of GameObject: {gameObject.Name}" } );

			result.StructuredContent = new
			{
				gameObjectId = gameObject.Id,
				rotation = new[]
				{
					gameObject.WorldRotation.Pitch(), gameObject.WorldRotation.Yaw(),
					gameObject.WorldRotation.Roll()
				}
			};
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error setting GameObject rotation: {ex.Message}" } );
		}

		return result;
	}
}
