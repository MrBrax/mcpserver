using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Braxnet;

public class ReadResourceResult
{
	[JsonPropertyName( "contents" )] public List<ResourceContents> Contents { get; set; } = new();
}
