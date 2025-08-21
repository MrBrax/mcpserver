using System.Text.Json.Serialization;

namespace Braxnet;

public class ClientCapabilities
{
	[JsonPropertyName( "roots" )] public RootsCapability Roots { get; set; }

	[JsonPropertyName( "sampling" )] public SamplingCapability Sampling { get; set; }

	[JsonPropertyName( "elicitation" )] public ElicitationCapability Elicitation { get; set; }
}
