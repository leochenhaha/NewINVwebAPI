using System; // 註解：Exception
using Microsoft.Extensions.Options; // 註解：IValidateOptions

namespace INVwebAPI.Service; // 註解：命名空間

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions> // 註解：啟動時驗證 JwtOptions
{ // 註解：類別開始
    public ValidateOptionsResult Validate(string? name, JwtOptions options) // 註解：驗證方法
    { // 註解：方法開始
        if (string.IsNullOrWhiteSpace(options.Issuer)) return ValidateOptionsResult.Fail("Jwt:Issuer is required."); // 註解：必填
        if (string.IsNullOrWhiteSpace(options.Audience)) return ValidateOptionsResult.Fail("Jwt:Audience is required."); // 註解：必填
        if (string.IsNullOrWhiteSpace(options.SigningKey)) return ValidateOptionsResult.Fail("Jwt:SigningKey is required."); // 註解：必填
        if (options.AccessTokenMinutes <= 0) return ValidateOptionsResult.Fail("Jwt:AccessTokenMinutes must be > 0."); // 註解：必填
        if (options.RefreshTokenDays <= 0) return ValidateOptionsResult.Fail("Jwt:RefreshTokenDays must be > 0."); // 註解：必填
        return ValidateOptionsResult.Success; // 註解：通過
    } // 註解：方法結束
} // 註解：類別結束
