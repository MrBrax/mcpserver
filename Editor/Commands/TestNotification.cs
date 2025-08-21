using System;
using System.Threading.Tasks;
using System.Text.Json;
using Sandbox;
using Braxnet.Commands;

namespace Braxnet.Tests;

/// <summary>
/// Test notification handler for debugging and validation
/// </summary>
[MCPNotification( "test/notification" )]
public class TestNotification : IMCPNotification
{
    public string Name => "test/notification";

    public async Task HandleAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
    {
        Log.Info( $"Test notification received for session {sessionId}" );
        Log.Info( $"Protocol version: {protocolVersion}" );

        if ( request.Params.HasValue )
        {
            Log.Info( $"Test notification params: {request.Params.Value.GetRawText()}" );
        }

        await Task.CompletedTask;
    }
}
