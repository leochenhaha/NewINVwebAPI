using System.Collections.Concurrent; // 註解：ConcurrentDictionary
namespace INVwebAPI.Service; // 註解：命名空間
public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore // 註解：暫存版 refresh token store
{ // 註解：類別開始
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _dict = new(); // 註解：key=refreshToken
    public bool TryGet(string refreshToken, out RefreshTokenRecord record) // 註解：查詢
    { // 註解：方法開始
        return _dict.TryGetValue(refreshToken, out record!); // 註解：回傳是否存在
    } // 註解：方法結束
    public void Upsert(RefreshTokenRecord record) // 註解：新增或更新
    { // 註解：方法開始
        _dict[record.RefreshToken] = record; // 註解：直接覆蓋即可
    } // 註解：方法結束
    public bool Remove(string refreshToken) // 註解：刪除
    { // 註解：方法開始
        return _dict.TryRemove(refreshToken, out _); // 註解：移除舊 token
    } // 註解：方法結束
    public int CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var removed = 0;

        foreach (var kv in _dict)
        {
            if (kv.Value.ExpiresAt <= now)
            {
                if (_dict.TryRemove(kv.Key, out _))
                {
                    removed++;
                }
            }
        }

        return removed;
    }
} // 註解：類別結束
