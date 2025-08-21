using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "get_game_objects", "List Game Objects",
	"List all GameObjects in the current scene" )]
public class ListGameObjectsTool : IMCPTool
{
	public string Name => "get_game_objects";
	public string Title => "List Game Objects";
	public string Description => "List all GameObjects in the current scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object", properties = new { }, required = Array.Empty<string>()
	} );

	public JsonElement OutputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			SceneFile = new { type = "string", description = "Path to the scene file" },
			GameObjects = new
			{
				type = "array",
				items = new
				{
					type = "object",
					properties = new
					{
						Id = new { type = "string", description = "GameObject ID" },
						Name = new { type = "string", description = "GameObject name" },
						Position =
							new
							{
								type = "object",
								properties =
									new
									{
										x = new { type = "number" },
										y = new { type = "number" },
										z = new { type = "number" }
									}
							},
						Rotation =
							new
							{
								type = "object",
								properties =
									new
									{
										pitch = new { type = "number" },
										yaw = new { type = "number" },
										roll = new { type = "number" }
									}
							},
						Components = new { type = "integer", description = "Number of components" },
						ChildrenCount =
							new { type = "integer", description = "Number of child GameObjects" },
						Enabled =
							new { type = "boolean", description = "Is the GameObject enabled?" },
						Tags = new
						{
							type = "array",
							items = new { type = "string" },
							description = "List of tags assigned to the GameObject"
						}
					},
					required = new[]
					{
						"Id", "Name", "Position", "Rotation", "Components", "ChildrenCount", "Enabled", "Tags"
					}
				}
			}
		},
		required = new[] { "SceneFile", "GameObjects" }
	} );

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		try
		{
			var rootGameObjects = SceneEditorSession.Active.Scene.Children;
			if ( rootGameObjects == null || rootGameObjects.Count == 0 )
			{
				result.Content.Add( new TextContent { Text = "No GameObjects found in the scene." } );
				return result;
			}

			result.Content.Add( new TextContent
			{
				Text = $"Found {rootGameObjects.Count} root GameObjects in the scene."
			} );

			var gameObjectsList = new List<GameObjectInfo>();
			foreach ( var gameObject in rootGameObjects )
			{
				gameObjectsList.Add( new GameObjectInfo
				{
					Id = gameObject.Id,
					Name = gameObject.Name,
					Position = gameObject.WorldPosition,
					Rotation = gameObject.WorldRotation,
					Components = gameObject.Components.Count,
					ChildrenCount = gameObject.Children.Count,
					Enabled = gameObject.Enabled,
					Tags = gameObject.Tags.ToList(),
				} );
			}

			result.StructuredContent = new
			{
				SceneFile = SceneEditorSession.Active.Scene.Source.ResourcePath, GameObjects = gameObjectsList
			};
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error listing GameObjects: {ex.Message}" } );
		}

		return result;
	}

	public class GameObjectInfo
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public Vector3 Position { get; set; }
		public Angles Rotation { get; set; }
		public int Components { get; set; }
		public int ChildrenCount { get; set; }
		public bool Enabled { get; set; }
		public List<string> Tags { get; set; }
	}
}
