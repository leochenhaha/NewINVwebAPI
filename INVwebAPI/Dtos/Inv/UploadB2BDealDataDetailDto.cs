namespace INVwebAPI.Dtos.Inv; // 註解：命名空間

public sealed class UploadB2BDealDataDetailDto // 註解：明細 DTO
{
    public string ITEM_NO { get; set; } = ""; // 註解：項次（舊碼 Convert.ToDecimal）
    public string GOO_NA { get; set; } = ""; // 註解：品名
    public decimal QTY { get; set; } // 註解：數量
    public decimal SPRICE { get; set; } // 註解：單價
    public decimal AMT { get; set; } // 註解：金額
    public decimal TAX { get; set; } // 註解：稅額
}
