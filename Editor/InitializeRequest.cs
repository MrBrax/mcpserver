using System.Text.Json.Serialization;

namespace Braxnet;

public class InitializeRequest
{
	[JsonPropertyName( "protocolVersion" )]
	public string ProtocolVersion { get; set; } = "";

	[JsonPropertyName( "capabilities" )] public ClientCapabilities Capabilities { get; set; } = new();

	[JsonPropertyName( "clientInfo" )] public Implementation ClientInfo { get; set; } = new();
}
