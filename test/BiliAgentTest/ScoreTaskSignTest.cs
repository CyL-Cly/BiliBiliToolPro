using System.Collections.Generic;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;
using Xunit;

namespace BiliAgentTest;

public class ScoreTaskSignTest
{
    private const string AccessKeyFixture =
        "804aec6709824563629b76436c967481CjC33ps79WWaYJ9_vfOfkzntzYqAO0W5Nklh3YpDniZRtYfjpY_V6BRZrjVSeMK5USESVlhQNldIaEhQN0hwUWtlaXZKemJiMWZ5OUxnbmpDc1NveWR0OU9nUWt6dmVxbjZKeFFzVDI2Snc4LVNneVhHSE9iSjlrdE02anFacVM0aFg5Tk5FeFd3IIEC";

    /// <summary>
    /// 按 APP 抓包还原的公共参数（receive/v2 与 complete/v2 一致，仅 taskCode 与 ts 不同）
    /// </summary>
    private static Dictionary<string, string> BuildCapturedParameters(string taskCode, string ts)
    {
        const string fingerprint = "60b730e057a55f98a3dc8a5aea9bca122026081821304720bb6ffee7befa37fa";
        const string buvid = "XU68CE26E75C973C8833FD1FF69CF8546CEBC";

        return new Dictionary<string, string>
        {
            ["access_key"] = AccessKeyFixture,
            ["alwaysTranslate"] = "false",
            ["appKey"] = AppSignHelper.AppKey,
            ["appkey"] = AppSignHelper.AppKey,
            ["bili_local_id"] = fingerprint,
            ["brand"] = "nubia",
            ["build"] = "9070300",
            ["buvid"] = buvid,
            ["channel"] = "html5_app_bili",
            ["containerName"] = "AbstractWebActivity",
            ["csrf"] = "cb0d49a38143ec02a4d8eef02380a2a0",
            ["device"] = "phone",
            ["deviceId"] = "111647b8970e29c2",
            ["deviceName"] = "nubiaNX666J",
            ["devicePlatform"] = "Android12nubiaNX666J",
            ["device_id"] = fingerprint,
            ["device_name"] = "nubiaNX666J",
            ["device_platform"] = "Android12nubiaNX666J",
            ["disable_rcmd"] = "0",
            ["fingerprint"] = fingerprint,
            ["ipRegion"] = "CN",
            ["isDaylightTime"] = "false",
            ["isPad"] = "false",
            ["legalRegion"] = "CN",
            ["localFingerprint"] = fingerprint,
            ["local_id"] = buvid,
            ["mobi_app"] = "android",
            ["modelName"] = "NX666J",
            ["networkState"] = "2",
            ["networkstate"] = "2",
            ["osVer"] = "12",
            ["platform"] = "android",
            ["sessionID"] = "dc1352bb",
            ["statistics"] = "{\"appId\":1,\"platform\":3,\"version\":\"9.7.0\",\"abtest\":\"\"}",
            ["statusBarHeight"] = "111",
            ["taskCode"] = taskCode,
            ["timezone"] = "Asia/Shanghai",
            ["ts"] = ts,
            ["utcOffset"] = "+08:00",
        };
    }

    [Fact]
    public void CalcSign_ReceiveOgvWatchFixture_MatchesCapturedSign()
    {
        // Arrange: receive/v2 领取「观看剧集内容」抓包（ts=1787326055）
        var parameters = BuildCapturedParameters("ogvwatchnew", "1787326055");

        // Act
        var sign = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);

        // Assert
        Assert.Equal("b45229c7ef4c5ac9971c393a10e8cd99", sign);
    }

    [Fact]
    public void CalcSign_CompleteDressViewFixture_MatchesCapturedSign()
    {
        // Arrange: complete/v2 完成「浏览装扮商城」抓包（ts=1787326211）
        var parameters = BuildCapturedParameters("dress-view", "1787326211");

        // Act
        var sign = AppSignHelper.CalcSign(parameters, AppSignHelper.AppSec);

        // Assert
        Assert.Equal("b9c160235fb22fd271146977518953a0", sign);
    }

    [Fact]
    public void BuildSortedQuery_SortsKeysOrdinalAndEscapesValues()
    {
        var parameters = new Dictionary<string, string>
        {
            ["b"] = "2",
            ["a"] = "x y",
            ["sign"] = "ignored",
            ["c"] = "",
        };

        var query = AppSignHelper.BuildSortedQuery(parameters);

        Assert.Equal("a=x%20y&b=2", query);
    }
}
