using System.Text.Json.Serialization;

namespace Braxnet;

public class RootsCapability
{
	[JsonPropertyName( "listChanged" )] public bool ListChanged { get; set; }
}
