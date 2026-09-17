using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint.ThreeDaysSign;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ShowApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

public class VipBigPointDomainService(
    ILogger<VipBigPointDomainService> logger,
    IShowApi showApi,
    IApiApi apiApi,
    IAccountDomainService accountDomainService,
    IVideoDomainService videoDomainService
) : IVipBigPointDomainService
{
    public async Task<VipBigPointCombine> GetCombineAsync(BiliCookie ck)
    {
        var allTasks = await apiApi.GetCombineAsync(
            new GetCombineRequest { csrf = ck.BiliJct, buvid = ck.Buvid },
            ck.ToString()
        );
        if (allTasks.Code != 0)
            throw new BiliBusinessException(allTasks.ToJsonStr());
        if (allTasks.Data?.Task_info is null)
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
            var state = re.Data!.List.Find(x => x.Type == 9)?.State;

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
                            "大会员经验领取失败，响应原文：{msg}",
                            response.ToJsonStr()
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
        else
        {
            logger.LogInformation("获取大会员特权信息失败：{msg}", re.ToJsonStr());
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
        if (signInfo.Data?.three_day_sign is null)
        {
            logger.LogInformation("签到信息缺失：{msg}", signInfo.ToJsonStr());
            return;
        }

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
        logger.LogInformation(re.Data!.ToString());

        signInfo = await apiApi.GetThreeDaySignAsync(
            new ThreeDaySignRequest { csrf = ck.BiliJct },
            ck.ToString()
        );
        if (signInfo.Data?.three_day_sign is null)
        {
            logger.LogInformation("签到信息缺失：{msg}", signInfo.ToJsonStr());
            return;
        }
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

        var module = combine.Task_info?.Modules.FirstOrDefault(x => x.module_title == moduleCode);
        var missionsNeedReceive = module
            ?.common_task_item.Where(x => x.state == 0)
            .Where(x => !PurchaseTaskCodes.Contains(x.task_code ?? ""))
            .ToList();
        if (missionsNeedReceive == null || missionsNeedReceive.Count == 0)
        {
            logger.LogInformation("均已领取，跳过");
            return;
        }

        foreach (var targetTask in missionsNeedReceive)
        {
            if (string.IsNullOrEmpty(targetTask.task_code))
            {
                logger.LogWarning("任务缺少 task_code，跳过");
                continue;
            }
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
        var module = info.Task_info?.Modules.FirstOrDefault(x => x.module_title == moduleCode);
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
            await TryReceive(taskCode, ck);
        }

        logger.LogInformation("开始完成任务");
        var re = await completeFunc(taskCode, ck);

        //确认
        if (re)
        {
            var combine = await GetCombineAsync(ck);
            module = combine.Task_info?.Modules.FirstOrDefault(x => x.module_title == moduleCode);
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
    public async Task<bool> CompleteOgvWatchAsync(BiliCookie ck, CancellationToken ct = default)
    {
        //开始观看任务
        var startRe = await apiApi.StartOgvWatchAsync(
            OgvWatchRequest.BuildStart(),
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
            logger.LogInformation(
                "开始观看剧集任务失败：响应缺少 task_id/token：{msg}",
                startRe.ToJsonStr()
            );
            return false;
        }

        long countdownMs = watchCfg.milliseconds;
        if (countdownMs <= 0)
        {
            logger.LogInformation(
                "观看倒计时配置无效（milliseconds={ms}），按保守值 600 秒推进",
                countdownMs
            );
            countdownMs = 600_000;
        }

        try
        {
            var watched = await ReportOgvWatchHeartbeatAsync(ck, countdownMs, ct);
            if (!watched)
                logger.LogInformation("观看心跳未完整推进，仍尝试上报完成");
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogInformation("观看上报异常：{msg}", e.Message);
        }

        //上报完成
        var re = await apiApi.CompleteOgvWatchAsync(
            OgvWatchRequest.BuildComplete(taskId, watchCfg.token),
            ck.ToString(),
            ck.Buvid
        );
        bool completeOk = re.Code == 0;
        if (completeOk)
        {
            logger.LogInformation("已完成");
        }
        else
        {
            logger.LogInformation("失败：{msg}", re.ToJsonStr());
        }

        //复核：以 combine 中 ogvwatchnew 的实际状态为准
        try
        {
            var combine = await GetCombineAsync(ck);
            var state = combine
                .Task_info?.Modules.SelectMany(x => x.common_task_item)
                .FirstOrDefault(x => x.task_code == "ogvwatchnew")
                ?.state;
            if (state is null)
            {
                logger.LogInformation("复核：未找到 ogvwatchnew 任务");
                return completeOk;
            }
            logger.LogInformation("复核：ogvwatchnew.state={state}", state);
            return state == 3;
        }
        catch (Exception e)
        {
            logger.LogInformation("复核异常：{msg}", e.Message);
            return completeOk;
        }
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
    /// 上报剧集观看心跳：开场一次 + 播放中每 60 秒一步，直到累计观看达到目标时长 + 30 秒缓冲。
    /// 目标秒数优先用 deliver 返回的倒计时，钳制到 [15, 1800] 秒。
    /// </summary>
    /// <returns>是否完整推进到目标观看时长</returns>
    private async Task<bool> ReportOgvWatchHeartbeatAsync(
        BiliCookie ck,
        long countdownMs,
        CancellationToken ct = default
    )
    {
        if (
            !long.TryParse(OgvWatchRequest.SeasonId, out long sid)
            || !long.TryParse(OgvWatchRequest.EpId, out long epid)
        )
        {
            logger.LogInformation("观看上报跳过：剧集 id 无效");
            return false;
        }

        var episode = await GetOgvWatchEpisodeAsync(sid, epid, ck);
        if (episode is null)
        {
            logger.LogInformation("观看上报跳过：未能获取剧集信息");
            return false;
        }

        long aid = episode.aid;
        epid = episode.ep_id;
        //接口返回毫秒（实测 season 接口 duration=7914625 对应约 7914 秒）；兼容旧的秒级数据：秒值不可能 >= 10000
        int videoDuration =
            episode.duration >= 10_000 ? episode.duration / 1000 : Math.Max(episode.duration, 15);
        int rawTarget = (int)(countdownMs / 1000);
        int target = Math.Clamp(rawTarget, 15, 1800);
        if (rawTarget < 15 || rawTarget > 1800)
        {
            logger.LogInformation("倒计时时长异常，已钳制到 {target} 秒", target);
        }
        if (videoDuration < target + 30)
        {
            logger.LogInformation(
                "观看上报跳过：该集时长 {videoDuration} 秒不足目标 {target} 秒 + 30 秒缓冲",
                videoDuration,
                target
            );
            return false;
        }
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
            return false;
        }

        long startTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        //播放中：每 60 秒一步心跳，累计观看达到 target + 30 秒缓冲后结束
        int elapsed = 0;
        while (true)
        {
            await Task.Delay(60_000, ct);
            elapsed += 60;

            var playing = await apiApi.UploadMobileHeartbeat(
                MobileHeartbeatRequest.BuildPlaying(
                    ck,
                    aid,
                    episode.cid,
                    epid,
                    sid,
                    videoDuration,
                    elapsed,
                    startTs,
                    session
                ),
                ck.ToString(),
                ck.Buvid
            );
            logger.LogInformation("已连续观看 {elapsed}/{target} 秒", elapsed, target);
            if (playing.Code != 0)
                logger.LogInformation("播放心跳失败：{msg}", playing.ToJsonStr());

            if (elapsed >= target + 30)
                return true;
        }
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

            var ep = bangumiInfo.Result.episodes.FirstOrDefault(x => x.ep_id == epid);
            if (ep is null)
            {
                ep = bangumiInfo.Result.episodes[0];
                logger.LogInformation("目标 ep 未找到，回退到第一集：{ep}", ep.ep_id);
            }
            return ep;
        }
        catch (Exception e)
        {
            logger.LogError(e, "获取剧集信息失败");
            return null;
        }
    }

    #endregion
}
