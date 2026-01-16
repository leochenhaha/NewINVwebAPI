using System.Text.Json.Serialization; // 註解：使用 JsonPropertyName 讓輸入欄位對齊舊版 ID/KEY
namespace INVwebAPI.Dtos.Token; // 註解：命名空間
public sealed class SignInRequestDto // 註解：登入請求 DTO，對齊舊版 SignInViewModel
{ // 註解：類別開始
    [JsonPropertyName("ID")] public string? ID { get; set; } // 註解：對齊舊版 model.ID
    [JsonPropertyName("KEY")] public string? KEY { get; set; } // 註解：對齊舊版 model.KEY（密碼）
} // 註解：類別結束
