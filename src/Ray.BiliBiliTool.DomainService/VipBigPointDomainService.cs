using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint.ThreeDaysSign;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ShowApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

public class VipBigPointDomainService(
    ILogger<VipBigPointDomainService> logger,
    IOptionsMonitor<VipBigPointOptions> vipBigPointOptions,
    IShowApi showApi,
    IApiApi apiApi,
    IAccountDomainService accountDomainService,
    IVideoDomainService videoDomainService
) : IVipBigPointDomainService
{
    private readonly VipBigPointOptions _vipBigPointOptions = vipBigPointOptions.CurrentValue;

    public async Task<VipBigPointCombine> GetCombineAsync(BiliCookie ck)
    {
        var allTasks = await apiApi.GetCombineAsync(
            new GetCombineRequest { csrf = ck.BiliJct, buvid = ck.Buvid },
            ck.ToString()
        );
        if (allTasks.Code != 0)
            throw new BiliBusinessException(allTasks.ToJsonStr());
        return allTasks.Data;
    }

    /// <summary>
    /// 领取大会员专属等级加速包
    /// </summary>
    public async Task VipExpressAsync(BiliCookie ck)
    {
        var re = await apiApi.GetVouchersInfoAsync(ck.ToString());
        if (re.Code == 0)
        {
            var state = re.Data.List.Find(x => x.Type == 9)?.State;

            switch (state)
            {
                case 2:
                    logger.LogInformation("大会员经验观看任务未完成");
                    logger.LogInformation("开始观看视频");
                    // 观看视频，暂时没有好办法解决，先这样使着
                    DailyTaskInfo dailyTaskInfo = await accountDomainService.GetDailyTaskStatus(ck);
                    await videoDomainService.WatchAndShareVideo(dailyTaskInfo, ck);
                    // 跳转到未兑换，执行兑换任务
                    goto case 0;

                case 1:
                    logger.LogInformation("大会员经验已兑换");
                    break;

                case 0:
                    logger.LogInformation("大会员经验未兑换");
                    //兑换api
                    var response = await apiApi.ObtainVipExperienceAsync(
                        new VipExperienceRequest { csrf = ck.BiliJct },
                        ck.ToString()
                    );
                    if (response.Code != 0)
                    {
                        logger.LogInformation(
                            "大会员经验领取失败，错误信息：{message}",
                            response.Message
                        );
                        break;
                    }

                    logger.LogInformation("领取成功，经验+10 √");
                    var combine = await GetCombineAsync(ck);
                    combine.LogPointInfo(logger);
                    break;

                default:
                    logger.LogDebug("大会员经验领取失败，未知错误");
                    break;
            }
        }
    }

    /// <summary>
    /// 签到
    /// </summary>
    /// <param name="ck"></param>
    /// <exception cref="Exception"></exception>
    public async Task SignAsync(BiliCookie ck)
    {
        var signInfo = await apiApi.GetThreeDaySignAsync(
            new ThreeDaySignRequest { csrf = ck.BiliJct },
            ck.ToString()
        );
        if (signInfo.Data.three_day_sign.signed)
        {
            logger.LogInformation("已完成，跳过");
            logger.LogInformation(signInfo.Data.ToString());
            return;
        }

        BiliApiResponse<Sign2Response> re = await apiApi.Sign2Async(
            new Sign2RequestPath(ck.BiliJct),
            new Sign2Request(),
            ck.ToString()
        );
        if (re.Code != 0)
            throw new BiliBusinessException(re.ToJsonStr());

        logger.LogInformation("签到成功");
        logger.LogInformation(re.Data.ToString());

        signInfo = await apiApi.GetThreeDaySignAsync(
            new ThreeDaySignRequest { csrf = ck.BiliJct },
            ck.ToString()
        );
        signInfo.Data.LogPointInfo(logger);
    }

    /// <summary>
    /// 领取任务
    /// </summary>
    /// <param name="combine"></param>
    /// <param name="ck"></param>
    public async Task ReceiveDailyMissionsAsync(VipBigPointCombine combine, BiliCookie ck)
    {
        const string moduleCode = "日常任务";

        var module = combine.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
        var missionsNeedReceive = module
            ?.common_task_item.Where(x => x.state == 0)
            .Where(x => !PurchaseTaskCodes.Contains(x.task_code))
            .ToList();
        if (missionsNeedReceive == null || missionsNeedReceive.Count == 0)
        {
            logger.LogInformation("均已领取，跳过");
            return;
        }

        foreach (var targetTask in missionsNeedReceive)
        {
            logger.LogInformation("开始领取任务：{task}", targetTask.title);
            await TryReceive(targetTask.task_code, ck);
        }
    }

    public async Task ReceiveAndCompleteAsync(
        VipBigPointCombine info,
        string moduleCode,
        string taskCode,
        BiliCookie ck,
        Func<string, BiliCookie, Task<bool>> completeFunc
    )
    {
        var module = info.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
        var bonusTask = module?.common_task_item.FirstOrDefault(x => x.task_code == taskCode);

        if (bonusTask == null)
        {
            logger.LogInformation("任务失效");
            return;
        }

        if (bonusTask.state == 3)
        {
            logger.LogInformation("已完成，跳过");
            return;
        }

        if (bonusTask.state == 0)
        {
            logger.LogInformation("开始领取任务");
            await TryReceive(bonusTask.task_code, ck);
        }

        logger.LogInformation("开始完成任务");
        var re = await completeFunc(taskCode, ck);

        //确认
        if (re)
        {
            var combine = await GetCombineAsync(ck);
            module = combine.Task_info.Modules.FirstOrDefault(x => x.module_title == moduleCode);
            bonusTask = module?.common_task_item.FirstOrDefault(x => x.task_code == taskCode);
            var success = bonusTask is { state: 3, complete_times: >= 1 };
            logger.LogInformation("确认：{re}", success ? "成功，经验 +10" : "失败");
        }
    }

    public async Task<bool> CompleteAsync(string taskCode, BiliCookie ck)
    {
        var request = new ReceiveOrCompleteTaskRequest(taskCode);
        var re = await apiApi.VipBigPointCompleteAsync(request, ck.ToString());
        if (re.Code == 0)
        {
            logger.LogInformation("已完成");
            return true;
        }

        logger.LogInformation("失败：{msg}", re.ToJsonStr());
        return false;
    }

    public async Task<bool> CompleteViewAsync(string taskCode, BiliCookie ck)
    {
        var channel = taskCode switch
        {
            "animatetab" => "jp_channel",
            "filmtab" => "tv_channel",
            _ => throw new ArgumentOutOfRangeException(
                nameof(taskCode),
                $"Invalid taskCode: {taskCode}"
            ),
        };

        logger.LogInformation("开始浏览");
        await Task.Delay(10 * 1000);

        var request = new ViewRequest(channel);
        var re = await apiApi.VipBigPointViewComplete(request, ck.ToString());
        if (re.Code == 0)
        {
            logger.LogInformation("浏览完成");
            return true;
        }

        logger.LogInformation("浏览失败：{msg}", re.ToJsonStr());
        return false;
    }

    public async Task<bool> CompleteViewVipMallAsync(string taskCode, BiliCookie ck)
    {
        var re = await showApi.ViewVipMallAsync(
            new ViewVipMallRequest { Csrf = ck.BiliJct },
            ck.ToString()
        );
        if (re.Code != 0)
            throw new BiliBusinessException(re.ToJsonStr());
        return true;
    }

    public async Task<bool> CompleteV2Async(string taskCode, BiliCookie ck)
    {
        var request = ScoreTaskV2Request.Build(taskCode, ck);
        var re = await apiApi.VipBigPointCompleteV2(request, ck.ToString(), ck.Buvid);
        if (re.Code == 0)
        {
            logger.LogInformation("已完成");
            return true;
        }

        logger.LogInformation("失败：{msg}", re.ToJsonStr());
        return false;
    }

    /// <summary>
    /// 完成观看剧集任务（APP deliver 流程：开始观看 → 心跳上报 → 上报完成）
    /// </summary>
    /// <remarks>
    /// 旧的 score/task/complete/v2 对该任务已返回 -400，
    /// 需先经 deliver/material/receive 获取 task_id 与 token，
    /// 再经 heartbeat/mobile 上报观看进度，最后经 deliver/task/complete 上报完成（只能成功一次）。
    /// </remarks>
    public async Task<bool> CompleteOgvWatchAsync(BiliCookie ck)
    {
        //开始观看任务
        var startRe = await apiApi.StartOgvWatchAsync(
            OgvWatchRequest.BuildStart(ck),
            ck.ToString(),
            ck.Buvid
        );
        if (startRe.Code != 0)
        {
            logger.LogInformation("开始观看剧集任务失败：{msg}", startRe.ToJsonStr());
            return false;
        }

        var watchCfg = startRe.Data?.watch_count_down_cfg;
        if (
            watchCfg == null
            || !long.TryParse(watchCfg.task_id, out long taskId)
            || string.IsNullOrEmpty(watchCfg.token)
        )
        {
            logger.LogInformation("开始观看剧集任务失败：响应缺少 task_id/token");
            return false;
        }

        try
        {
            await ReportOgvWatchHeartbeatAsync(ck, watchCfg.milliseconds);
        }
        catch (Exception e)
        {
            logger.LogInformation("观看上报异常：{msg}", e.Message);
        }

        //上报完成
        var re = await apiApi.CompleteOgvWatchAsync(
            OgvWatchRequest.BuildComplete(taskId, watchCfg.token, ck),
            ck.ToString(),
            ck.Buvid
        );
        if (re.Code == 0)
        {
            logger.LogInformation("已完成");
            return true;
        }

        logger.LogInformation("失败：{msg}", re.ToJsonStr());
        return false;
    }

    #region private

    /// <summary>
    /// 需要购买才能完成的任务，不领取
    /// </summary>
    private static readonly HashSet<string> PurchaseTaskCodes =
    [
        "vipmallbuy",
        "tvodbuy",
        "dressbuyamount",
    ];

    /// <summary>
    /// 领取任务
    /// </summary>
    private async Task TryReceive(string taskCode, BiliCookie ck)
    {
        BiliApiResponse? re = null;
        try
        {
            var request = ScoreTaskV2Request.Build(taskCode, ck);
            re = await apiApi.VipBigPointReceiveV2(request, ck.ToString(), ck.Buvid);
            if (re.Code == 0)
                logger.LogInformation("领取任务成功");
            else
                logger.LogInformation("领取任务失败：{msg}", re.ToJsonStr());
        }
        catch (Exception e)
        {
            logger.LogError("领取任务异常");
            logger.LogError(e.Message + re?.ToJsonStr());
        }
    }

    /// <summary>
    /// 上报剧集观看心跳：开场一次 + 播放中一次。
    /// 观看秒数优先用 deliver 返回的倒计时，至少 15 秒。
    /// </summary>
    private async Task ReportOgvWatchHeartbeatAsync(BiliCookie ck, long countdownMs)
    {
        if (
            !long.TryParse(OgvWatchRequest.SeasonId, out long sid)
            || !long.TryParse(OgvWatchRequest.EpId, out long epid)
        )
        {
            logger.LogInformation("观看上报跳过：剧集 id 无效");
            return;
        }

        var episode = await GetOgvWatchEpisodeAsync(sid, epid, ck);
        if (episode is null)
        {
            logger.LogInformation("观看上报跳过：未能获取剧集信息");
            return;
        }

        long aid = episode.aid;
        epid = episode.ep_id;
        int videoDuration =
            episode.duration >= 10_000
                ? episode.duration / 1000
                : Math.Max(episode.duration, 15);
        int watchSeconds = countdownMs > 0 ? (int)(countdownMs / 1000) : 15;
        int playedTime = Math.Clamp(Math.Max(watchSeconds, 15), 1, videoDuration);
        long startTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string session = MobileHeartbeatRequest.NewSession();

        logger.LogInformation("开始上报观看进度：{title}", episode.share_copy);

        var opening = await apiApi.UploadMobileHeartbeat(
            MobileHeartbeatRequest.BuildOpening(
                ck,
                aid,
                episode.cid,
                epid,
                sid,
                videoDuration,
                session
            ),
            ck.ToString(),
            ck.Buvid
        );
        if (opening.Code != 0)
        {
            logger.LogInformation("开场心跳失败：{msg}", opening.ToJsonStr());
            return;
        }

        await Task.Delay(Math.Min(playedTime, 3) * 1000);

        var playing = await apiApi.UploadMobileHeartbeat(
            MobileHeartbeatRequest.BuildPlaying(
                ck,
                aid,
                episode.cid,
                epid,
                sid,
                videoDuration,
                playedTime,
                startTs,
                session
            ),
            ck.ToString(),
            ck.Buvid
        );
        if (playing.Code == 0)
            logger.LogInformation("观看上报成功，已观看到第{playedTime}秒", playedTime);
        else
            logger.LogInformation("播放心跳失败：{msg}", playing.ToJsonStr());
    }

    private async Task<bool> WatchBangumi(BiliCookie ck)
    {
        if (_vipBigPointOptions.ViewBangumiList.Count == 0)
            return false;

        long randomSsid = _vipBigPointOptions.ViewBangumiList[
            Random.Shared.Next(0, _vipBigPointOptions.ViewBangumiList.Count)
        ];

        var res = await GetBangumi(randomSsid, ck);
        if (res is null)
        {
            return false;
        }

        var videoInfo = res.Value.Item1;

        // 随机播放时间
        int playedTime = Random.Shared.Next(905, 1800);
        // 观看该视频
        var request = new UploadVideoHeartbeatRequest()
        {
            Aid = long.Parse(videoInfo.Aid),
            Bvid = videoInfo.Bvid,
            Cid = videoInfo.Cid,
            Mid = long.Parse(ck.UserId),
            Sid = randomSsid,
            Epid = res.Value.Item2,
            Csrf = ck.BiliJct,
            Type = 4,
            Sub_type = 1,
            Start_ts = DateTime.Now.ToTimeStamp() - playedTime,
            Played_time = playedTime,
            Realtime = playedTime,
            Real_played_time = playedTime,
        };
        BiliApiResponse apiResponse = await apiApi.UploadVideoHeartbeat(
            request.Aid,
            request.Played_time,
            request,
            ck.ToString()
        );
        if (apiResponse.Code == 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 按 season + ep 取观看上报目标，优先匹配开始观看时使用的剧集
    /// </summary>
    private async Task<Episode?> GetOgvWatchEpisodeAsync(long sid, long epid, BiliCookie ck)
    {
        try
        {
            var bangumiInfo = await apiApi.GetBangumiBySsid(sid, ck.ToString());
            if (bangumiInfo.Result.episodes.Count == 0)
                return null;

            return bangumiInfo.Result.episodes.FirstOrDefault(x => x.ep_id == epid)
                ?? bangumiInfo.Result.episodes[0];
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            return null;
        }
    }

    /// <summary>
    /// 从自定义的番剧ssid中选择其中的一部中的一集
    /// </summary>
    /// <param name="randomSsid">番剧ssid</param>
    /// <returns></returns>
    private async Task<(VideoInfoDto, long)?> GetBangumi(long randomSsid, BiliCookie ck)
    {
        try
        {
            if (randomSsid is 0 or long.MinValue)
                return null;
            var bangumiInfo = await apiApi.GetBangumiBySsid(randomSsid, ck.ToString());

            // 从获取的剧集中随机获得其中的一集

            var bangumi = bangumiInfo.Result.episodes[
                Random.Shared.Next(0, bangumiInfo.Result.episodes.Count)
            ];
            var videoInfo = new VideoInfoDto()
            {
                Bvid = bangumi.bvid,
                Aid = bangumi.aid.ToString(),
                Cid = bangumi.cid,
                Copyright = 1,
                Duration = bangumi.duration,
                Title = bangumi.share_copy,
            };
            logger.LogInformation("本次播放的正片为：{title}", bangumi.share_copy);
            return (videoInfo, bangumi.ep_id);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
        }

        return null;
    }

    #endregion
}
