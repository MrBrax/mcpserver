// Copyright Braxnet 2025 unless specified otherwise
// https://braxnet.online

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "cloud_install", "Cloud Install", "Install assets from the cloud" )]
public class CloudInstallTool : IMCPTool
{
	public string Name => "cloud_install";
	public string Title => "Cloud Install";
	public string Description => "Install assets from the cloud";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			packageIdent = new { type = "string", description = "Full ident of the package to install" }
		},
		required = new[] { "packageIdent" }
	} );

	public JsonElement OutputSchema => default;

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		if ( arguments == null || !arguments.ContainsKey( "packageIdent" ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Full package ident is required" } );
			return result;
		}

		var packageIdent = arguments["packageIdent"].ToString();
		if ( string.IsNullOrEmpty( packageIdent ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Package ident cannot be empty" } );
			return result;
		}

		var package = await Package.FetchAsync( packageIdent, false );
		if ( package == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Package not found: {packageIdent}" } );
			return result;
		}

		if ( !AssetSystem.CanCloudInstall( package ) )
		{
			result.IsError = true;
			result.Content.Add(
				new TextContent { Text = $"Package cannot be installed from the cloud: {packageIdent}" } );
			return result;
		}

		if ( AssetSystem.IsCloudInstalled( package ) )
		{
			string assetPath = package.GetMeta<string>( "PrimaryAsset" );
			var asset = AssetSystem.FindByPath( assetPath );
			if ( asset != null )
			{
				result.Content.Add( new TextContent { Text = $"Asset already installed at: {assetPath}" } );

				result.StructuredContent = new
				{
					Asset = new AssetInfo()
					{
						Path = asset.RelativePath,
						RenderMins = package.GetMeta( "RenderMins", "" ),
						RenderMaxs = package.GetMeta( "RenderMaxs", "" ),
						PhysicsMins = package.GetMeta( "PhysicsMins", "" ),
						PhysicsMaxs = package.GetMeta( "PhysicsMaxs", "" ),
					}
				};

				return result;
			}
			else
			{
				result.IsError = true;
				result.Content.Add(
					new TextContent { Text = $"Asset already installed but not found at: {assetPath}" } );
				return result;
			}
		}

		await GameTask.MainThread(); // Ensure this runs on the main thread

		var installedAsset = await AssetSystem.InstallAsync( package.FullIdent );
		if ( installedAsset == null )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = $"Failed to install package: {packageIdent}" } );
			return result;
		}

		result.Content.Add( new TextContent { Text = $"Successfully installed package: {packageIdent}" } );
		result.StructuredContent = new
		{
			Asset = new AssetInfo()
			{
				Path = installedAsset.RelativePath,
				RenderMins = package.GetMeta( "RenderMins", "" ),
				RenderMaxs = package.GetMeta( "RenderMaxs", "" ),
				PhysicsMins = package.GetMeta( "PhysicsMins", "" ),
				PhysicsMaxs = package.GetMeta( "PhysicsMaxs", "" ),
			}
		};

		return result;
	}

	public class AssetInfo
	{
		public string Path { get; set; }
		public string RenderMins { get; set; }
		public string RenderMaxs { get; set; }
		public string PhysicsMins { get; set; }
		public string PhysicsMaxs { get; set; }
	}
}
