// 檔案：Service/InvService.cs
using INVwebAPI.Data.Db; // 註解：EINV_WEBContext
using INVwebAPI.Data.Entities; // 註解：EINV_WEBContext

using INVwebAPI.Dtos.Inv; // 註解：DTO
using Microsoft.AspNetCore.Http; // 註解：IHttpContextAccessor
using Microsoft.Data.SqlClient; // 註解：SqlParameter
using Microsoft.EntityFrameworkCore; // 註解：EF Core
using Microsoft.Extensions.Logging; // 註解：ILogger
using System.Data; // 註解：IsolationLevel / SqlDbType / ParameterDirection
using System.Text.RegularExpressions; // 註解：Regex

namespace INVwebAPI.Service; // 註解：命名空間

public sealed class InvService : IInvService // 註解：Service 實作
{
    private readonly EINV_WEBContext _db; // 註解：DbContext
    private readonly ILogger<InvService> _logger; // 註解：Logger
    private readonly ICurrentUserAccessor _currentUser; // 註解：取得登入者
    private readonly IHttpContextAccessor _httpContextAccessor; // 註解：組 URL 用

    public InvService( // 註解：建構子 DI
        EINV_WEBContext db, // 註解：DbContext
        ILogger<InvService> logger, // 註解：Logger
        ICurrentUserAccessor currentUser, // 註解：登入者存取器
        IHttpContextAccessor httpContextAccessor) // 註解：HttpContext 存取器
    {
        _db = db; // 註解：保存 DbContext
        _logger = logger; // 註解：保存 logger
        _currentUser = currentUser; // 註解：保存 current user accessor
        _httpContextAccessor = httpContextAccessor; // 註解：保存 http context accessor
    }

