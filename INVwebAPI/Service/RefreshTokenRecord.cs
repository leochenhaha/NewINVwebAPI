using System; // 註解：DateTimeOffset
namespace INVwebAPI.Service; // 註解：命名空間
public sealed class RefreshTokenRecord // 註解：RefreshToken 存放紀錄（先用 in-memory）
{ // 註解：類別開始
    public string RefreshToken { get; set; } = ""; // 註解：refresh token 字串
    public string CompNo { get; set; } = ""; // 註解：公司代號
    public string StrNo { get; set; } = ""; // 註解：店別代號
    public string PassNo { get; set; } = ""; // 註解：使用者代號
    public DateTimeOffset ExpiresAt { get; set; } // 註解：refresh token 到期時間
} // 註解：類別結束
