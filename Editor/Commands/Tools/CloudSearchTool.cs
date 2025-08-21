// Copyright Braxnet 2025 unless specified otherwise
// https://braxnet.online

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace Braxnet.Commands.Tools;

[MCPTool( "cloud_search", "Cloud Search", "Search for assets in the cloud" )]
public class CloudSearchTool : IMCPTool
{
	public string Name => "cloud_search";
	public string Title => "Cloud Search";
	public string Description => "Search for assets in the cloud";

	public JsonElement InputSchema => JsonSerializer.SerializeToElement( new
	{
		type = "object",
		properties = new
		{
			query = new { type = "string", description = "Search query" },
			page = new { type = "integer", description = "Page number for results" },
			pageSize = new { type = "integer", description = "Number of results per page" }
		},
		required = new[] { "query" }
	} );

	public JsonElement OutputSchema => default;

	public async Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId )
	{
		var result = new CallToolResult();

		if ( arguments == null || !arguments.ContainsKey( "query" ) )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "Query is required" } );
			return result;
		}

		var query = arguments["query"].ToString();

		if ( !int.TryParse( arguments.GetValueOrDefault( "page", 1 )?.ToString(), out var page ) )
		{
			page = 1;
		}

		if ( !int.TryParse( arguments.GetValueOrDefault( "pageSize", 10 )?.ToString(), out var pageSize ) )
		{
			pageSize = 10;
		}

		if ( page < 1 ) page = 1;
		if ( pageSize < 1 ) pageSize = 10;

		var skip = (page - 1) * pageSize;
		var take = pageSize;

		var findResult = await Package.FindAsync( query, take, skip );

		if ( findResult == null || findResult.TotalCount == 0 )
		{
			result.IsError = true;
			result.Content.Add( new TextContent { Text = "No results found" } );
			return result;
		}

		result.Content.Add( new TextContent { Text = $"Found {findResult.TotalCount} results for '{query}'" } );

		var packagesList = new List<PackageInfo>();
		foreach ( var package in findResult.Packages )
		{
			var packageInfo = new PackageInfo()
			{
				Ident = package.FullIdent,
				Title = package.Title,
				Description = package.Description,
				Size = package.FileSize,
				Type = package.TypeName,
				Tags = package.Tags.ToList(),
				Thumbnail = package.Thumb,
				Created = package.Created.ToString( "yyyy-MM-dd HH:mm:ss" )
			};

			packagesList.Add( packageInfo );
		}

		result.StructuredContent = new
		{
			packages = packagesList, totalCount = findResult.TotalCount, page = page, pageSize = pageSize
		};

		return result;
	}

	public class PackageInfo
	{
		public string Ident { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public float Size { get; set; }
		public string Type { get; set; }
		public List<string> Tags { get; set; }
		public string Thumbnail { get; set; }
		public string Created { get; set; }
	}
}
