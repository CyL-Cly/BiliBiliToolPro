using System.Text;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint.ThreeDaysSign;

public class ThreeDaySignResponse
{
    public BigPointDto? big_point { get; set; }

    public ThreeDaySignDto? three_day_sign { get; set; }

    public override string ToString()
    {
        if (three_day_sign is null)
            return "(签到响应缺少 three_day_sign)";

        var sb = new StringBuilder();
        sb.AppendLine($"今日获得签到积分: {three_day_sign.score}");
        sb.AppendLine($"累计签到: {three_day_sign.count}/{three_day_sign.duration} 天");

        if (three_day_sign.count < 3)
        {
            sb.AppendLine(
                $"{three_day_sign.duration} 天内累计签到 3 天，可额外获取 {three_day_sign.exp_value} 经验"
            );
        }
        else
        {
            sb.AppendLine($"满 3 天，已获得额外 {three_day_sign.vip_score} 经验");
        }

        return sb.ToString();
    }

    public void LogPointInfo(ILogger logger)
    {
        if (big_point is null)
        {
            logger.LogWarning("签到响应缺少 big_point");
            return;
        }
        logger.LogInformation("当前经验：{point}", big_point.point);
    }
}
