using System.Collections.Generic;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;
using Xunit;

namespace BiliAgentTest;

public class MobileHeartbeatRequestTest
{
    private static BiliCookie CreateCookie() =>
        new(
            new Dictionary<string, string>
            {
                ["DedeUserID"] = "123",
                ["SESSDATA"] = "sess",
                ["bili_jct"] = "jct",
                ["buvid3"] = "buvid",
                ["access_key"] = "ak",
            }
        );

    [Fact]
    public void BuildOpening_OmitsAccessKeyAndHasZeroProgress()
    {
        var parameters = MobileHeartbeatRequest.BuildOpening(
            CreateCookie(),
            861312341,
            926248568,
            328482,
            12548,
            1450,
            "session1"
        );

        Assert.False(parameters.ContainsKey("access_key"));
        Assert.Equal("0", parameters["actual_played_time"]);
        Assert.Equal("0", parameters["played_time"]);
        Assert.Equal("0", parameters["paused_time"]);
        Assert.Equal("0", parameters["total_time"]);
        Assert.Equal("0", parameters["start_ts"]);
        Assert.Equal("0", parameters["last_play_progress_time"]);
        Assert.Equal("0", parameters["max_play_progress_time"]);
        Assert.Equal("99", parameters["auto_play"]);
        Assert.Equal("4", parameters["type"]);
        Assert.Equal("1", parameters["sub_type"]);
        Assert.Equal("328482", parameters["epid"]);
        Assert.Equal("12548", parameters["sid"]);
        Assert.Equal("session1", parameters["session"]);
        Assert.Equal(AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec), parameters["sign"]);
    }

    [Fact]
    public void BuildPlaying_SatisfiesTimeInvariantsAndOmitsAccessKey()
    {
        const int playedTime = 475;
        const long startTs = 1787619121;
        var parameters = MobileHeartbeatRequest.BuildPlaying(
            CreateCookie(),
            861312341,
            926248568,
            328482,
            12548,
            1450,
            playedTime,
            startTs,
            "session2"
        );

        Assert.False(parameters.ContainsKey("access_key"));
        Assert.Equal("475", parameters["played_time"]);
        Assert.Equal("475", parameters["actual_played_time"]);
        Assert.Equal("0", parameters["paused_time"]);
        Assert.Equal("475", parameters["total_time"]);
        Assert.Equal(startTs.ToString(), parameters["start_ts"]);
        Assert.Equal((startTs + playedTime).ToString(), parameters["ts"]);
        Assert.Equal("2", parameters["play_type"]);
        Assert.Equal("0", parameters["auto_play"]);
        Assert.Equal(
            int.Parse(parameters["played_time"]) + int.Parse(parameters["paused_time"]),
            int.Parse(parameters["total_time"])
        );
        Assert.Equal(AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec), parameters["sign"]);
    }
}