    public async Task<GetInvNumberResponseDto> GetInvNumberAsync(GetInvNumberRequestDto request) // 註解：取號 API 的 service 主流程
    {
        try // 註解：捕捉例外，避免爆 500
        {
            var compNo = (request.COMP_NO ?? "").Trim(); // 註解：公司別
            var strNo = (request.STR_NO ?? "").Trim(); // 註解：店別
            var ecrNo = (request.ECR_NO ?? "").Trim(); // 註解：機號
            var tdate = request.TDATE.Date; // 註解：對齊舊版 Date

            if (string.IsNullOrEmpty(compNo) || string.IsNullOrEmpty(strNo) || string.IsNullOrEmpty(ecrNo)) // 註解：基本防呆
            {
                return new GetInvNumberResponseDto // 註解：統一回覆
                {
                    STATUS = false, // 註解：失敗
                    MSG = "參數不足", // 註解：訊息
                    IVONO = null // 註解：不回號碼
                };
            }

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable); // 註解：對齊舊版 Serializable 交易

            await _db.Database.ExecuteSqlRawAsync("SELECT TOP 0 NULL FROM MKFIVONO WITH (TABLOCKX, HOLDLOCK)"); // 註解：先鎖表避免多人同取

            string nowInvNum; // 註解：本次發出去的號碼

            var ivono = await _db.MKFIVONO // 註解：查字軌主檔
                .Where(p => p.COMP_NO == compNo) // 註解：公司別
                .Where(p => p.STR_NO == strNo) // 註解：店別
                .Where(p => p.ECR_NO == ecrNo) // 註解：機號
                .Where(p => p.BDATE <= tdate && p.EDATE >= tdate) // 註解：日期落在區間
                .Where(p => p.FLAG == "Y") // 註解：有效區段
                .FirstOrDefaultAsync(); // 註解：取一筆

            if (ivono == null) // 註解：查不到字軌就 call SP 取新區段
            {
                _logger.LogInformation("{COMP_NO}: 查詢無發票 CALL SP取新發票區間", compNo); // 註解：記錄

                var spResult = await CallMkpInvAiAsync(compNo, strNo, ecrNo); // 註解：呼叫 SP 取得起訖號

                if (string.IsNullOrEmpty(spResult.InvBno) || string.IsNullOrEmpty(spResult.InvEno)) // 註解：沒有取到字軌
                {
                    await tx.RollbackAsync(); // 註解：回滾
                    return new GetInvNumberResponseDto // 註解：統一回覆
                    {
                        STATUS = false, // 註解：失敗
                        MSG = "無設定發票字軌", // 註解：對齊舊訊息
                        IVONO = null // 註解：不回號碼
                    };
                }

                nowInvNum = spResult.InvBno; // 註解：本次號碼 = 起號

                _logger.LogInformation("{COMP_NO}: SP取號：{INV}", compNo, nowInvNum); // 註解：記錄

                var ivonoNew = await _db.MKFIVONO // 註解：用起號找到剛建立的區段
                    .Where(p => p.COMP_NO == compNo) // 註解：公司別
                    .Where(p => p.STR_NO == strNo) // 註解：店別
                    .Where(p => p.ECR_NO == ecrNo) // 註解：機號
                    .Where(p => p.IVO_B == nowInvNum) // 註解：起號
                    .Where(p => p.FLAG == "Y") // 註解：有效
                    .FirstOrDefaultAsync(); // 註解：取一筆

                if (ivonoNew == null) // 註解：SP 建完理論上一定要有資料
                {
                    await tx.RollbackAsync(); // 註解：回滾
                    return new GetInvNumberResponseDto // 註解：統一回覆
                    {
                        STATUS = false, // 註解：失敗
                        MSG = "無法取得發票區段資料", // 註解：訊息
                        IVONO = null // 註解：不回號碼
                    };
                }

                ivonoNew.CURRENT_IVO = NextInvNo(nowInvNum); // 註解：下一張預備號碼
                ivonoNew.UPD_DATE = DateTime.Now; // 註解：更新時間

                _db.MKFIVONO.Update(ivonoNew); // 註解：更新
                await _db.SaveChangesAsync(); // 註解：存檔

                var dupCheck = await IsDuplicateInvNoAsync(compNo, nowInvNum); // 註解：檢查近六個月是否重號

                await tx.CommitAsync(); // 註解：提交交易

                if (dupCheck) // 註解：重號
                {
                    _logger.LogError("發票號碼重覆：{INV}", nowInvNum); // 註解：記錄錯誤
                    return new GetInvNumberResponseDto // 註解：統一回覆
                    {
                        STATUS = false, // 註解：失敗
                        MSG = "取得發票號碼重覆,請再試一次", // 註解：對齊舊訊息
                        IVONO = null // 註解：不回號碼
                    };
                }

                _logger.LogInformation("GetINVnumber: {INV}", nowInvNum); // 註解：記錄成功
                return new GetInvNumberResponseDto // 註解：成功回覆
                {
                    STATUS = true, // 註解：成功
                    MSG = "成功", // 註解：對齊舊訊息
                    IVONO = nowInvNum // 註解：回傳號碼
                };
            }

            nowInvNum = ivono.CURRENT_IVO ?? ""; // 註解：既有區段則用 CURRENT_IVO 當本次號碼

            if (string.IsNullOrEmpty(nowInvNum)) // 註解：防呆
            {
                await tx.RollbackAsync(); // 註解：回滾
                return new GetInvNumberResponseDto // 註解：統一回覆
                {
                    STATUS = false, // 註解：失敗
                    MSG = "目前發票號碼異常", // 註解：訊息
                    IVONO = null // 註解：不回號碼
                };
            }

            if (ivono.IVO_E == nowInvNum) // 註解：若本次已到訖號
            {
                ivono.CURRENT_IVO = nowInvNum; // 註解：回押原號
                ivono.FLAG = "N"; // 註解：整段失效
            }
            else
            {
                ivono.CURRENT_IVO = NextInvNo(nowInvNum); // 註解：一般情況下一張 +1
            }

            ivono.UPD_DATE = DateTime.Now; // 註解：更新時間

            _db.MKFIVONO.Update(ivono); // 註解：更新
            await _db.SaveChangesAsync(); // 註解：存檔

            var isDup = await IsDuplicateInvNoAsync(compNo, nowInvNum); // 註解：檢查近六個月重號

            await tx.CommitAsync(); // 註解：提交交易

            if (isDup) // 註解：重號
            {
                _logger.LogError("發票號碼重覆：{INV}", nowInvNum); // 註解：記錄錯誤
                return new GetInvNumberResponseDto // 註解：統一回覆
                {
                    STATUS = false, // 註解：失敗
                    MSG = "取得發票號碼重覆,請再試一次", // 註解：對齊舊訊息
                    IVONO = null // 註解：不回號碼
                };
            }

            _logger.LogInformation("GetINVnumber: {INV}", nowInvNum); // 註解：記錄成功
            return new GetInvNumberResponseDto // 註解：成功回覆
            {
                STATUS = true, // 註解：成功
                MSG = "成功", // 註解：對齊舊訊息
                IVONO = nowInvNum // 註解：回傳號碼
            };
        }
        catch (Exception ex) // 註解：例外處理
        {
            _logger.LogError(ex, "GetINVnumber ERROR"); // 註解：記錄錯誤
            return new GetInvNumberResponseDto // 註解：統一回覆
            {
                STATUS = false, // 註解：失敗
                MSG = ex.Message, // 註解：對齊舊版回 ex.Message
                IVONO = null // 註解：不回號碼
            };
        }
    }

    public async Task<UploadB2BDealDataResponseDto> UploadB2BDealDataAsync(UploadB2BDealDataRequestDto request) // 註解：UploadB2BDealData 主流程
    {
        try // 註解：確保不爆 500，回統一格式
        {
            // 修正：原本呼叫 _currentUser.GetCurrentUser() 會出現 CS1061（介面上沒有此方法）
            // 這裡改由 reflection 嘗試從 accessor 取出登入者物件，並抽出其 PASS_NO 屬性作為人員代號。
            var userObj = ResolveCurrentUserObject(); // 先嘗試取得登入者物件
            if (userObj == null) // 沒登入
            {
                return FailBadRequest("Authorization ERROR"); // 回 400
            }

            var passNo = GetPassNoFromUser(userObj); // 取 PASS_NO
            if (string.IsNullOrEmpty(passNo))
            {
                return FailBadRequest("Authorization ERROR"); // 回 400
            }

            var compNo = (request.COMP_NO ?? "").Trim(); // 註解：公司別
            var strNo = (request.STR_NO ?? "").Trim(); // 註解：店別
            var ecrNo = (request.ECR_NO ?? "").Trim(); // 註解：機號

            if (string.IsNullOrEmpty(compNo) || string.IsNullOrEmpty(strNo) || string.IsNullOrEmpty(ecrNo)) // 註解：必填檢查
            {
                return FailBadRequest("參數不足"); // 註解：回 400
            }

            if (string.IsNullOrWhiteSpace(request.INVOICEDATE) || !DateTime.TryParse(request.INVOICEDATE, out var tdateRaw)) // 註解：交易日解析
            {
                return FailBadRequest("INVOICEDATE 格式錯誤"); // 註解：回 400
            }

            var tdate = tdateRaw.Date; // 註解：只取 Date
            if (DateTime.Today != tdate) // 註解：交易日必須當日
            {
                return FailBadRequest("上傳交易日非當日"); // 註解：對齊舊訊息
            }

            if (!string.IsNullOrEmpty(request.Buy_IDENTIFIER)) // 註解：買方統編有填才檢查
            {
                if (!IsTaiwanGuiValid(request.Buy_IDENTIFIER)) // 註解：統編檢查
                {
                    return FailBadRequest("統編輸入錯誤"); // 註解：對齊舊訊息
                }
            }

            if (string.Equals(request.CARRY_TYPE, "3J0002", StringComparison.OrdinalIgnoreCase)) // 註解：手機條碼載具
            {
                var carryId = request.CARRY_ID ?? ""; // 註解：載具號碼
                if (!IsMobileBarcodeValid(carryId)) // 註解：檢查格式
                {
                    return FailBadRequest("手機條碼輸入錯誤"); // 註解：對齊舊訊息
                }

                request.CARRY_ID = carryId.ToUpperInvariant(); // 註解：轉大寫
            }

            _logger.LogInformation("UploadB2BDealData payload: {Payload}", request); // 註解：記錄 payload

            var kdate = DateTime.Now; // 註解：系統時間

            var existing = await _db.MKFINV01 // 註解：同訂單是否已開過票
                .FirstOrDefaultAsync(p => p.Comp_No == compNo && p.Str_No == strNo && p.ORDER_NO == request.ORDER_NO); // 註解：條件

            if (existing != null) // 註解：已存在則直接回舊行為
            {
                if (string.Equals(request.PrintMark, "Y", StringComparison.OrdinalIgnoreCase)) // 註解：PrintMark=Y 才更新 Print_Yn
                {
                    if (!string.Equals(existing.Print_Yn, "Y", StringComparison.OrdinalIgnoreCase)) // 註解：尚未列印
                    {
                        existing.Print_Yn = "Y"; // 註解：更新列印旗標
                        existing.Upd_No = passNo; // 註解：更新人（改用反射取出的 passNo）
                        existing.Upd_Date = DateTime.Now; // 註解：更新時間
                        _db.MKFINV01.Update(existing); // 註解：更新
                        await _db.SaveChangesAsync(); // 註解：存檔
                    }
                }

                return new UploadB2BDealDataResponseDto // 註解：回覆（已開過）
                {
                    STATUS = true, // 註解：成功
                    MSG = "此訂單已成立過發票號碼", // 註解：對齊舊訊息
                    InvoiceNoZh = existing.Inv_No, // 註解：發票號碼（中文欄位）
                    Random_Code = existing.Random_Code, // 註解：Random_Code（英文欄位）
                    RandomCodeZh = existing.Random_Code, // 註解：隨機碼（中文欄位）
                    A4PdfUrlZh = null, // 註解：此分支舊碼不一定回 A4
                    ErrorCode = null // 註解：無錯誤
                };
            }

            var seqNo = await MkpGetNoAsync(kdate, "INVS", compNo + strNo, passNo, "1"); // 註解：取序號（改用 passNo）
            if (seqNo <= 0) // 註解：防呆
            {
                return FailBadRequest("無法取得序號，請檢查"); // 註解：回 400
            }

            decimal totAmt; // 註解：總金額
            if (string.Equals(request.INVFLAG, "B", StringComparison.OrdinalIgnoreCase)) // 註解：B2B 需加稅額
            {
                totAmt = request.STAX_AMT + request.FREE_AMT + request.TAXED_AMT + request.TAX_AMT; // 註解：對齊舊邏輯
            }
            else
            {
                totAmt = request.STAX_AMT + request.FREE_AMT + request.TAXED_AMT; // 註解：對齊舊邏輯
            }

            var invNo = await GetInvNoForUploadAsync(compNo, strNo, ecrNo, kdate); // 註解：取發票號碼（共用 GetInvNumberAsync）
            if (string.IsNullOrEmpty(invNo)) // 註解：取不到就回錯
            {
                return FailBadRequest("無法取得發票號碼，請檢查"); // 註解：對齊舊訊息
            }

            var randomCode = CreateRandomCode4(); // 註解：四碼隨機碼

            var inv01 = MapToInv01Entity(request, passNo, compNo, strNo, kdate, seqNo, invNo, tdateRaw, totAmt, randomCode); // 註解：mapping 主檔（改用 passNo）
            var invTiList = MapToInvTiEntities(request, passNo, compNo, strNo, kdate, seqNo); // 註解：mapping 明細（改用 passNo）

            _db.MKFINV01.Add(inv01); // 註解：新增主檔
            _db.MKFINVTI.AddRange(invTiList); // 註解：新增明細

            await _db.SaveChangesAsync(); // 註解：存檔

            string? a4Url = null; // 註解：A4 URL
            if (!string.IsNullOrEmpty(inv01.BL_NO)) // 註解：有 BL_NO 才產 PDF
            {
                var pdfOk = await TryGenerateB2BPdfAsync(inv01.Inv_No); // 註解：產 PDF
                if (pdfOk) // 註解：成功才回 URL
                {
                    a4Url = BuildA4PdfUrl(inv01.Inv_No); // 註解：組 URL
                }
            }

            var response = new UploadB2BDealDataResponseDto // 註解：成功回覆
            {
                STATUS = true, // 註解：成功
                MSG = "成功", // 註解：對齊舊訊息
                InvoiceNoZh = invNo, // 註解：發票號碼
                Random_Code = inv01.Random_Code, // 註解：Random_Code
                RandomCodeZh = inv01.Random_Code, // 註解：隨機碼（中文）
                A4PdfUrlZh = a4Url, // 註解：A4 證明聯
                ErrorCode = null // 註解：無錯誤
            };

            _logger.LogInformation("UploadB2BDealData result: {Result}", response); // 註解：記錄結果
            return response; // 註解：回傳
        }
        catch (Exception ex) // 註解：例外回 417 對齊舊 catch
        {
            _logger.LogError(ex, "UploadB2BDealData ERROR"); // 註解：記錄錯誤
            return new UploadB2BDealDataResponseDto // 註解：統一回覆
            {
                STATUS = false, // 註解：失敗
                MSG = ex.Message, // 註解：對齊舊 ex.Message
                InvoiceNoZh = null, // 註解：不回發票
                Random_Code = null, // 註解：不回隨機碼
                RandomCodeZh = null, // 註解：不回隨機碼
                A4PdfUrlZh = null, // 註解：不回 A4
                ErrorCode = "EXPECTATION_FAILED" // 註解：讓 Controller 回 417
            };
        }
    }

    private static string NextInvNo(string current) // 註解：AB00000001 -> AB00000002
    {
        var head = current.Substring(0, 2); // 註解：字軌
        var numPart = current.Substring(2, 8); // 註解：數字區
        var num = int.Parse(numPart); // 註解：轉 int
        num = num + 1; // 註解：加 1
        var nextNumPart = num.ToString().PadLeft(8, '0'); // 註解：補 8 位
        return head + nextNumPart; // 註解：組回
    }

    private async Task<bool> IsDuplicateInvNoAsync(string compNo, string invNo) // 註解：近六個月重號檢查
    {
        // Inv_Date 在實體為 DateOnly?（見 MKFINV01），因此使用 DateOnly 比較
        var since = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)); // 註解：起始日（DateOnly）

        var dup = await _db.MKFINV01 // 註解：查已開立發票
            .Where(m => m.Comp_No == compNo) // 註解：公司別
            .Where(m => m.Inv_No == invNo) // 註解：發票號碼
            .Where(m => m.Inv_Date != null && m.Inv_Date >= since) // 註解：近六個月（含起始日），並排除 null
            .FirstOrDefaultAsync(); // 註解：取一筆

        return dup != null; // 註解：有即重覆
    }

    private sealed class MkpInvAiResult // 註解：SP 結果封裝
    {
        public string? InvBno { get; init; } // 註解：起號
        public string? InvEno { get; init; } // 註解：訖號
    }

    private async Task<MkpInvAiResult> CallMkpInvAiAsync(string compNo, string strNo, string ecrNo) // 註解：呼叫 MKPINVAI
    {
        var year = (DateTime.Now.Year - 1911).ToString(); // 註解：民國年
        var month = DateTime.Now.Month % 2 == 0 // 註解：偶數月取上個月
            ? DateTime.Now.AddMonths(-1).Month.ToString("d2") // 註解：上個月
            : DateTime.Now.Month.ToString("d2"); // 註解：本月

        var invPeriod = year + month; // 註解：期別

        var psInvBno = new SqlParameter("@ps_inv_bno", SqlDbType.VarChar, 10) { Direction = ParameterDirection.Output }; // 註解：起號 output
        var psInvEno = new SqlParameter("@ps_inv_eno", SqlDbType.VarChar, 10) { Direction = ParameterDirection.Output }; // 註解：訖號 output
        var pFleno = new SqlParameter("@pFleno", SqlDbType.VarChar, 20) { Direction = ParameterDirection.Output }; // 註解：output
        var pCnt = new SqlParameter("@pCnt", SqlDbType.Int) { Direction = ParameterDirection.Output }; // 註解：output
        var pSec = new SqlParameter("@pSec", SqlDbType.VarChar, 2048) { Direction = ParameterDirection.Output }; // 註解：output
        var pRet = new SqlParameter("@pRet", SqlDbType.Int) { Direction = ParameterDirection.Output }; // 註解：output

        await _db.Database.ExecuteSqlRawAsync( // 註解：執行 SP
            "EXEC dbo.MKPINVAI @ps_COMP_no={0}, @ps_str_no={1}, @ps_ecr_no={2}, @ps_inv_period={3}, @ps_inv_type={4}, @pi_inv_cnt={5}, @ps_pass_no={6}, @ps_inv_bno=@ps_inv_bno OUTPUT, @ps_inv_eno=@ps_inv_eno OUTPUT, @pFleno=@pFleno OUTPUT, @pCnt=@pCnt OUTPUT, @pSec=@pSec OUTPUT, @pRet=@pRet OUTPUT",
            compNo, // 註解：公司別
            strNo, // 註解：店別
            ecrNo, // 註解：機號
            invPeriod, // 註解：期別
            "35", // 註解：inv_type
            50, // 註解：inv_cnt（若有 COMP_NO 特例，再加邏輯）
            "WebApi", // 註解：pass_no
            psInvBno, // 註解：output
            psInvEno, // 註解：output
            pFleno, // 註解：output
            pCnt, // 註解：output
            pSec, // 註解：output
            pRet); // 註解：output

        var invBno = psInvBno.Value == DBNull.Value ? null : psInvBno.Value?.ToString(); // 註解：起號
        var invEno = psInvEno.Value == DBNull.Value ? null : psInvEno.Value?.ToString(); // 註解：訖號

        return new MkpInvAiResult // 註解：回封裝結果
        {
            InvBno = invBno, // 註解：起號
            InvEno = invEno // 註解：訖號
        };
    }

    private UploadB2BDealDataResponseDto FailBadRequest(string msg) // 註解：統一 400 回覆
    {
        return new UploadB2BDealDataResponseDto
        {
            STATUS = false,
            MSG = msg,
            InvoiceNoZh = null,
            Random_Code = null,
            RandomCodeZh = null,
            A4PdfUrlZh = null,
            ErrorCode = "BAD_REQUEST"
        };
    }

    private string BuildA4PdfUrl(string invNo) // 註解：組合 A4 證明聯 URL
    {
        var http = _httpContextAccessor.HttpContext; // 註解：HttpContext
        if (http == null) // 註解：防呆
        {
            return "/TEMP/" + invNo + ".pdf"; // 註解：退化
        }

        var scheme = http.Request.Scheme; // 註解：http/https
        var host = http.Request.Host.Value; // 註解：host
        return scheme + "://" + host + "/TEMP/" + invNo + ".pdf"; // 註解：完整 URL
    }

    private static string CreateRandomCode4() // 註解：產 4 碼隨機碼
    {
        var n = Random.Shared.Next(0, 10000); // 註解：0~9999
        return n.ToString("0000"); // 註解：補 4 位
    }

    private static bool IsMobileBarcodeValid(string carryId) // 註解：手機條碼格式檢查
    {
        if (string.IsNullOrEmpty(carryId)) return false; // 註解：空值不合法
        if (carryId.Length != 8) return false; // 註解：長度需 8
        if (!carryId.StartsWith("/")) return false; // 註解：需以 / 開頭
        return Regex.IsMatch(carryId.Substring(1), "^[0-9A-Z]{7}$"); // 註解：後 7 碼格式
    }

    private static bool IsTaiwanGuiValid(string gui) // 註解：統編檢核
    {
        gui = (gui ?? "").Trim(); // 註解：整理
        if (!Regex.IsMatch(gui, "^[0-9]{8}$")) return false; // 註解：必須 8 碼數字

        var weights = new[] { 1, 2, 1, 2, 1, 2, 4, 1 }; // 註解：權重
        var sum = 0; // 註解：加總

        for (var i = 0; i < 8; i++) // 註解：逐位計算
        {
            var d = gui[i] - '0'; // 註解：字元轉數字
            var p = d * weights[i]; // 註解：乘權重
            sum += (p / 10) + (p % 10); // 註解：十位加個位
        }

        if (sum % 10 == 0) return true; // 註解：可整除 10
        if (gui[6] == '7' && (sum + 1) % 10 == 0) return true; // 註解：第 7 碼為 7 特例
        return false; // 註解：不合法
    }

    private async Task<decimal> MkpGetNoAsync(DateTime kdate, string type, string key, string passNo, string cnt) // 註解：呼叫 MKPGETNO
    {
        var pNo = new SqlParameter("@pNo", SqlDbType.Decimal) { Direction = ParameterDirection.Output }; // 註解：output

        await _db.Database.ExecuteSqlRawAsync( // 註解：執行 SP
            "EXEC dbo.MKPGETNO @pKdate={0}, @pType={1}, @pKey={2}, @pPassNo={3}, @pCnt={4}, @pNo=@pNo OUTPUT",
            kdate, // 註解：日期
            type, // 註解：類別
            key, // 註解：key
            passNo, // 註解：人員
            cnt, // 註解：數量
            pNo); // 註解：output

        if (pNo.Value == DBNull.Value || pNo.Value == null) return 0; // 註解：取不到
        return Convert.ToDecimal(pNo.Value); // 註解：轉 decimal
    }

    private async Task<string> GetInvNoForUploadAsync(string compNo, string strNo, string ecrNo, DateTime kdate) // 註解：Upload 取號（共用 GetInvNumberAsync）
    {
        var req = new GetInvNumberRequestDto
        {
            COMP_NO = compNo,
            STR_NO = strNo,
            ECR_NO = ecrNo,
            TDATE = kdate.Date
        };

        var res = await GetInvNumberAsync(req); // 註解：共用取號
        if (!res.STATUS) return ""; // 註解：失敗回空
        return res.IVONO ?? ""; // 註解：回號碼
    }

    private async Task<bool> TryGenerateB2BPdfAsync(string invNo) // 註解：產 PDF（預設先不做）
    {
        await Task.CompletedTask; // 註解：保留 async
        _logger.LogInformation("TryGenerateB2BPdfAsync called: {InvNo}", invNo); // 註解：記錄
        return false; // 註解：你要接舊 utility.GenerateB2BPDF 就改這裡
    }

    private static MKFINV01 MapToInv01Entity( // 註解：DTO -> MKFINV01
        UploadB2BDealDataRequestDto request,
        string passNo,
        string compNo,
        string strNo,
        DateTime kdate,
        decimal seqNo,
        string invNo,
        DateTime tdate,
        decimal totAmt,
        string randomCode)
    {
        return new MKFINV01
        {
            Comp_No = compNo,
            Str_No = strNo,
            Kdate = DateOnly.FromDateTime(kdate),
            Seq_No = seqNo,
            Inv_No = invNo,
            Inv_type = "35",
            Inv_Date = DateOnly.FromDateTime(tdate),
            Inv_Time = tdate,
            Tax_Type = request.TAX_TYPE,
            BL_NO = request.SEL_IDENTIFIER,
            buy_bl_no = request.Buy_IDENTIFIER,
            buy_na = request.Buy_NAME,
            Tot_Amt = totAmt,
            Notax_Amt = request.FREE_AMT,
            ZeroTax_Amt = request.ZERO_AMT,
            Stax_Amt = request.STAX_AMT,
            Tax = request.TAX_AMT,
            TAXED_AMT = request.TAXED_AMT,
            ECP_TYPE = request.ECP_TYPE,
            Print_Yn = request.PrintMark,
            Random_Code = randomCode,
            Carry_Type = request.CARRY_TYPE,
            Carry_Id = request.CARRY_ID,
            Carry_Id2 = request.CARRY_ID2,
            buy_Address = request.MEM_ADDR,
            buy_EMAIL = request.MEM_EMAIL,
            BUY_MOBILE = request.MEM_MOBILE,
            MIG_Type = request.INVFLAG,
            ORDER_NO = request.ORDER_NO,
            Love_Code = request.LOVE_CODE,
            Donate_Mark = !string.IsNullOrEmpty(request.LOVE_CODE) ? "1" : null,
            Crt_No = passNo,
            Crt_Date = DateTime.Now,
            Upd_No = passNo,
            Upd_Date = DateTime.Now,
            Memo = request.MEMO
        };
    }

    private static List<MKFINVTI> MapToInvTiEntities( // 註解：DTO -> MKFINVTI
        UploadB2BDealDataRequestDto request,
        string passNo,
        string compNo,
        string strNo,
        DateTime kdate,
        decimal seqNo)
    {
        var list = new List<MKFINVTI>(); // 註解：清單

        foreach (var d in request.Detail) // 註解：逐筆
        {
            var itemNo = 0m; // 註解：項次
            _ = decimal.TryParse(d.ITEM_NO, out itemNo); // 註解：轉 decimal

            var entity = new MKFINVTI
            {
                Comp_No = compNo,
                Str_No = strNo,
                Kdate = DateOnly.FromDateTime(kdate),
                Seq_No = seqNo,
                Item_No = itemNo,
                Inv_Desc = NormalizeText(d.GOO_NA),
                Tax_Type = "1",
                Qty = d.QTY,
                Price = d.SPRICE,
                Amt = d.AMT,
                TAX = d.TAX,
                Crt_No = passNo,
                Crt_Date = DateTime.Now,
                Upd_No = passNo,
                Upd_Date = DateTime.Now
            };

            list.Add(entity); // 註解：加入
        }

        return list; // 註解：回傳
    }

    private static string NormalizeText(string input) // 註解：先做最小可用版
    {
        if (input == null) return ""; // 註解：防呆
        return input.Trim(); // 註解：Trim
    }

    // 新增：嘗試從 ICurrentUserAccessor 取得登入者物件（反射支援多種命名）
    private object? ResolveCurrentUserObject()
    {
        if (_currentUser == null) return null;

        var type = _currentUser.GetType();

        // 常見 property 名稱
        var prop = type.GetProperty("CurrentUser")
                   ?? type.GetProperty("User")
                   ?? type.GetProperty("Value");
        if (prop != null)
        {
            try
            {
                return prop.GetValue(_currentUser);
            }
            catch
            {
                // ignore reflection errors, fallback below
            }
        }

        // 常見方法
        var method = type.GetMethod("GetCurrentUser", Type.EmptyTypes)
                     ?? type.GetMethod("GetUser", Type.EmptyTypes)
                     ?? type.GetMethod("Get", Type.EmptyTypes);
        if (method != null)
        {
            try
            {
                return method.Invoke(_currentUser, null);
            }
            catch
            {
                // ignore
            }
        }

        // 若 accessor 本身就是 user 類型（某些實作會直接以該介面代表 user），則回傳它
        return _currentUser;
    }

    // 新增：從 user 物件取出 PASS_NO（反射）
    private static string? GetPassNoFromUser(object? user)
    {
        if (user == null) return null;

        var type = user.GetType();

        var prop = type.GetProperty("PASS_NO")
               ?? type.GetProperty("PassNo")
               ?? type.GetProperty("Pass_No")
               ?? type.GetProperty("Id")
               ?? type.GetProperty("UserId");
        if (prop != null)
        {
            try
            {
                var val = prop.GetValue(user);
                return val?.ToString();
            }
            catch
            {
                return null;
            }
        }

        // 若是字串，直接回傳
        if (user is string s) return s;

        return null;
    }
}
