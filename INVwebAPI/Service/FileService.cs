using INVwebAPI.Dtos.File;
using Microsoft.AspNetCore.Hosting;

namespace INVwebAPI.Service
{
    public sealed class FileService
    {
        private readonly IWebHostEnvironment _env;

        private const long MaxFileSizeBytes = 1 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico"
        };

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<UploadLogoResponseDto> UploadLogoAsync(UploadLogoRequestDto dto, CancellationToken ct)
        {
            if (dto.logoFile == null || dto.logoFile.Length <= 0)
            {
                return Fail("請選擇要上傳的 Logo 檔案");
            }

            if (dto.logoFile.Length > MaxFileSizeBytes)
            {
                var mb = dto.logoFile.Length / 1024d / 1024d;
                return Fail($"檔案大小超過限制（最大 1MB），實際大小: {mb:F2}MB");
            }

            var originalFileName = dto.logoFile.FileName ?? string.Empty;
            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(ext))
            {
                return Fail($"不支援的檔案格式，僅支援: {string.Join(", ", AllowedExtensions)}");
            }

            var webRoot = _env.WebRootPath ?? _env.ContentRootPath;
            var logoDir = Path.Combine(webRoot, "IMG", "LOGO", dto.COMP_NO, dto.STR_NO);

            Directory.CreateDirectory(logoDir);

            var existingFiles = Directory.GetFiles(logoDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            foreach (var f in existingFiles)
            {
                File.Delete(f);
            }

            var newFileName = $"logo{ext}";
            var filePath = Path.Combine(logoDir, newFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await dto.logoFile.CopyToAsync(stream, ct);
            }

            var relativePath = $"IMG/LOGO/{dto.COMP_NO}/{dto.STR_NO}/{newFileName}";

            return new UploadLogoResponseDto
            {
                STATUS = true,
                MSG = "成功",
                COMP_NO = dto.COMP_NO,
                STR_NO = dto.STR_NO,
                FILE_NAME = newFileName,
                FILE_PATH = relativePath
            };
        }

        private static UploadLogoResponseDto Fail(string msg)
        {
            return new UploadLogoResponseDto
            {
                STATUS = false,
                MSG = msg
            };
        }
    }
}
