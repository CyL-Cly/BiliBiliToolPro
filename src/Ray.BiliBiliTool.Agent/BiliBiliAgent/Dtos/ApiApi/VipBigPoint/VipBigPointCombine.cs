using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;

public class VipBigPointCombine
{
    public PointInfo? point_info { get; set; }
    public TaskInfo? Task_info { get; set; }

    public void LogFullInfo(ILogger logger)
    {
        if (point_info is null)
        {
            logger.LogWarning("combine 响应缺少 point_info");
            return;
        }
        logger.LogInformation("当前经验：{point}", point_info.point);
        foreach (var moduleItem in Task_info?.Modules ?? Enumerable.Empty<ModuleItem>())
        {
            logger.LogInformation("-{title}", moduleItem.module_title);
            foreach (var commonTaskItem in moduleItem.common_task_item)
            {
                logger.LogInformation(
                    "---{title}：{status}",
                    commonTaskItem.title,
                    commonTaskItem.state == 3 ? "√" : "X"
                );
            }
        }
    }

    public void LogPointInfo(ILogger logger)
    {
        if (point_info is null)
        {
            logger.LogWarning("combine 响应缺少 point_info");
            return;
        }
        logger.LogInformation("当前经验：{point}", point_info.point);
    }
}
