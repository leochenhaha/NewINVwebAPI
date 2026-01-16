using INVwebAPI.Dtos.Token; // 註解：TokenDto
using INVwebAPI.Security;
using Microsoft.Extensions.Options; // 註解：IOptions
using Microsoft.IdentityModel.Tokens; // 註解：SymmetricSecurityKey
using System; // 註解：DateTimeOffset
using System.IdentityModel.Tokens.Jwt; // 註解：JwtSecurityTokenHandler
using System.Security.Claims; // 註解：Claim
using System.Security.Cryptography; // 註解：RandomNumberGenerator
using System.Text; // 註解：Encoding

namespace INVwebAPI.Service; // 註解：命名空間
public sealed class JwtTokenService : IJwtTokenService // 註解：JWT 產生器實作
{ // 註解：類別開始
    private readonly JwtOptions _opt; // 註解：JWT 設定
    public JwtTokenService(IOptions<JwtOptions> opt) // 註解：DI 注入設定
    { // 註解：建構子開始
        _opt = opt.Value; // 註解：取得設定值
    } // 註解：建構子結束
    public TokenDto CreateToken(string compNo, string strNo, string passNo) // 註解：建立 token
    { // 註解：方法開始
        if (string.IsNullOrWhiteSpace(_opt.SigningKey)) throw new InvalidOperationException("Jwt:SigningKey is missing or empty."); // 註解：防呆
        var now = DateTimeOffset.UtcNow; // 註解：UTC 現在時間
        var expiresAt = now.AddMinutes(_opt.AccessTokenMinutes); // 註解：access token 到期時間
        var claims = new[]
    {
    new Claim(JwtClaimNames.CompNo, compNo ?? ""),
    new Claim(JwtClaimNames.StrNo, strNo ?? ""),
    new Claim(JwtClaimNames.PassNo, passNo ?? "")
};

        var keyBytes = Encoding.UTF8.GetBytes(_opt.SigningKey); // 註解：簽章 key bytes
        var signingKey = new SymmetricSecurityKey(keyBytes); // 註解：建立驗簽金鑰
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256); // 註解：簽章演算法
        var jwt = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims, now.UtcDateTime, expiresAt.UtcDateTime, creds); // 註解：建立 JWT
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt); // 註解：輸出 token 字串
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // 註解：產生 refresh token
        return new TokenDto // 註解：回傳 TokenDto
        { // 註解：初始化開始
            AccessToken = accessToken, // 註解：access_token
            RefreshToken = refreshToken, // 註解：refresh_token
            TokenType = "Bearer", // 註解：token_type
            ExpiresAt = expiresAt, // 註解：expires_at
            ExpiresIn = (long)Math.Max(0, (expiresAt - now).TotalSeconds) // 註解：expires_in
        }; // 註解：初始化結束
    } // 註解：方法結束
} // 註解：類別結束
