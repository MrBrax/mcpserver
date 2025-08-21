using System.Text.Json.Serialization;

namespace Braxnet;

public class PromptsCapability
{
	[JsonPropertyName( "listChanged" )] public bool ListChanged { get; set; }
}
