using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Sandbox;
using Editor;
using FileSystem = Editor.FileSystem;

namespace Braxnet.Commands;

[MCPCommand( "resources/list" )]
public class ListResourcesCommand : IMCPCommand
{
	public string Name => "resources/list";

	public async Task<object> ExecuteAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		var resources = new List<Resource>();

		try
		{
			var projectDir = Path.Combine( FileSystem.Mounted.GetFullPath( "." ), ".." );
			projectDir = Path.GetFullPath( projectDir );

			var files = Directory.GetFiles( projectDir, "*.*", SearchOption.AllDirectories )
				.Where( f => !f.Contains( ".git" ) && !f.Contains( ".vscode" ) && !f.Contains( ".idea" ) )
				.Take( 100 );

			foreach ( var file in files )
			{
				var relativePath = Path.GetRelativePath( projectDir, file );
				var fileInfo = new FileInfo( file );

				resources.Add( new Resource
				{
					Uri = $"file://{file.Replace( '\\', '/' )}",
					Name = relativePath.Replace( '\\', '/' ),
					Title = Path.GetFileName( file ),
					Description = $"Project file: {relativePath}",
					MimeType = MCPServer.GetMimeType( file ),
					Size = fileInfo.Length
				} );
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error listing resources: {ex.Message}" );
		}

		return new ListResourcesResult { Resources = resources };
	}
}

[MCPCommand( "resources/read" )]
public class ReadResourceCommand : IMCPCommand
{
	public string Name => "resources/read";

	public async Task<object> ExecuteAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		var contents = new List<ResourceContents>();

		if ( request.Params.HasValue )
		{
			var paramsObj = JsonSerializer.Deserialize<Dictionary<string, object>>(
				request.Params.Value.GetRawText(), MCPServer.JsonOptions );

			if ( paramsObj?.ContainsKey( "uri" ) == true )
			{
				var uriStr = paramsObj["uri"]?.ToString();
				if ( !string.IsNullOrEmpty( uriStr ) && uriStr.StartsWith( "file://" ) )
				{
					var filePath = uriStr.Substring( 7 );
					try
					{
						if ( File.Exists( filePath ) )
						{
							var text = await File.ReadAllTextAsync( filePath );
							contents.Add( new ResourceContents
							{
								Uri = uriStr, Text = text, MimeType = MCPServer.GetMimeType( filePath )
							} );
						}
					}
					catch ( Exception ex )
					{
						Log.Error( $"Error reading file {filePath}: {ex.Message}" );
						throw new Exception( $"Could not read file: {ex.Message}" );
					}
				}
			}
		}

		return new ReadResourceResult { Contents = contents };
	}
}
