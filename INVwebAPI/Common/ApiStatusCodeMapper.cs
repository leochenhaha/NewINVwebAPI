namespace INVwebAPI.Common; // 註解：命名空間
public static class ApiStatusCodeMapper // 註解：狀態碼映射工具
{ // 註解：類別開始
    public static int Map(bool status, string msg) // 註解：依 STATUS/MSG 判斷 HTTP code
    { // 註解：方法開始
        if (status) return 200; // 註解：成功一律 200
        if (msg != null && msg.StartsWith("登入失敗:")) return 500; // 註解：登入流程例外
        if (msg != null && msg.StartsWith("Refresh失敗:")) return 500; // 註解：Refresh 流程例外
        return 400; // 註解：其餘視為輸入或驗證失敗
    } // 註解：方法結束
} // 註解：類別結束
