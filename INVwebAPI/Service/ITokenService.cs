using System.Security.Claims; // 註解：ClaimsPrincipal
using System.Threading; // 註解：CancellationToken
using System.Threading.Tasks; // 註解：Task
using INVwebAPI.Dtos.Token; // 註解：DTO
namespace INVwebAPI.Service; // 註解：命名空間
public interface ITokenService // 註解：Token 服務介面
{ // 註解：介面開始
    Task<SignInResponseDto> SignInAsync(SignInRequestDto request, CancellationToken ct); // 註解：登入
    Task<RefreshResponseDto> RefreshAsync(RefreshRequestDto request, CancellationToken ct); // 註解：換新 token
    Task<IsAuthenticatedResponseDto> IsAuthenticatedAsync(ClaimsPrincipal user, CancellationToken ct); // 註解：驗證狀態
} // 註解：介面結束
