using System.Text.Json.Serialization;

namespace Braxnet;

public class InitializeResult
{
	[JsonPropertyName( "protocolVersion" )]
	public string ProtocolVersion { get; set; } = "2025-06-18";

	[JsonPropertyName( "capabilities" )] public ServerCapabilities Capabilities { get; set; } = new();

	[JsonPropertyName( "serverInfo" )] public Implementation ServerInfo { get; set; } = new();

	[JsonPropertyName( "instructions" )] public string Instructions { get; set; }
}
