using System.Collections.Generic;
using System.Text.Json.Serialization; // 註解：JsonPropertyName

namespace INVwebAPI.Dtos.Inv; // 註解：命名空間

public sealed class UploadB2BDealDataRequestDto // 註解：UploadB2BDealData Request DTO
{
    public string COMP_NO { get; set; } = ""; // 註解：公司別
    public string STR_NO { get; set; } = ""; // 註解：店別
    public string ECR_NO { get; set; } = ""; // 註解：機號

    public string INVOICEDATE { get; set; } = ""; // 註解：交易日（舊系統用字串，再 Convert.ToDateTime）

    public string ORDER_NO { get; set; } = ""; // 註解：訂單號

    public string PrintMark { get; set; } = ""; // 註解：是否列印（Y/N）

    public string INVFLAG { get; set; } = ""; // 註解：MIG_Type（舊碼用 INVFLAG，B 代表 B2B 金額要加稅）

    public string TAX_TYPE { get; set; } = ""; // 註解：稅別

    public decimal STAX_AMT { get; set; } // 註解：應稅銷售額
    public decimal FREE_AMT { get; set; } // 註解：免稅銷售額
    public decimal ZERO_AMT { get; set; } // 註解：零稅銷售額
    public decimal TAX_AMT { get; set; } // 註解：稅額
    public decimal TAXED_AMT { get; set; } // 註解：含稅金額或應稅金額（依你舊欄位語意）

    public string ECP_TYPE { get; set; } = ""; // 註解：載具/電商類型

    public string SEL_IDENTIFIER { get; set; } = ""; // 註解：賣方統編（舊碼 BL_NO）

    public string? Buy_IDENTIFIER { get; set; } // 註解：買方統編（可空）
    public string? Buy_NAME { get; set; } // 註解：買方名稱

    public string? MEM_ADDR { get; set; } // 註解：買方地址
    public string? MEM_EMAIL { get; set; } // 註解：買方 Email
    public string? MEM_MOBILE { get; set; } // 註解：買方手機

    public string? MEMO { get; set; } // 註解：備註

    public string? CARRY_TYPE { get; set; } // 註解：載具類別
    public string? CARRY_ID { get; set; } // 註解：載具號碼
    public string? CARRY_ID2 { get; set; } // 註解：載具第二段（舊碼可能會塞 QR 字串片段）

    public string? LOVE_CODE { get; set; } // 註解：愛心碼

    public List<UploadB2BDealDataDetailDto> Detail { get; set; } = new(); // 註解：明細
}
