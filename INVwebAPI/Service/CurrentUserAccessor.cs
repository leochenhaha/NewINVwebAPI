using INVwebAPI.Security; // 註解：CurrentUserContext
using Microsoft.AspNetCore.Http; // 註解：IHttpContextAccessor

namespace INVwebAPI.Service; // 註解：命名空間

public sealed class CurrentUserAccessor : ICurrentUserAccessor // 註解：目前登入者資訊的實作
{ // 註解：類別開始
 private readonly IHttpContextAccessor _http; // 註解：HttpContext 存取器

 public CurrentUserAccessor(IHttpContextAccessor http) // 註解：DI 建構子
 { // 註解：建構子開始
 _http = http; // 註解：注入
 } // 註解：建構子結束

 public CurrentUserContext Get() // 註解：取得目前登入者資訊
 { // 註解：方法開始
 var user = _http.HttpContext?.User; // 註解：拿 ClaimsPrincipal
 var ok = user?.Identity?.IsAuthenticated == true; // 註解：是否已驗證

 return new CurrentUserContext // 註解：回傳 Context
 { // 註解：初始化開始
 IsAuthenticated = ok, // 註解：驗證狀態
 CompNo = ok ? (user!.GetCompNo()) : "", // 註解：COMP_NO
 StrNo = ok ? (user!.GetStrNo()) : "", // 註解：STR_NO
 PassNo = ok ? (user!.GetPassNo()) : "" // 註解：PASS_NO
 }; // 註解：初始化結束
 } // 註解：方法結束
} // 註解：類別結束
