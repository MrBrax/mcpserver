using System.Text.Json.Serialization;

namespace Braxnet;

public class TextContent
{
	[JsonPropertyName( "type" )] public string Type { get; } = "text";

	[JsonPropertyName( "text" )] public string Text { get; set; } = "";
}
