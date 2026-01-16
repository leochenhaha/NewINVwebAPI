namespace INVwebAPI.Service; // 註解：命名空間

public sealed class JwtOptions // 註解：JWT 設定模型，從 appsettings.json 綁定
{ // 註解：類別開始
    public string Issuer { get; set; } = ""; // 註解：簽發者
    public string Audience { get; set; } = ""; // 註解：受眾
    public string SigningKey { get; set; } = ""; // 註解：簽章密鑰（請放長一點）
    public int AccessTokenMinutes { get; set; } = 30; // 註解：Access token 有效分鐘
    public int RefreshTokenDays { get; set; } = 7; // 註解：Refresh token 有效天數（暫存用）
} // 註解：類別結束
