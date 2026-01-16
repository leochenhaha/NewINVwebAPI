using System.Text.Json.Serialization; // 註解：使用 JsonPropertyName
namespace INVwebAPI.Dtos.Token; // 註解：命名空間
public sealed class SignInResponseDto : ApiResponseBaseDto // 註解：登入回應 DTO，包含 STATUS/MSG/Token
{ // 註解：類別開始
    [JsonPropertyName("Token")] public TokenDto? Token { get; set; } // 註解：對齊舊版回傳欄位 Token
} // 註解：類別結束
