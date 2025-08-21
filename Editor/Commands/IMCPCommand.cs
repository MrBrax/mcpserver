using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace Braxnet.Commands;

public interface IMCPCommand
{
	string Name { get; }
	Task<object> ExecuteAsync( JsonRpcRequest request, string sessionId, string protocolVersion );
}

public interface IMCPTool
{
	string Name { get; }
	string Title { get; }
	string Description { get; }
	JsonElement InputSchema { get; }
	JsonElement OutputSchema { get; }
	Task<CallToolResult> ExecuteAsync( Dictionary<string, object> arguments, string sessionId );
}

[System.AttributeUsage( System.AttributeTargets.Class )]
public class MCPCommandAttribute : System.Attribute
{
	public string Name { get; }

	public MCPCommandAttribute( string name )
	{
		Name = name;
	}
}

[System.AttributeUsage( System.AttributeTargets.Class )]
public class MCPToolAttribute : System.Attribute
{
	public string Name { get; }
	public string Title { get; }
	public string Description { get; }

	public MCPToolAttribute( string name, string title, string description )
	{
		Name = name;
		Title = title;
		Description = description;
	}
}
