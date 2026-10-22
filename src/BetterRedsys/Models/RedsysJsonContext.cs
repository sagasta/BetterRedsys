using System.Text.Json.Serialization;

namespace BetterRedsys.Models;

/// <summary>
/// System.Text.Json Source Generator serialization context for Redsys wire payloads.
/// Guarantees 100% Native AOT and Trimming readiness without runtime reflection.
/// </summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(RedsysMerchantParametersDto))]
[JsonSerializable(typeof(RedsysNotificationParametersDto))]
[JsonSerializable(typeof(RedsysRestRequestDto))]
[JsonSerializable(typeof(RedsysRestResponseDto))]
internal sealed partial class RedsysJsonContext : JsonSerializerContext;
