using INVwebAPI.Data.Db; // 註解：DbContext
using INVwebAPI.Dtos.Token; // 註解：DTO
using INVwebAPI.Security;
using Microsoft.EntityFrameworkCore; // 註解：EF Core async
using Microsoft.Extensions.Logging; // 註解：ILogger
using System; // 註解：Exception、DateTimeOffset
using System.Security.Claims; // 註解：ClaimsPrincipal
using System.Security.Cryptography; // 註解：CryptographicOperations、SHA256CryptoServiceProvider
using System.Text; // 註解：Encoding
using System.Threading; // 註解：CancellationToken
using System.Threading.Tasks; // 註解：Task

namespace INVwebAPI.Service; // 註解：命名空間
public sealed class TokenService : ITokenService // 註解：Token 服務
{ // 註解：類別開始
    private readonly EINV_WEBContext _db; // 註解：DbContext
    private readonly ILogger<TokenService> _logger; // 註解：logger
    private readonly IJwtTokenService _jwt; // 註解：JWT 產生器
    private readonly IRefreshTokenStore _store; // 註解：refresh token store
    private readonly JwtOptions _opt; // 註解：JWT 設定（只用來算 refresh token 到期）
    public TokenService(EINV_WEBContext db, ILogger<TokenService> logger, IJwtTokenService jwt, IRefreshTokenStore store, Microsoft.Extensions.Options.IOptions<JwtOptions> opt) // 註解：DI 建構子
    { // 註解：建構子開始
        _db = db; // 註解：注入 DbContext
        _logger = logger; // 註解：注入 logger
        _jwt = jwt; // 註解：注入 jwt service
        _store = store; // 註解：注入 store
        _opt = opt.Value; // 註解：注入 options
    } // 註解：建構子結束

