using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;
using Xunit;

namespace BiliAgentTest;

public class OgvWatchRequestTest
{
    [Fact]
    public void BuildComplete_TaskSignAndSign_AreDoubleSignedConsistently()
    {
        // Arrange & Act
        var parameters = OgvWatchRequest.BuildComplete(4320003, "a78ecb9553");

        // Assert: task_sign = 排除 task_sign 与 sign 后签名一次
        var taskSign = parameters["task_sign"];
        parameters.Remove("task_sign");
        var expectTaskSign = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);
        Assert.Equal(expectTaskSign, taskSign);

        // Assert: sign = 带上 task_sign 后再签名一次
        parameters["task_sign"] = taskSign;
        var expectSign = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);
        Assert.Equal(expectSign, parameters["sign"]);
    }

    [Fact]
    public void BuildComplete_FollowsCapturedParameterShape()
    {
        var parameters = OgvWatchRequest.BuildComplete(4320003, "a78ecb9553");

        Assert.Equal("4320003", parameters["task_id"]);
        Assert.Equal("a78ecb9553", parameters["token"]);
        Assert.Equal(AppSignHelper.AppKey, parameters["appkey"]);
        Assert.Equal("9070300", parameters["build"]);
        Assert.Equal("android", parameters["mobi_app"]);
        Assert.Equal("android", parameters["platform"]);
        Assert.Equal("html5_app_bili", parameters["channel"]);
        Assert.Equal(
            "{\"appId\":1,\"platform\":3,\"version\":\"9.7.0\",\"abtest\":\"\"}",
            parameters["statistics"]
        );
        Assert.True(long.TryParse(parameters["timestamp"], out long tsMs) && tsMs > 0);
        Assert.True(long.TryParse(parameters["ts"], out long ts) && ts > 0);
    }

    [Fact]
    public void BuildStart_ContainsEpisodeAndSelfConsistentSign()
    {
        var parameters = OgvWatchRequest.BuildStart();

        Assert.Equal("12548", parameters["season_id"]);
        Assert.Equal("328482", parameters["ep_id"]);
        Assert.Equal("united.player-video-detail.0.0", parameters["spmid"]);

        var expectSign = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);
        Assert.Equal(expectSign, parameters["sign"]);
    }
}
