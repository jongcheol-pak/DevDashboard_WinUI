using System.Text.Json.Serialization;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// AppSettings 직렬화/역직렬화용 소스 생성기 컨텍스트.
/// 트리밍 환경에서 리플렉션 없이 JSON 처리를 보장합니다.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppJsonContext : JsonSerializerContext;
