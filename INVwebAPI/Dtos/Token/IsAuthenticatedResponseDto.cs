using System.Text.Json.Serialization; // 註解：JsonPropertyName
namespace INVwebAPI.Dtos.Token; // 註解：命名空間
public sealed class IsAuthenticatedResponseDto : ApiResponseBaseDto // 註解：驗證回應 DTO（統一格式）
{ // 註解：類別開始
    [JsonPropertyName("IsAuthenticated")] public bool IsAuthenticated { get; set; } // 註解：讓行為更像舊版 bool，但仍符合統一回傳格式
    public string? PASS_NO { get; set; } // 註解：回傳目前 token 內的使用者代號，方便驗證

} // 註解：類別結束
