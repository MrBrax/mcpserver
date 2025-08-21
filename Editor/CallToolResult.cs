using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Braxnet;

public class CallToolResult
{
	[JsonPropertyName( "content" )] public List<TextContent> Content { get; set; } = new();

	[JsonPropertyName( "isError" )] public bool IsError { get; set; }

	[JsonPropertyName( "structuredContent" )]
	public object StructuredContent { get; set; }
}
