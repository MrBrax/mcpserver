using System.Text.Json.Serialization;

namespace Braxnet;

public class ResourcesCapability
{
	[JsonPropertyName( "subscribe" )] public bool Subscribe { get; set; }

	[JsonPropertyName( "listChanged" )] public bool ListChanged { get; set; }
}
