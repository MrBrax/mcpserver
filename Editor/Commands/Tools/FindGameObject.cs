// Copyright Braxnet 2025 unless specified otherwise
// https://braxnet.online

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "find_gameobject", "Find GameObject", "Find a GameObject in the scene" )]
public class FindGameObject : IMCPTool
{
	public string Name => "find_gameobject";
	public string Title => "Find GameObject";
	public string Description => "Find a GameObject in the scene";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new { name = new { type = "string", description = "Name of the GameObject to find" } },
		required = new[] { "name" }
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

		if ( string.IsNullOrEmpty( name ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Name is required" } );
			return result;
		}

		var gameObjects = SceneEditorSession.Active.Scene.Directory.FindByName( name ).ToList();
		if ( !gameObjects.Any() )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"No GameObjects found with name '{name}'." } );
			return result;
		}

		await GameTask.MainThread(); // Ensure this runs on the main thread

		var foundObjects = gameObjects.ToList().Select( go => new ListGameObjectsTool.GameObjectInfo()
		{
			Id = go.Id, Name = go.Name, Position = go.WorldPosition, Rotation = go.WorldRotation,
		} ).ToList();

		result.Content.Add( new TextContent { Text = $"Found {foundObjects.Count} GameObjects with name '{name}'." } );

		result.StructuredContent = new
		{
			SceneFile = SceneEditorSession.Active.Scene.Source.ResourcePath, GameObjects = foundObjects
		};

		return result;
	}
}
