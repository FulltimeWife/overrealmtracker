using OverrealmTracker.Utils;

namespace OverrealmTracker.Config;

public static class ConfigManager
{
    public static string AppDir { get; } =
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

    public static string ConfigPath { get; } = Path.Combine(AppDir, "config.json");
    public static string StatePath { get; } = Path.Combine(AppDir, "tracker_data.json");

    public static AppConfig Load()
    {
        var defaults = new AppConfig();
        var saved = JsonHelper.Load<AppConfig>(ConfigPath) ?? new AppConfig();

        foreach (var kv in saved.Bosses) defaults.Bosses[kv.Key] = kv.Value;
        foreach (var kv in saved.FloorNames) defaults.FloorNames[kv.Key] = kv.Value;
        foreach (var kv in saved.Items) defaults.Items[kv.Key] = kv.Value;
        foreach (var kv in saved.ConditionalItems) defaults.ConditionalItems[kv.Key] = kv.Value;

        if (!string.IsNullOrEmpty(saved.LogPath)) defaults.LogPath = saved.LogPath;
        if (!string.IsNullOrEmpty(saved.EntryPortalCfg)) defaults.EntryPortalCfg = saved.EntryPortalCfg;
        if (!string.IsNullOrEmpty(saved.NextFloorPortalCfg)) defaults.NextFloorPortalCfg = saved.NextFloorPortalCfg;
        if (!string.IsNullOrEmpty(saved.ObsBackdropColor)) defaults.ObsBackdropColor = saved.ObsBackdropColor;
        if (!string.IsNullOrEmpty(saved.IconsDir)) defaults.IconsDir = saved.IconsDir;
        if (saved.HubLevelType != 0) defaults.HubLevelType = saved.HubLevelType;
        if (saved.BossRarity != 0) defaults.BossRarity = saved.BossRarity;
        if (saved.PollMs > 0) defaults.PollMs = saved.PollMs;
        if (saved.Opacity > 0) defaults.Opacity = saved.Opacity;
        if (saved.IconSize > 0) defaults.IconSize = saved.IconSize;
        defaults.ObsMode = saved.ObsMode;
        defaults.Compact = saved.Compact;
        if (saved.X != 0 || saved.Y != 0) { defaults.X = saved.X; defaults.Y = saved.Y; }
        if (saved.OverrealmLevelTypes.Count > 0) defaults.OverrealmLevelTypes = saved.OverrealmLevelTypes;

        Save(defaults);
        return defaults;
    }

    public static void Save(AppConfig config) => JsonHelper.Save(ConfigPath, config);
}
