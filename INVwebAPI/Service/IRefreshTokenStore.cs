namespace INVwebAPI.Service; // 註解：命名空間
public interface IRefreshTokenStore // 註解：RefreshToken 存取介面（之後要換 DB 只要改實作）
{ // 註解：介面開始
    bool TryGet(string refreshToken, out RefreshTokenRecord record); // 註解：查詢 refresh token
    void Upsert(RefreshTokenRecord record); // 註解：新增或更新 refresh token
    bool Remove(string refreshToken); // 註解：刪除舊 refresh token
} // 註解：介面結束
