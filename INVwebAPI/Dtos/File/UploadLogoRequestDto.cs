using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace INVwebAPI.Dtos.File
{
    public sealed class UploadLogoRequestDto
    {
        [Required(ErrorMessage = "COMP_NO 參數為必填")]
        public string COMP_NO { get; set; } = string.Empty;

        [Required(ErrorMessage = "STR_NO 參數為必填")]
        public string STR_NO { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇要上傳的 Logo 檔案")]
        public IFormFile logoFile { get; set; } = default!;
    }
}
