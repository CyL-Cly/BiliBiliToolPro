using System.Security.Cryptography;
using System.Text;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;

/// <summary>
/// APP 侧签名工具（appkey + ts + sign）
/// </summary>
/// <remarks>
/// 算法：将请求参数按 key 字母排序拼接为 query string，末尾拼接 appSec 后取 MD5。
/// appkey 与 appSec 必须配对使用，否则会返回 {"code":-3,"message":"API校验密匙错误"}。
/// </remarks>
public static class AppSignHelper
{
    /// <summary>
    /// 已登录用户接口的 appKey
    /// </summary>
    public const string AppKey = "1d8b6e7d45233436";

    /// <summary>
    /// 已登录用户接口的 appSec
    /// </summary>
    public const string AppSec = "560c52ccd288fed045859ed18bffd973";

    /// <summary>
    /// 计算 APP 签名
    /// </summary>
    /// <param name="parameters">请求参数（不含 sign）</param>
    /// <param name="appSec">与 appkey 配对的密钥</param>
    /// <returns>32位小写 hex 签名</returns>
    public static string CalcSign(IDictionary<string, string> parameters, string appSec)
    {
        var queryString = BuildSortedQuery(parameters);
        var hashStr = queryString + appSec;
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(hashStr));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 按 key 排序拼接 query string（排除空值与 sign 自身）
    /// </summary>
    public static string BuildSortedQuery(IDictionary<string, string> parameters)
    {
        var queryList = parameters
            .Where(x => x.Key != "sign" && !string.IsNullOrEmpty(x.Value))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}")
            .ToList();
        return string.Join("&", queryList);
    }
}
