namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;

/// <summary>
/// deliver/material/receive 开始观看剧集任务的响应
/// </summary>
public class StartOgvWatchResponse
{
    public string? closeType { get; set; }

    public string? showTime { get; set; }

    public WatchCountDownCfg? watch_count_down_cfg { get; set; }
}

/// <summary>
/// 观看倒计时配置，包含标识该次观看任务的 task_id 与 token
/// </summary>
public class WatchCountDownCfg
{
    public string? task_id { get; set; }

    public string? token { get; set; }

    public long milliseconds { get; set; }
}
