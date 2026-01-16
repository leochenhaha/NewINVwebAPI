using System.Text.Json.Serialization; // 註解：JsonPropertyName
namespace INVwebAPI.Dtos.Token; // 註解：命名空間
public sealed class RefreshResponseDto : ApiResponseBaseDto // 註解：Refresh 回應 DTO
{ // 註解：類別開始
    [JsonPropertyName("Token")] public TokenDto? Token { get; set; } // 註解：對齊舊版回傳 Token 物件
} // 註解：類別結束
