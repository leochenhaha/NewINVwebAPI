using System.Text.Json.Serialization; // 註解：JsonPropertyName

namespace INVwebAPI.Dtos.Inv; // 註解：命名空間

public sealed class UploadB2BDealDataResponseDto // 註解：Response DTO（統一格式）
{
    public bool STATUS { get; set; } // 註解：成功與否
    public string MSG { get; set; } = ""; // 註解：訊息

    [JsonPropertyName("發票號碼")] // 註解：對齊舊回傳欄位
    public string? InvoiceNoZh { get; set; } // 註解：中文欄位：發票號碼

    public string? Random_Code { get; set; } // 註解：對齊舊回傳欄位 Random_Code

    [JsonPropertyName("隨機碼")] // 註解：舊成功含 PDF 分支用「隨機碼」
    public string? RandomCodeZh { get; set; } // 註解：中文欄位：隨機碼

    [JsonPropertyName("A4證明聯")] // 註解：對齊舊回傳欄位
    public string? A4PdfUrlZh { get; set; } // 註解：A4 證明聯 URL

    public string? ErrorCode { get; set; } // 註解：讓 Controller 決定回 400 或 417（不影響舊欄位）
}
