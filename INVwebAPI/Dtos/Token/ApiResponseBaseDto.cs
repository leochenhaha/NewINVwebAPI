using System.Text.Json.Serialization; // 註解：使用 System.Text.Json 的序列化屬性
namespace INVwebAPI.Dtos.Token; // 註解：命名空間，請依你專案現有 namespace 根調整
public abstract class ApiResponseBaseDto // 註解：所有回應 DTO 的共同欄位
{ // 註解：類別開始
    [JsonPropertyName("STATUS")] public bool STATUS { get; set; } // 註解：對齊舊版回傳欄位 STATUS
    [JsonPropertyName("MSG")] public string MSG { get; set; } = ""; // 註解：對齊舊版回傳欄位 MSG
} // 註解：類別結束
