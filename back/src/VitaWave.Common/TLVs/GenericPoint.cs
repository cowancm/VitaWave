using System.Text.Json.Serialization;

public record GenericPoint
{
    [JsonPropertyName("x")]
    public required double X { get; init; }

    [JsonPropertyName("y")]
    public required double Y { get; init; }

    [JsonPropertyName("z")]
    public double Z { get; init; }
}