using INVwebAPI.Dtos.Token; // 註解：TokenDto
namespace INVwebAPI.Service; // 註解：命名空間
public interface IJwtTokenService // 註解：JWT 產生器介面
{ // 註解：介面開始
    TokenDto CreateToken(string compNo, string strNo, string passNo); // 註解：建立 access_token + refresh_token
} // 註解：介面結束
