using System.Text.Json.Serialization;

namespace Braxnet;

public class ResourceContents
{
	[JsonPropertyName( "uri" )] public string Uri { get; set; } = "";

	[JsonPropertyName( "text" )] public string Text { get; set; } = "";

	[JsonPropertyName( "mimeType" )] public string MimeType { get; set; }
}
