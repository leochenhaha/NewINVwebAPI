using INVwebAPI.Dtos.File;
using INVwebAPI.Service;
using Microsoft.AspNetCore.Mvc;

namespace INVwebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class FileController : ControllerBase
    {
        private readonly FileService _fileService;

        public FileController(FileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("UploadLogo")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<UploadLogoResponseDto>> UploadLogo([FromForm] UploadLogoRequestDto request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var msg = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "參數驗證失敗";

                return BadRequest(new UploadLogoResponseDto
                {
                    STATUS = false,
                    MSG = msg
                });
            }

            var result = await _fileService.UploadLogoAsync(request, ct);

            if (!result.STATUS)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
