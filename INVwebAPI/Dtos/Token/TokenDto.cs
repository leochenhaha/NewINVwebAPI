using System; // 註解：使用 DateTimeOffset
using System.Text.Json.Serialization; // 註解：使用 JsonPropertyName 來對齊欄位名稱
namespace INVwebAPI.Dtos.Token; // 註解：命名空間
public sealed class TokenDto // 註解：對齊舊版 result.Token 的物件結構
{ // 註解：類別開始
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = ""; // 註解：JWT access token
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = ""; // 註解：refresh token
    [JsonPropertyName("token_type")] public string TokenType { get; set; } = "Bearer"; // 註解：token 類型
    [JsonPropertyName("expires_in")] public long ExpiresIn { get; set; } // 註解：剩餘秒數
    [JsonPropertyName("expires_at")] public DateTimeOffset ExpiresAt { get; set; } // 註解：到期時間
} // 註解：類別結束
