namespace INVwebAPI.Dtos.Inv
{
    public sealed class GetInvNumberResponseDto // 每行註解：Response DTO，統一回傳格式
    {
        public bool STATUS { get; set; }            // 每行註解：成功與否
        public string MSG { get; set; } = "";       // 每行註解：訊息，對齊舊版文字
        public string? IVONO { get; set; }          // 每行註解：成功時回傳發票號碼，失敗可為 null
    }
}
