using System.Security.Cryptography;
using System.Text;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;

/// <summary>
/// 大会员赚积分任务 receive/v2、complete/v2 的 APP 风格请求体构造
/// </summary>
/// <remarks>
/// 服务端要求完整的 APP webview 表单参数并校验 sign，仅传 taskCode 会返回 -400。
/// </remarks>
public static class ScoreTaskV2Request
{
    /// <summary>
    /// 构造带 sign 的请求体
    /// </summary>
    /// <param name="taskCode">任务 code，如 ogvwatchnew、dress-view</param>
    /// <param name="ck">账号 cookie</param>
    public static Dictionary<string, string> Build(string taskCode, BiliCookie ck)
    {
        string fingerprint = GetSha256Hex(ck.UserId);
        string deviceId = GetMd5Hex(ck.UserId)[..16];
        string sessionId = GetRandomHex(8);

        var parameters = new Dictionary<string, string>
        {
            ["alwaysTranslate"] = "false",
            ["appKey"] = AppSignHelper.AppKey,
            ["appkey"] = AppSignHelper.AppKey,
            ["bili_local_id"] = fingerprint,
            ["brand"] = "nubia",
            ["build"] = "9070300",
            ["buvid"] = ck.Buvid,
            ["channel"] = "html5_app_bili",
            ["containerName"] = "AbstractWebActivity",
            ["csrf"] = ck.BiliJct,
            ["device"] = "phone",
            ["deviceId"] = deviceId,
            ["deviceName"] = "nubiaNX666J",
            ["devicePlatform"] = "Android12nubiaNX666J",
            ["device_id"] = fingerprint,
            ["device_name"] = "nubiaNX666J",
            ["device_platform"] = "Android12nubiaNX666J",
            ["disable_rcmd"] = "0",
            ["fingerprint"] = fingerprint,
            ["ipRegion"] = "CN",
            ["isDaylightTime"] = "false",
            ["isPad"] = "false",
            ["legalRegion"] = "CN",
            ["localFingerprint"] = fingerprint,
            ["local_id"] = ck.Buvid,
            ["mobi_app"] = "android",
            ["modelName"] = "NX666J",
            ["networkState"] = "2",
            ["networkstate"] = "2",
            ["osVer"] = "12",
            ["platform"] = "android",
            ["sessionID"] = sessionId,
            ["statistics"] = "{\"appId\":1,\"platform\":3,\"version\":\"9.7.0\",\"abtest\":\"\"}",
            ["statusBarHeight"] = "111",
            ["taskCode"] = taskCode,
            ["timezone"] = "Asia/Shanghai",
            ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["utcOffset"] = "+08:00",
        };

        parameters["sign"] = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);

        return parameters;
    }

    private static string GetMd5Hex(string input)
    {
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    private static string GetSha256Hex(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    private static string GetRandomHex(int length)
    {
        const string hexChars = "0123456789abcdef";
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = hexChars[Random.Shared.Next(hexChars.Length)];
        }
        return new string(chars);
    }
}
