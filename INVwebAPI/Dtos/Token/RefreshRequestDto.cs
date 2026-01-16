using System.Text.Json.Serialization; // 註解：JsonPropertyName
namespace INVwebAPI.Dtos.Token; // 註解：命名空間
public sealed class RefreshRequestDto // 註解：Refresh 請求 DTO
{ // 註解：類別開始
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; } // 註解：對齊舊版 jsonResult["refresh_token"]
} // 註解：類別結束
