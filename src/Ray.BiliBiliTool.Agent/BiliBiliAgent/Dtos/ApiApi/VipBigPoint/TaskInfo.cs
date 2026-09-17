namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;

public class TaskInfo
{
    public int Score_month { get; set; }

    public int Score_limit { get; set; }

    public List<ModuleItem> Modules { get; set; } = [];
}

public class ModuleItem
{
    public string? module_title { get; set; }

    public List<CommonTaskItem> common_task_item { get; set; } = [];
}

public class CommonTaskItem
{
    public string? title { get; set; }

    public string? subtitle { get; set; }

    public string? explain { get; set; }

    public string? task_code { get; set; }

    /// <summary>
    /// 状态 0-未领取 3-已完成
    /// </summary>
    public int state { get; set; }

    public int vip_limit { get; set; }

    public int complete_times { get; set; }

    public int max_times { get; set; }

    public int recall_num { get; set; }
}
