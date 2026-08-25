using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;

/// <summary>
/// 大会员赚积分「观看剧集内容」（ogvwatchnew）APP deliver 流程请求体构造
/// </summary>
/// <remarks>
/// 流程：receive/v2 领取 → deliver/material/receive 开始观看
/// → heartbeat/mobile 观看上报 → deliver/task/complete 上报完成。
/// 旧的 score/task/complete/v2 对该任务已返回 -400，必须走 deliver 接口。
/// </remarks>
public static class OgvWatchRequest
{
    /// <summary>
    /// 默认剧集：《让子弹飞》
    /// </summary>
    public const string SeasonId = "12548";

    public const string EpId = "328482";

    /// <summary>
    /// 构造开始观看（deliver/material/receive）的请求体，返回该次观看的 task_id 与 token
    /// </summary>
    public static Dictionary<string, string> BuildStart(BiliCookie ck)
    {
        var parameters = BuildCommon(ck);
        parameters["activity_code"] = "";
        parameters["ep_id"] = EpId;
        parameters["from_spmid"] = "activity.h5.0.0";
        parameters["season_id"] = SeasonId;
        parameters["spmid"] = "united.player-video-detail.0.0";

        parameters["sign"] = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);

        return parameters;
    }

    /// <summary>
    /// 构造上报完成（deliver/task/complete）的请求体
    /// </summary>
    /// <param name="taskId">开始观看返回的 task_id</param>
    /// <param name="token">开始观看返回的 token</param>
    /// <param name="ck">账号 cookie（access_key 未配置时省略该参数）</param>
    public static Dictionary<string, string> BuildComplete(long taskId, string token, BiliCookie ck)
    {
        var parameters = BuildCommon(ck);
        parameters["task_id"] = taskId.ToString();
        parameters["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        parameters["token"] = token;

        // task_sign：先排除 task_sign 和 sign，签名一次
        parameters["task_sign"] = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);
        // sign：再带上 task_sign，签名一次
        parameters["sign"] = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);

        return parameters;
    }

    /// <summary>
    /// 按抓包还原的 APP 公共参数
    /// </summary>
    private static Dictionary<string, string> BuildCommon(BiliCookie ck)
    {
        var parameters = new Dictionary<string, string>
        {
            ["appkey"] = AppSignHelper.AppKey,
            ["build"] = "9070300",
            ["c_locale"] = "zh-Hans_HK",
            ["channel"] = "html5_app_bili",
            ["disable_rcmd"] = "0",
            ["mobi_app"] = "android",
            ["platform"] = "android",
            ["s_locale"] = "zh-Hans_HK",
            ["statistics"] = "{\"appId\":1,\"platform\":3,\"version\":\"9.7.0\",\"abtest\":\"\"}",
            ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
        };

        if (!string.IsNullOrEmpty(ck.AccessKey))
            parameters["access_key"] = ck.AccessKey;

        return parameters;
    }
}
