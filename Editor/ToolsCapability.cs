using System.Text.Json.Serialization;

namespace Braxnet;

public class ToolsCapability
{
	[JsonPropertyName( "listChanged" )] public bool ListChanged { get; set; }
}
