using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "create_gameobject", "Create GameObject", "Create a new GameObject in the scene" )]
public class CreateGameObjectTool : IMCPTool
{
	public string Name => "create_gameobject";
	public string Title => "Create GameObject";
	public string Description => "Create a new GameObject in the scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			name = new { type = "string", description = "Name of the GameObject" },
			position = new
			{
				type = "array",
				items = new { type = "number" },
				description = "Vector3 position in world space, Z-up"
			},
			rotation = new
			{
				type = "array",
				items = new { type = "number" },
				description = "Angles rotation in world space"
			},
		},
		required = new[] { "name", "position", "rotation" }
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

		var name = arguments.GetValueOrDefault( "name" )?.ToString();
		var positionArray = arguments.GetValueOrDefault( "position" ) as JsonElement?;
		var rotationArray = arguments.GetValueOrDefault( "rotation" ) as JsonElement?;

		if ( string.IsNullOrEmpty( name ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Name is required" } );
			return result;
		}

		try
		{
			Vector3 position = Vector3.Zero;
			Angles rotation = Angles.Zero;

			// Parse position
			if ( positionArray.HasValue && positionArray.Value.ValueKind == JsonValueKind.Array )
			{
				var posArray = positionArray.Value.EnumerateArray().Select( x => x.GetSingle() ).ToArray();
				if ( posArray.Length >= 3 )
				{
					position = new Vector3( posArray[0], posArray[1], posArray[2] );
				}
			}

			// Parse rotation
			if ( rotationArray.HasValue && rotationArray.Value.ValueKind == JsonValueKind.Array )
			{
				var rotArray = rotationArray.Value.EnumerateArray().Select( x => x.GetSingle() ).ToArray();
				if ( rotArray.Length >= 3 )
				{
					rotation = new Angles( rotArray[0], rotArray[1], rotArray[2] );
				}
			}

			await GameTask.MainThread(); // Ensure this runs on the main thread

			GameObject gameObject;

			using ( SceneEditorSession.Active.UndoScope( "Create Empty" ).WithGameObjectCreations().Push() )
			{
				gameObject = SceneEditorSession.Active.Scene.CreateObject();
				gameObject.Name = name;
				gameObject.WorldPosition = position;
				gameObject.WorldRotation = rotation;
			}

			result.Content.Add( new TextContent { Text = $"Successfully created GameObject: {name}" } );
			result.StructuredContent = new
			{
				gameObjectId = gameObject.Id,
				name = gameObject.Name,
				position =
					new[] { gameObject.WorldPosition.x, gameObject.WorldPosition.y, gameObject.WorldPosition.z },
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
			result.Content.Add( new TextContent { Text = $"Error creating GameObject: {ex.Message}" } );
		}

		return result;
	}
}

/*[MCPTool( "enter_play_mode", "Enter Play Mode",
	"Enter the play mode of the scene" )]
public class EnterPlayModeTool : IMCPTool
{
	public string Name => "enter_play_mode";
	public string Title => "Enter Play Mode";
	public string Description => "Enter the play mode of the scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object", properties = new { }, required = Array.Empty<string>()
	} );

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		EditorScene.Play();

		result.Content.Add( new TextContent { Text = "Entered play mode" } );
		result.StructuredContent = new { status = "play_mode", message = "Entered play mode successfully" };

		return result;
	}
}*/

/*[MCPTool( "take_screenshot", "Take Screenshot",
	"Take a screenshot of the current scene" )]
public class TakeScreenshotTool : IMCPTool
{
	public string Name => "take_screenshot";
	public string Title => "Take Screenshot";
	public string Description => "Take a screenshot of the current scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object", properties = new { }, required = Array.Empty<string>()
	} );

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		try
		{
			if ( Gizmo.Camera == null )
			{
				result.IsError = true;
				result.Content.Add( new TextContent { Text = "No active camera found" } );
				return result;
			}

			var pixmap = new Pixmap( (int)Screen.Width, (int)Screen.Height );
			Gizmo.Camera.RenderToPixmap( pixmap );

			// store the screenshot in a temporary file
			var tempFilePath = Path.Combine( FileSystem.Temporary.GetFullPath( "." ),
				$"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png" );
			pixmap.SaveJpg( tempFilePath, 90 );

			result.Content.Add( new TextContent { Text = "Screenshot taken successfully" } );
			result.StructuredContent = new { filePath = tempFilePath, message = "Screenshot saved to temporary file" };
		}
		catch ( Exception ex )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Error taking screenshot: {ex.Message}" } );
		}

		return result;
	}
}*/
