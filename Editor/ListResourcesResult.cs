using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Braxnet;

public class ListResourcesResult
{
	[JsonPropertyName( "resources" )] public List<Resource> Resources { get; set; } = new();

	[JsonPropertyName( "nextCursor" )] public string NextCursor { get; set; }
}
