using System.Threading.Tasks;
using System.Text.Json;
using Sandbox;

namespace Braxnet.Commands;

[MCPNotification( "notifications/progress" )]
public class ProgressNotification : IMCPNotification
{
    public string Name => "notifications/progress";

    public async Task HandleAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
    {
        Log.Info( $"Received progress notification for session {sessionId}" );

        if ( request.Params.HasValue )
        {
            try
            {
                var progressParams = JsonSerializer.Deserialize<ProgressParams>(
                    request.Params.Value.GetRawText(), MCPServer.JsonOptions );

                if ( progressParams != null )
                {
                    Log.Info( $"Progress for request {progressParams.ProgressToken}: {progressParams.Progress}% - {progressParams.Total}" );
                }
            }
            catch ( JsonException ex )
            {
                Log.Warning( $"Failed to parse progress params: {ex.Message}" );
            }
        }

        await Task.CompletedTask;
    }
}

public class ProgressParams
{
    public object ProgressToken { get; set; }
    public int Progress { get; set; }
    public int Total { get; set; }
}
