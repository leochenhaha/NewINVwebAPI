namespace INVwebAPI.Dtos.Inv
{
    public sealed class GetInvNumberRequestDto // 每行註解：Request DTO，對齊舊版 JObject 欄位
    {
        public string COMP_NO { get; set; } = string.Empty; // 每行註解：公司別，必填
        public string STR_NO { get; set; } = string.Empty;  // 每行註解：店別，必填
        public string ECR_NO { get; set; } = string.Empty;  // 每行註解：機號，必填
        public DateTime TDATE { get; set; }                 // 每行註解：交易日期，舊版會取 .Date
    }
}
