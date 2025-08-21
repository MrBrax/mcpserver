using System.Text.Json.Serialization;

namespace Braxnet;

public class ServerCapabilities
{
	[JsonPropertyName( "resources" )] public ResourcesCapability Resources { get; set; }

	[JsonPropertyName( "tools" )] public ToolsCapability Tools { get; set; }

	[JsonPropertyName( "prompts" )] public PromptsCapability Prompts { get; set; }

	[JsonPropertyName( "logging" )] public LoggingCapability Logging { get; set; }

	[JsonPropertyName( "completions" )] public CompletionsCapability Completions { get; set; }
}
