using System.Text.Json;
using System.Text.Json.Serialization;

namespace Braxnet;

public class JsonRpcRequest
{
	[JsonPropertyName( "jsonrpc" )] public string JsonRpc { get; set; } = "2.0";

	[JsonPropertyName( "id" )] public object Id { get; set; }

	[JsonPropertyName( "method" )] public string Method { get; set; } = "";

	[JsonPropertyName( "params" )] public JsonElement? Params { get; set; }
}
