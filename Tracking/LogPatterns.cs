using System.Globalization;
using System.Text.RegularExpressions;

namespace OverrealmTracker.Tracking;

public static class LogPatterns
{
    public const double PortalToLevelSeconds = 10.0;

    public static readonly Regex Monster = new(
        @"^\[(\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]\[\s*\d+\].*Monster Created: npc id (\d+) rarity (\d+)",
        RegexOptions.Compiled);

    public static readonly Regex Portal = new(
        @"^\[(\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]\[\s*\d+\].*Create Map Portal cfgId (\d+)(?: etyId (\d+))?",
        RegexOptions.Compiled);

    public static readonly Regex Bag = new(
        @"^\[(\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]\[\s*\d+\].*BagMgr@:Modfy BagItem PageId = \d+ SlotId = \d+ ConfigBaseId = (\d+) Num = (\d+)",
        RegexOptions.Compiled);

    public static readonly Regex Active = new(
        @"^\[(\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]\[\s*\d+\].*Begin active Map Portal cfgId (\d+)",
        RegexOptions.Compiled);

    public static readonly Regex Leave = new(
        @"^\[(\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]\[\s*\d+\].*LevelMgr RecvPlayerLeaveLevel Type=(\d+)",
        RegexOptions.Compiled);

    // LevelLinkData： <LevelUid> <LevelType> <LevelId>
    // Tolerates both ASCII ':' and full-width '：' (U+FF1A).
    public static readonly Regex LevelLinkData = new(
        @"^\[(\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]\[\s*\d+\].*LevelLinkData\s*[:：]\s*(\d+)\s+(\d+)\s+(\d+)",
        RegexOptions.Compiled);

    public static double? TsSeconds(string ts)
    {
        if (ts.Length < 23) return null;
        var core = ts[..19];
        var msStr = ts.Substring(20, 3);
        try
        {
            var dt = DateTime.ParseExact(core, "yyyy.MM.dd-HH.mm.ss",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            var ms = int.Parse(msStr, CultureInfo.InvariantCulture);
            return new DateTimeOffset(dt).ToUnixTimeSeconds() + ms / 1000.0;
        }
        catch { return null; }
    }
}
