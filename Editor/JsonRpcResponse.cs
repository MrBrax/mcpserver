using System.Text.Json.Serialization;

namespace Braxnet;

public class JsonRpcResponse
{
	[JsonPropertyName( "jsonrpc" )] public string JsonRpc { get; set; } = "2.0";

	[JsonPropertyName( "id" )] public object Id { get; set; }

	[JsonPropertyName( "result" )] public object Result { get; set; }

	[JsonPropertyName( "error" )] public JsonRpcError Error { get; set; }
}
