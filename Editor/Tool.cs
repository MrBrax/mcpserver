using System.Text.Json;
using System.Text.Json.Serialization;

namespace Braxnet;

public class Tool
{
	[JsonPropertyName( "name" )] public string Name { get; set; } = "";

	[JsonPropertyName( "title" )] public string Title { get; set; }

	[JsonPropertyName( "description" )] public string Description { get; set; }

	[JsonPropertyName( "inputSchema" )] public JsonElement InputSchema { get; set; }

	// [JsonPropertyName( "outputSchema" )] public JsonElement OutputSchema { get; set; }
}
