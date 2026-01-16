using System.Security.Claims; // 註解：ClaimsPrincipal

namespace INVwebAPI.Security; // 註解：命名空間

public static class ClaimsPrincipalExtensions // 註解：ClaimsPrincipal 擴充方法
{ // 註解：類別開始
    public static string GetCompNo(this ClaimsPrincipal user) // 註解：取 COMP_NO
    { // 註解：方法開始
        return user?.FindFirst(JwtClaimNames.CompNo)?.Value ?? ""; // 註解：找不到回空字串
    } // 註解：方法結束

    public static string GetStrNo(this ClaimsPrincipal user) // 註解：取 STR_NO
    { // 註解：方法開始
        return user?.FindFirst(JwtClaimNames.StrNo)?.Value ?? ""; // 註解：找不到回空字串
    } // 註解：方法結束

    public static string GetPassNo(this ClaimsPrincipal user) // 註解：取 PASS_NO
    { // 註解：方法開始
        return user?.FindFirst(JwtClaimNames.PassNo)?.Value ?? ""; // 註解：找不到回空字串
    } // 註解：方法結束
} // 註解：類別結束