    public async Task<SignInResponseDto> SignInAsync(SignInRequestDto request, CancellationToken ct) // 註解：登入
    { // 註解：方法開始
        try // 註解：catch 住避免預設 500
        { // 註解：try 開始
            var id = (request.ID ?? "").Trim(); // 註解：ID
            var key = request.KEY ?? ""; // 註解：KEY
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(key)) return new SignInResponseDto { STATUS = false, MSG = "登入失敗，帳號或密碼錯誤:", Token = null }; // 註解：基本檢查
            _logger.LogInformation("登入:{Id}", id); // 註解：記錄登入帳號
            var sys = await _db.SYSPASMI.FirstOrDefaultAsync(x => x.pass_no == id, ct); // 註解：查 SYSPASMI
            if (sys == null) return new SignInResponseDto { STATUS = false, MSG = "登入失敗，帳號不存在或密碼錯誤", Token = null }; // 註解：帳號不存在
            var salt = sys.pass_salt ?? ""; // 註解：salt
            var stored = (sys.pass_wd ?? "").Trim(); // 註解：DB hash（Base64）
            var computed = EncryptSha256Base64Legacy(salt + key); // 註解：對齊舊版 TokenCrypto.EncryptSHA256
            var ok = FixedTimeEqualsBase64(computed, stored); // 註解：固定時間比對
            if (!ok) { _logger.LogWarning("登入失敗，帳號或密碼錯誤:{Id}", id); return new SignInResponseDto { STATUS = false, MSG = "登入失敗，帳號或密碼錯誤:", Token = null }; } // 註解：帳密錯
            var token = _jwt.CreateToken(sys.comp_no ?? "", sys.str_no ?? "", id); // 註解：產 token
            _store.Upsert(new RefreshTokenRecord { RefreshToken = token.RefreshToken, CompNo = sys.comp_no ?? "", StrNo = sys.str_no ?? "", PassNo = id, ExpiresAt = DateTimeOffset.UtcNow.AddDays(_opt.RefreshTokenDays) }); // 註解：存 refresh token
            return new SignInResponseDto { STATUS = true, MSG = "成功", Token = token }; // 註解：成功
        } // 註解：try 結束
        catch (Exception ex) // 註解：例外
        { // 註解：catch 開始
            _logger.LogError(ex, "SignIn 發生例外"); // 註解：log
            return new SignInResponseDto { STATUS = false, MSG = "登入失敗:" + ex.Message, Token = null }; // 註解：統一格式
        } // 註解：catch 結束
    } // 註解：方法結束

    public Task<RefreshResponseDto> RefreshAsync(RefreshRequestDto request, CancellationToken ct) // 註解：Refresh
    { // 註解：方法開始
        try // 註解：try
        { // 註解：try 開始
            (_store as IRefreshTokenCleanup)?.CleanupExpired();
            var rt = request.RefreshToken ?? ""; // 註解：refresh token
            if (string.IsNullOrWhiteSpace(rt)) return Task.FromResult(new RefreshResponseDto { STATUS = false, MSG = "查無此 Refresh Token", Token = null }); // 註解：空值視為不存在
            if (!_store.TryGet(rt, out var rec)) return Task.FromResult(new RefreshResponseDto { STATUS = false, MSG = "查無此 Refresh Token", Token = null }); // 註解：不存在
            if (rec.ExpiresAt <= DateTimeOffset.UtcNow) { _store.Remove(rt); return Task.FromResult(new RefreshResponseDto { STATUS = false, MSG = "查無此 Refresh Token", Token = null }); } // 註解：過期當不存在
            _store.Remove(rt); // 註解：刪除舊的（對齊舊版 refreshTokens.Remove）
            var token = _jwt.CreateToken(rec.CompNo, rec.StrNo, rec.PassNo); // 註解：產新 token
            _store.Upsert(new RefreshTokenRecord { RefreshToken = token.RefreshToken, CompNo = rec.CompNo, StrNo = rec.StrNo, PassNo = rec.PassNo, ExpiresAt = DateTimeOffset.UtcNow.AddDays(_opt.RefreshTokenDays) }); // 註解：存新的
            _logger.LogInformation("換取新 Token:{PassNo}", rec.PassNo); // 註解：log
            return Task.FromResult(new RefreshResponseDto { STATUS = true, MSG = "成功", Token = token }); // 註解：成功回傳
        } // 註解：try 結束
        catch (Exception ex) // 註解：例外
        { // 註解：catch 開始
            _logger.LogError(ex, "Refresh 發生例外"); // 註解：log
            return Task.FromResult(new RefreshResponseDto { STATUS = false, MSG = "Refresh失敗:" + ex.Message, Token = null }); // 註解：統一格式
        } // 註解：catch 結束
    } // 註解：方法結束

    public Task<IsAuthenticatedResponseDto> IsAuthenticatedAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var ok = user?.Identity?.IsAuthenticated == true;

        if (!ok)
        {
            return Task.FromResult(new IsAuthenticatedResponseDto
            {
                STATUS = false,
                MSG = "未通過驗證",
                IsAuthenticated = false
            });
        }

        return Task.FromResult(new IsAuthenticatedResponseDto
        {
            STATUS = true,
            MSG = "成功",
            IsAuthenticated = true,
            PASS_NO = user.GetPassNo()
        });
    }


    private static string EncryptSha256Base64Legacy(string original) // 註解：對齊舊版 SHA256 Base64
    { // 註解：方法開始
        using var sha256 = new SHA256CryptoServiceProvider(); // 註解：舊版用 SHA256CryptoServiceProvider
        var bytes = Encoding.Default.GetBytes(original); // 註解：舊版用 Encoding.Default
        var hash = sha256.ComputeHash(bytes); // 註解：ComputeHash
        return Convert.ToBase64String(hash); // 註解：輸出 Base64
    } // 註解：方法結束

    private static bool FixedTimeEqualsBase64(string a, string b) // 註解：固定時間比對
    { // 註解：方法開始
        try { var ba = Convert.FromBase64String(a ?? ""); var bb = Convert.FromBase64String(b ?? ""); return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb); } // 註解：Base64 bytes 比對
        catch { return string.Equals(a, b, StringComparison.Ordinal); } // 註解：非 Base64 則回退字串比對
    } // 註解：方法結束
} // 註解：類別結束
