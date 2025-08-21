using System.Threading.Tasks;

namespace Braxnet.Commands;

[MCPCommand( "ping" )]
public class PingCommand : IMCPCommand
{
	public string Name => "ping";

	public async Task<object> ExecuteAsync( JsonRpcRequest request, string sessionId, string protocolVersion )
	{
		await Task.CompletedTask;
		return new { };
	}
}
