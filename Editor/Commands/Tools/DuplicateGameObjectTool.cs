using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "duplicate_game_object", "Duplicate Game Object",
	"Duplicate a GameObject in the current scene" )]
public class DuplicateGameObjectTool : IMCPTool
{
	public string Name => "duplicate_game_object";
	public string Title => "Duplicate Game Object";
	public string Description => "Duplicate a GameObject in the current scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			id = new { type = "string", description = "The ID of the GameObject to duplicate" },
			position =
				new
				{
					type = "object",
					properties =
						new
						{
							x = new { type = "number", description = "X position" },
							y = new { type = "number", description = "Y position" },
							z = new { type = "number", description = "Z position" }
						},
					description = "Position to place the duplicated GameObject"
				},
			rotation = new
			{
				type = "object",
				properties =
					new
					{
						pitch = new { type = "number", description = "Pitch rotation" },
						yaw = new { type = "number", description = "Yaw rotation" },
						roll = new { type = "number", description = "Roll rotation" }
					},
				description = "Rotation of the duplicated GameObject"
			},
			parentId = new
			{
				type = "string",
				description = "Optional parent GameObject ID to set the duplicated GameObject's parent"
			}
		},
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

		var gameObject = SceneEditorSession.Active.Scene.Directory.FindByGuid( guid );
		if ( gameObject == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"GameObject with ID '{id}' not found." } );
			return result;
		}

		await GameTask.MainThread(); // Ensure this runs on the main thread

		GameObject duplicate = null;

		using ( SceneEditorSession.Active.UndoScope( "Duplicate GameObject" )
			       .WithGameObjectCreations().Push() )
		{
			var position = arguments.GetValueOrDefault( "position" ) as JsonElement?;
			var rotation = arguments.GetValueOrDefault( "rotation" ) as JsonElement?;
			var parentId = arguments.GetValueOrDefault( "parentId" ) as string;

			duplicate = gameObject.Clone();

			if ( position.HasValue && position.Value.ValueKind == JsonValueKind.Object )
			{
				var posX = position.Value.GetProperty( "x" ).GetSingle();
				var posY = position.Value.GetProperty( "y" ).GetSingle();
				var posZ = position.Value.GetProperty( "z" ).GetSingle();
				duplicate.WorldPosition = new Vector3( posX, posY, posZ );
			}

			if ( rotation.HasValue && rotation.Value.ValueKind == JsonValueKind.Object )
			{
				var pitch = rotation.Value.GetProperty( "pitch" ).GetSingle();
				var yaw = rotation.Value.GetProperty( "yaw" ).GetSingle();
				var roll = rotation.Value.GetProperty( "roll" ).GetSingle();
				duplicate.WorldRotation = new Angles( pitch, yaw, roll );
			}

			if ( !string.IsNullOrEmpty( parentId ) && Guid.TryParse( parentId, out var parentGuid ) )
			{
				var parentObject = SceneEditorSession.Active.Scene.Directory.FindByGuid( parentGuid );
				if ( parentObject != null )
				{
					duplicate.SetParent( parentObject );
				}
				else
				{
					result.IsError = true;
					result.Content.Add(
						new TextContent { Text = $"Parent GameObject with ID '{parentId}' not found." } );
					return result;
				}
			}
		}

		result.Content.Add( new TextContent { Text = $"Successfully duplicated GameObject: {gameObject.Name}" } );
		result.StructuredContent = new
		{
			duplicatedGameObjectId = duplicate.Id,
			duplicatedGameObjectName = duplicate.Name,
			duplicatedGameObjectPosition =
				new[] { duplicate.WorldPosition.x, duplicate.WorldPosition.y, duplicate.WorldPosition.z },
			duplicatedGameObjectRotation = new[]
			{
				duplicate.WorldRotation.Pitch(), duplicate.WorldRotation.Yaw(), duplicate.WorldRotation.Roll()
			}
		};

		return result;
	}
}
