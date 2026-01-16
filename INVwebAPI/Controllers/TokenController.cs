using System.Threading; // 註解：CancellationToken
using System.Threading.Tasks; // 註解：Task
using INVwebAPI.Common; // 註解：ApiStatusCodeMapper
using INVwebAPI.Dtos.Token; // 註解：DTO
using INVwebAPI.Service; // 註解：Service
using Microsoft.AspNetCore.Authorization; // 註解：Authorize
using Microsoft.AspNetCore.Mvc; // 註解：ControllerBase
namespace INVwebAPI.Controllers; // 註解：命名空間
[ApiController] // 註解：ApiController
[Route("api/token")] // 註解：對齊舊版 RoutePrefix
public sealed class TokenController : ControllerBase // 註解：ControllerBase
{ // 註解：類別開始
    private readonly ITokenService _svc; // 註解：注入 token service
    public TokenController(ITokenService svc) { _svc = svc; } // 註解：建構子

    [AllowAnonymous]
    [HttpPost("SignIn")] // 註解：對齊舊版
    public async Task<IActionResult> SignIn([FromBody] SignInRequestDto req, CancellationToken ct) // 註解：SignIn
    { // 註解：方法開始
        var dto = await _svc.SignInAsync(req, ct); // 註解：呼叫 service
        return StatusCode(ApiStatusCodeMapper.Map(dto.STATUS, dto.MSG), dto); // 註解：統一狀態碼
    } // 註解：方法結束

    [AllowAnonymous]
    [HttpPost("Refresh")] // 註解：對齊舊版
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto req, CancellationToken ct) // 註解：Refresh
    { // 註解：方法開始
        var dto = await _svc.RefreshAsync(req, ct); // 註解：呼叫 service
        return StatusCode(ApiStatusCodeMapper.Map(dto.STATUS, dto.MSG), dto); // 註解：統一狀態碼
    } // 註解：方法結束

    [Authorize] // 註解：需要 access_token
    [HttpPost("IsAuthenticated")] // 註解：對齊舊版
    public async Task<IActionResult> IsAuthenticated(CancellationToken ct) // 註解：IsAuthenticated
    { // 註解：方法開始
        var dto = await _svc.IsAuthenticatedAsync(User, ct); // 註解：呼叫 service
        return StatusCode(dto.STATUS ? 200 : 401, dto); // 註解：這支用 401 表達未授權比較合理
    } // 註解：方法結束
} // 註解：類別結束
