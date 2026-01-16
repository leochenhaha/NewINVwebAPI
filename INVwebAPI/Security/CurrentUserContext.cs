namespace INVwebAPI.Security; // 註解：命名空間

public sealed class CurrentUserContext // 註解：目前登入者資訊的模型
{ // 註解：類別開始
 public bool IsAuthenticated { get; set; } // 註解：是否已通過驗證
 public string CompNo { get; set; } = ""; // 註解：公司代號
 public string StrNo { get; set; } = ""; // 註解：店別代號
 public string PassNo { get; set; } = ""; // 註解：使用者代號
} // 註解：類別結束
