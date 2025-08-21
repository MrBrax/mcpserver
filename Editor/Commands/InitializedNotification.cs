using System.Threading.Tasks;
using Sandbox;

namespace Braxnet.Commands;

[MCPNotification( "notifications/initialized" )]
public class InitializedNotification : IMCPNotification
{
	public string Name => "notifications/initialized";

	public async Task HandleAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		Log.Info( $"Client initialized notification received for session {sessionId}" );

		// Mark session as fully initialized
		MCPServer.MarkSessionInitialized( sessionId );

		// Log protocol version for debugging
		Log.Info( $"Session {sessionId} initialized with protocol version {protocolVersion}" );

		await Task.CompletedTask;
	}
}
