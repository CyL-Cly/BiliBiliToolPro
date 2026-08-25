using System.Security.Cryptography;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;

/// <summary>
/// 移动端播放心跳 <c>POST /x/report/heartbeat/mobile</c> 请求体构造
/// </summary>
/// <remarks>
/// 喂给观看历史与续播。不发送 access_key，登录态走 Cookie。
/// 开场心跳全部进度为 0；播放中心跳满足
/// played_time + paused_time = total_time，且 ts − start_ts ≈ total_time。
/// </remarks>
public static class MobileHeartbeatRequest
{
    public static string NewSession() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// 开场心跳（进度全 0）
    /// </summary>
    public static Dictionary<string, string> BuildOpening(
        BiliCookie ck,
        long aid,
        long cid,
        long epid,
        long sid,
        int videoDuration,
        string session
    )
    {
        var parameters = BuildCommon(ck, aid, cid, epid, sid, videoDuration, session);
        parameters["actual_played_time"] = "0";
        parameters["auto_play"] = "99";
        parameters["last_play_progress_time"] = "0";
        parameters["max_play_progress_time"] = "0";
        parameters["paused_time"] = "0";
        parameters["played_time"] = "0";
        parameters["start_ts"] = "0";
        parameters["total_time"] = "0";
        parameters["sign"] = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);
        return parameters;
    }

    /// <summary>
    /// 播放中心跳
    /// </summary>
    public static Dictionary<string, string> BuildPlaying(
        BiliCookie ck,
        long aid,
        long cid,
        long epid,
        long sid,
        int videoDuration,
        int playedTime,
        long startTs,
        string session
    )
    {
        var parameters = BuildCommon(ck, aid, cid, epid, sid, videoDuration, session);
        parameters["actual_played_time"] = playedTime.ToString();
        parameters["auto_play"] = "0";
        parameters["last_play_progress_time"] = playedTime.ToString();
        parameters["max_play_progress_time"] = playedTime.ToString();
        parameters["paused_time"] = "0";
        parameters["play_type"] = "2";
        parameters["played_time"] = playedTime.ToString();
        parameters["start_ts"] = startTs.ToString();
        parameters["total_time"] = playedTime.ToString();
        parameters["ts"] = (startTs + playedTime).ToString();
        parameters["sign"] = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);
        return parameters;
    }

    private static Dictionary<string, string> BuildCommon(
        BiliCookie ck,
        long aid,
        long cid,
        long epid,
        long sid,
        int videoDuration,
        string session
    )
    {
        return new Dictionary<string, string>
        {
            ["aid"] = aid.ToString(),
            ["appkey"] = AppSignHelper.AppKey,
            ["build"] = "9070300",
            ["c_locale"] = "zh-Hans_HK",
            ["channel"] = "html5_app_bili",
            ["cid"] = cid.ToString(),
            ["disable_rcmd"] = "0",
            ["epid"] = epid.ToString(),
            ["epid_status"] = "13",
            ["extra"] = "{\"from_outer_spmid\":\"activity.h5.0.0\",\"inline_type\":\"1\"}",
            ["from"] = "24",
            ["from_spmid"] = "activity.h5.0.0",
            ["is_audio_play"] = "2",
            ["is_auto_qn"] = "1",
            ["list_play_time"] = "0",
            ["mid"] = ck.UserId,
            ["miniplayer_play_time"] = "0",
            ["mobi_app"] = "android",
            ["network_type"] = "1",
            ["platform"] = "android",
            ["play_mode"] = "1",
            ["play_status"] = "1",
            ["polaris_action_id"] = NewPolarisActionId(),
            ["quality"] = "80",
            ["s_locale"] = "zh-Hans_HK",
            ["session"] = session,
            ["sid"] = sid.ToString(),
            ["spmid"] = "united.player-video-detail.0.0",
            ["statistics"] = "{\"appId\":1,\"platform\":3,\"version\":\"9.7.0\",\"abtest\":\"\"}",
            ["sub_type"] = "1",
            ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["type"] = "4",
            ["user_status"] = "1",
            ["video_duration"] = videoDuration.ToString(),
        };
    }

    private static string NewPolarisActionId()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
