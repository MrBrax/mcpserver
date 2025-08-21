using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

[MCPTool( "asset_info", "Asset Info", "Get information about an asset" )]
public class AssetInfoTool : IMCPTool
{
	public string Name => "asset_info";
	public string Title => "Asset Info";
	public string Description => "Get information about an asset";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new { assetPath = new { type = "string", description = "Path to the asset" }, },
		required = new[] { "assetPath" }
	} );

	public JsonElement OutputSchema => default;

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		if ( arguments == null || !arguments.ContainsKey( "assetPath" ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Asset path is required" } );
			return result;
		}

		var assetPath = arguments["assetPath"].ToString();
		if ( string.IsNullOrEmpty( assetPath ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Asset path cannot be empty" } );
			return result;
		}

		await GameTask.MainThread();

		var asset = AssetSystem.FindByPath( assetPath );
		if ( asset == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Asset not found: {assetPath}" } );
			return result;
		}

		var resource = asset.LoadResource();
		if ( resource == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Failed to load resource for asset: {assetPath}" } );
			return result;
		}

		result.Content.Add( new TextContent { Text = $"Asset Path: {assetPath}" } );

		if ( resource is Model model )
		{
			result.StructuredContent = new
			{
				modelBounds = model.Bounds,
				modelMaterialCount = model.Materials.Length,
				modelMeshCount = model.MeshCount,
				modelRenderBounds = model.RenderBounds,
				modelVertexCount = model.GetVertices().Length,
				physicsBounds = model.PhysicsBounds,
			};
		}

		return result;
	}
}
