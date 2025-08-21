using System.Threading.Tasks;
using System.Text.Json;

namespace Braxnet.Commands;

/// <summary>
/// Interface for MCP notification handlers
/// </summary>
public interface IMCPNotification
{
	/// <summary>
	/// The name of the notification method this handler processes
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Handle the notification
	/// </summary>
	/// <param name="request">The JSON-RPC request containing the notification</param>
	/// <param name="sessionId">The session ID</param>
	/// <param name="protocolVersion">The protocol version</param>
	/// <returns>Task for async processing</returns>
	Task HandleAsync( JsonRpcRequest request, string sessionId, string protocolVersion );
}
