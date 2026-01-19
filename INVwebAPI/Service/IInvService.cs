// 檔案：Service/IInvService.cs
using INVwebAPI.Dtos.Inv; // 註解：DTO

namespace INVwebAPI.Service; // 註解：命名空間

public interface IInvService // 註解：發票 Service 介面
{
    Task<GetInvNumberResponseDto> GetInvNumberAsync(GetInvNumberRequestDto request); // 註解：你已經測通的取號
    Task<UploadB2BDealDataResponseDto> UploadB2BDealDataAsync(UploadB2BDealDataRequestDto request); // 註解：本次新增的 B2B 上傳
}
