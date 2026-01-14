namespace INVwebAPI.Dtos.File
{
    public sealed class UploadLogoResponseDto
    {
        public bool STATUS { get; set; }
        public string MSG { get; set; } = string.Empty;

        public string? COMP_NO { get; set; }
        public string? STR_NO { get; set; }
        public string? FILE_NAME { get; set; }
        public string? FILE_PATH { get; set; }
    }
}
