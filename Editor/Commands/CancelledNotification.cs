using System.Text.Json;
using System.Threading.Tasks;

namespace Braxnet.Commands;

[MCPNotification( "notifications/cancelled" )]
public class CancelledNotification : IMCPNotification
{
	public string Name => "notifications/cancelled";

	public async Task HandleAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		Log.Info( $"Received cancellation notification for session {sessionId}" );

		// Extract request ID if provided in params
		if ( request.Params.HasValue )
		{
			try
			{
				var cancelParams = JsonSerializer.Deserialize<CancelledParams>(
					request.Params.Value.GetRawText(), MCPServer.JsonOptions );

				if ( cancelParams?.RequestId != null )
				{
					Log.Info( $"Request {cancelParams.RequestId} was cancelled" );
					// TODO: Implement request cancellation logic if needed
				}
			}
			catch ( JsonException ex )
			{
				Log.Warning( $"Failed to parse cancellation params: {ex.Message}" );
			}
		}

		await Task.CompletedTask;
	}
}