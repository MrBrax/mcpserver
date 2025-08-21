// Copyright Braxnet 2025 unless specified otherwise
// https://braxnet.online

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "create_model", "Create Model", "Create a new vmdl model" )]
public class CreateModelTool : IMCPTool
{
	public string Name => "create_model";
	public string Title => "Create Model";
	public string Description => "Create a new vmdl model";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			meshPath = new { type = "string", description = "Path to the mesh file (obj, fbx, etc.)" },
			modelPath = new { type = "string", description = "Path to the vmdl file to create" },
		},
		required = new[] { "meshPath", "modelPath" }
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

		var meshPath = arguments.GetValueOrDefault( "meshPath" )?.ToString();
		var modelPath = arguments.GetValueOrDefault( "modelPath" )?.ToString();

		if ( string.IsNullOrEmpty( meshPath ) || string.IsNullOrEmpty( modelPath ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Both meshPath and modelPath are required" } );
			return result;
		}

		if ( !meshPath.ToLower().EndsWith( ".obj" ) && !meshPath.ToLower().EndsWith( ".fbx" ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent
			{
				Text = "Unsupported mesh file format. Only .obj and .fbx are allowed."
			} );
			return result;
		}

		if ( !modelPath.EndsWith( ".vmdl" ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Model path must end with .vmdl" } );
			return result;
		}

		await GameTask.MainThread();

		var meshAsset = AssetSystem.FindByPath( meshPath );
		if ( meshAsset == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Mesh asset file not found: {meshPath}" } );
			return result;
		}

		var modelAsset = EditorUtility.CreateModelFromMeshFile( meshAsset );

		if ( modelAsset == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Failed to create model from mesh file." } );
			return result;
		}

		result.Content.Add( new TextContent { Text = $"Model created successfully at: {modelPath}" } );

		result.StructuredContent = new Dictionary<string, string>
		{
			{ "modelPath", modelPath }, { "meshPath", meshPath }
		};

		result.IsError = false;

		return result;
	}
}
