using System.Text.Json.Serialization;
using OverrealmTracker.Utils;

namespace OverrealmTracker.Tracking;

public sealed class TrackerState
{
    [JsonPropertyName("counts")]
    public Dictionary<string, int> Counts { get; set; } = new();

    [JsonPropertyName("last_ts")]
    public string LastTs { get; set; } = "";

    [JsonPropertyName("floor_counts")]
    public Dictionary<string, int> FloorCounts { get; set; } = new();

    [JsonPropertyName("last_floor_ts")]
    public string LastFloorTs { get; set; } = "";

    [JsonPropertyName("floor_logic")]
    public int FloorLogic { get; set; } = 3;

    [JsonPropertyName("item_counts")]
    public Dictionary<string, int> ItemCounts { get; set; } = new();

    [JsonPropertyName("last_item_ts")]
    public string LastItemTs { get; set; } = "";

    [JsonPropertyName("in_overrealm")]
    public bool InOverrealm { get; set; }

    [JsonPropertyName("last_overrealm_leave_ts")]
    public string LastOverrealmLeaveTs { get; set; } = "";

    public static TrackerState Load(string path)
    {
        var s = JsonHelper.Load<TrackerState>(path) ?? new TrackerState();
        if (s.FloorLogic != 3)
        {
            s.FloorCounts.Clear();
            s.LastFloorTs = "";
            s.FloorLogic = 3;
        }
        return s;
    }

    public void Save(string path) => JsonHelper.Save(path, this);
}
