using System.Text.Json.Serialization;

namespace OverrealmTracker.Config;

public sealed class ConditionalItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("require_overrealm")]
    public bool RequireOverrealm { get; set; } = true;

    [JsonPropertyName("grace_seconds")]
    public double GraceSeconds { get; set; } = 10.0;
}

public sealed class AppConfig
{
    [JsonPropertyName("log_path")]
    public string LogPath { get; set; } = @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Torchlight Infinite\\UE_game\\TorchLight\\Saved\\Logs\\UE_game.log";

    [JsonPropertyName("bosses")]
    public Dictionary<string, string> Bosses { get; set; } = new()
    {
        ["2984001"] = "Doorkeeper's:",
        ["2984002"] = "Doorhunter's:",
    };

    [JsonPropertyName("floor_names")]
    public Dictionary<string, string> FloorNames { get; set; } = new()
    {
        ["1"] = "Floor 1",
        ["2"] = "Floor 2",
        ["3"] = "Floor 3",
        ["4"] = "Floor 4",
    };

    [JsonPropertyName("entry_portal_cfg")]
    public string EntryPortalCfg { get; set; } = "51";

    [JsonPropertyName("next_floor_portal_cfg")]
    public string NextFloorPortalCfg { get; set; } = "50";

    [JsonPropertyName("hub_level_type")]
    public int HubLevelType { get; set; } = 3;

    [JsonPropertyName("items")]
    public Dictionary<string, string> Items { get; set; } = new()
    {
        ["360701"] = "Inverse Image",
    };

    [JsonPropertyName("conditional_items")]
    public Dictionary<string, ConditionalItem> ConditionalItems { get; set; } = new()
    {
        ["3811"] = new ConditionalItem
        {
            Name = "Solid River",
            RequireOverrealm = true,
            GraceSeconds = 10.0,
        },
    };

    [JsonPropertyName("overrealm_level_types")]
    public List<int> OverrealmLevelTypes { get; set; } = new() { 25 };

    [JsonPropertyName("boss_rarity")]
    public int BossRarity { get; set; } = 4;

    [JsonPropertyName("poll_ms")]
    public int PollMs { get; set; } = 500;

    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 0.85;

    [JsonPropertyName("obs_mode")]
    public bool ObsMode { get; set; }

    [JsonPropertyName("obs_backdrop_color")]
    public string ObsBackdropColor { get; set; } = "#1e1e1e";

    [JsonPropertyName("x")]
    public int X { get; set; } = 60;

    [JsonPropertyName("y")]
    public int Y { get; set; } = 60;
}
