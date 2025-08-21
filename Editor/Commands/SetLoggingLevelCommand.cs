// Copyright Braxnet 2025 unless specified otherwise
// https://braxnet.online

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace Braxnet.Commands;

[MCPCommand( "logging/setLevel" )]
public class SetLoggingLevelCommand : IMCPCommand
{
	public string Name => "logging/setLevel";

	public async Task<object> ExecuteAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		if ( request.Params.HasValue )
		{
			var paramsObj = JsonSerializer.Deserialize<Dictionary<string, object>>(
				request.Params.Value.GetRawText(), MCPServer.JsonOptions );

			if ( paramsObj.TryGetValue( "level", out var levelObj ) && levelObj is string levelStr )
			{
				if ( Enum.TryParse<LogLevel>( levelStr, true, out var level ) )
				{
					// Log.SetLevel( level );
					return new { Success = true, Message = $"Logging level set to {level}" };
				}
				else
				{
					return new { Success = false, Message = $"Invalid logging level: {levelStr}" };
				}
			}
			else
			{
				return new { Success = false, Message = "Missing or invalid 'level' parameter" };
			}
		}

		return new { Success = false, Message = "No parameters provided" };
	}
}
