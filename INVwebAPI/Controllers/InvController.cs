using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; // 每行註解：ASP.NET Core MVC
using INVwebAPI.Dtos.Inv;     // 每行註解：DTO 引用
using INVwebAPI.Service; // 每行註解：Service 介面引用

namespace INVwebAPI.Controllers
{
    [ApiController] // 每行註解：啟用 ApiController 行為（自動模型綁定、400 等）
    [Route("api/INV")] // 每行註解：對齊舊版 [RoutePrefix("api/INV")]
    public sealed class InvController : ControllerBase // 每行註解：使用 ControllerBase，不做 View
    {
        private readonly IInvService _invService; // 每行註解：注入 Service

        public InvController(IInvService invService) // 每行註解：建構子注入
        {
            _invService = invService; // 每行註解：保存注入的 service
        }

        [HttpPost("GetINVnumber")] // 每行註解：對齊舊版 [Route("GetINVnumber")]
        public async Task<ActionResult<GetInvNumberResponseDto>> GetINVnumber([FromBody] GetInvNumberRequestDto request) // 每行註解：只接 DTO
        {
            var response = await _invService.GetInvNumberAsync(request); // 每行註解：交給 service 做所有事

            if (response.STATUS) // 每行註解：成功時維持 200
            {
                return Ok(response); // 每行註解：回 200 + DTO
            }

            // 每行註解：舊版有用 417 ExpectationFailed，我們保留語意（你若想統一 200 也能改）
            // 每行註解：這裡先用 417，讓行為接近舊系統（錯誤時不是 500 ）
            return StatusCode(StatusCodes.Status417ExpectationFailed, response); // 每行註解：回 417 + DTO
        }

        [HttpPost("UploadB2BDealData")] // 每行註解：對齊舊版 Route("UploadB2BDealData")
        public async Task<ActionResult<UploadB2BDealDataResponseDto>> UploadB2BDealData([FromBody] UploadB2BDealDataRequestDto request)
        {
            var response = await _invService.UploadB2BDealDataAsync(request);

            if (response.STATUS)
            {
                return Ok(response);
            }

            if (string.Equals(response.ErrorCode, "EXPECTATION_FAILED", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status417ExpectationFailed, response);
            }

            return BadRequest(response);
        }
    }
}
