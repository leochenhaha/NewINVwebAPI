using INVwebAPI.Security; // 註解：CurrentUserContext

namespace INVwebAPI.Service; // 註解：命名空間

public interface ICurrentUserAccessor // 註解：取得目前登入者資訊的介面
{ // 註解：介面開始
 CurrentUserContext Get(); // 註解：取得目前登入者資訊
} // 註解：介面結束
