using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Braxnet;

public class ListToolsResult
{
	[JsonPropertyName( "tools" )] public List<Tool> Tools { get; set; } = new();

	[JsonPropertyName( "nextCursor" )] public string NextCursor { get; set; }
}
