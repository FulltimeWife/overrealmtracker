using OverrealmTracker.Config;

namespace OverrealmTracker.Tracking;

public sealed class BossTracker
{
    private readonly AppConfig _config;
    private readonly TrackerState _state;
    private long _pos;

    // Floor-tracking scratch
    private bool _inPick;
    private readonly Dictionary<string, string> _portalEty = new();
    private string? _enteredEty;
    private (string cfgId, string activeTs)? _pending;
    private int _depth;

    // Overrealm state
    private bool _inOverrealm;
    private string _lastOverrealmLeaveTs = "";
    private bool _overrealmRestored;

    // AttributeMgr 
    private static readonly HashSet<string> AttributeCheckedItems = new() { "3811" };
    private const double AttributeWindowSeconds = 1.0;
    private (string itemId, string ts)? _pendingLegendary;

    public IReadOnlyDictionary<string, int> Counts => _state.Counts;
    public IReadOnlyDictionary<string, int> FloorCounts => _state.FloorCounts;
    public IReadOnlyDictionary<string, int> ItemCounts => _state.ItemCounts;
    public bool IsInOverrealm => _inOverrealm;

    public BossTracker(AppConfig config)
    {
        _config = config;
        _state = TrackerState.Load(ConfigManager.StatePath);
        ResetFloorTracking();
    }

    private void ResetFloorTracking()
    {
        _inPick = false;
        _portalEty.Clear();
        _enteredEty = null;
        _pending = null;
        _depth = 0;
    }

    public void ResetPositionAndFloorState()
    {
        _pos = 0;
        ResetFloorTracking();
    }

    public void Save() => _state.Save(ConfigManager.StatePath);

    public void Reset()
    {
        _state.Counts.Clear();
        _state.FloorCounts.Clear();
        _state.ItemCounts.Clear();
        Save();
    }

    public bool Poll(string logPath)
    {
        var flushed = FlushStalePending();
        if (flushed) Save();

        long size;
        try { size = new FileInfo(logPath).Length; }
        catch { return flushed; }

        if (size < _pos)
        {
            _pos = 0;
            ResetFloorTracking();
            _pendingLegendary = null;
        }
        if (size == _pos) return flushed;

        byte[] data;
        using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete))
        {
            fs.Seek(_pos, SeekOrigin.Begin);
            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            data = ms.ToArray();
        }

        var lastNl = Array.LastIndexOf(data, (byte)'\n');
        if (lastNl < 0) return flushed;

        var consumed = data.AsSpan(0, lastNl + 1);
        _pos += consumed.Length;
        var text = System.Text.Encoding.UTF8.GetString(consumed);

        var bossIds = _config.Bosses;
        var rarityNeeded = _config.BossRarity;
        var floorNames = _config.FloorNames;
        var entryCfg = _config.EntryPortalCfg;
        var nextCfg = _config.NextFloorPortalCfg;
        var hubType = _config.HubLevelType;
        var itemIds = _config.Items;
        var overrealmLevelTypes = new HashSet<int>(_config.OverrealmLevelTypes);
        var condItems = _config.ConditionalItems;

        var bossBase = _state.LastTs;
        var floorBase = _state.LastFloorTs;
        var itemBase = _state.LastItemTs;
        var bossNewest = bossBase;
        var floorNewest = floorBase;
        var itemNewest = itemBase;
        var changed = false;

        if (!_overrealmRestored)
        {
            _inOverrealm = _state.InOverrealm;
            _lastOverrealmLeaveTs = _state.LastOverrealmLeaveTs;
            _overrealmRestored = true;
        }

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0) continue;

            if (_pendingLegendary is not null)
            {
                var (pendId, pendTs) = _pendingLegendary.Value;

                if (line.Contains("AttributeMgr"))
                {
                    var am = LogPatterns.AttributeBase.Match(line);
                    if (am.Success && am.Groups[1].Value == pendId)
                    {
                        _pendingLegendary = null;
                        continue;
                    }
                }
                else if (line.Length > 24 && line[0] == '[')
                {
                    var lineSec = LogPatterns.TsSeconds(line.Substring(1, 23));
                    var pendSec = LogPatterns.TsSeconds(pendTs);
                    if (lineSec is not null && pendSec is not null && lineSec.Value - pendSec.Value > AttributeWindowSeconds)
                    {
                        CommitPending();
                        changed = true;
                    }
                }
            }

            // ---- Overrealm detection via LevelLinkData ----
            var llm = LogPatterns.LevelLinkData.Match(line);
            if (llm.Success)
            {
                var ts = llm.Groups[1].Value;
                var levelType = int.Parse(llm.Groups[3].Value);
                var wasOverrealm = _inOverrealm;

                if (overrealmLevelTypes.Contains(levelType))
                {
                    _inOverrealm = true;
                    _lastOverrealmLeaveTs = "";
                }
                else if (_inOverrealm)
                {
                    _lastOverrealmLeaveTs = ts;
                    _inOverrealm = false;
                }

                if (wasOverrealm != _inOverrealm)
                {
                    _state.InOverrealm = _inOverrealm;
                    _state.LastOverrealmLeaveTs = _lastOverrealmLeaveTs;
                    changed = true;
                }
                continue;
            }

            if (line.Contains("Monster Created"))
            {
                var m = LogPatterns.Monster.Match(line);
                if (!m.Success) continue;
                var ts = m.Groups[1].Value;
                var npcId = m.Groups[2].Value;
                var rarity = int.Parse(m.Groups[3].Value);
                if (!bossIds.ContainsKey(npcId) || rarity != rarityNeeded) continue;
                if (string.CompareOrdinal(ts, bossBase) <= 0) continue;
                _state.Counts[npcId] = _state.Counts.GetValueOrDefault(npcId) + 1;
                changed = true;
                if (string.CompareOrdinal(ts, bossNewest) > 0) bossNewest = ts;
            }
            else if (line.Contains("ProtoName=PickItem"))
            {
                _inPick = line.TrimEnd().EndsWith("start", StringComparison.Ordinal);
            }
            else if (_inPick && line.Contains("BagMgr@:Modfy BagItem"))
            {
                var m = LogPatterns.Bag.Match(line);
                if (!m.Success) continue;
                var ts = m.Groups[1].Value;
                var itemId = m.Groups[2].Value;

                if (string.CompareOrdinal(ts, itemBase) <= 0) continue;

                if (condItems.TryGetValue(itemId, out var cond))
                {
                    if (cond.RequireOverrealm && !IsWithinOverrealmWindow(cond.GraceSeconds))
                    {
                        if (string.CompareOrdinal(ts, itemNewest) > 0) itemNewest = ts;
                        continue;
                    }

                    if (AttributeCheckedItems.Contains(itemId))
                    {
                        if (_pendingLegendary is not null)
                        {
                            CommitPending();
                            changed = true;
                        }
                        _pendingLegendary = (itemId, ts);
                        if (string.CompareOrdinal(ts, itemNewest) > 0) itemNewest = ts;
                        continue;
                    }

                    _state.ItemCounts[itemId] = _state.ItemCounts.GetValueOrDefault(itemId) + 1;
                    _state.LastItemPickupTs[itemId] = ts;
                    changed = true;
                    if (string.CompareOrdinal(ts, itemNewest) > 0) itemNewest = ts;
                    continue;
                }

                if (!itemIds.ContainsKey(itemId)) continue;
                _state.ItemCounts[itemId] = _state.ItemCounts.GetValueOrDefault(itemId) + 1;
                changed = true;
                if (string.CompareOrdinal(ts, itemNewest) > 0) itemNewest = ts;
            }
            else if (line.Contains("Create Map Portal"))
            {
                var m = LogPatterns.Portal.Match(line);
                if (m.Success && m.Groups[3].Success)
                    _portalEty[m.Groups[2].Value] = m.Groups[3].Value;
            }
            else if (line.Contains("Begin active Map Portal"))
            {
                var m = LogPatterns.Active.Match(line);
                if (m.Success) _pending = (m.Groups[2].Value, m.Groups[1].Value);
            }
            else if (line.Contains("RecvPlayerLeaveLevel"))
            {
                var m = LogPatterns.Leave.Match(line);
                if (!m.Success) continue;
                var ts = m.Groups[1].Value;
                var levelType = int.Parse(m.Groups[2].Value);

                // Refresh grace window on any Overrealm-type leave (covers floor transitions).
                if (overrealmLevelTypes.Contains(levelType))
                {
                    _lastOverrealmLeaveTs = ts;
                    _state.LastOverrealmLeaveTs = _lastOverrealmLeaveTs;
                    changed = true;
                }

                if (levelType == hubType)
                {
                    _depth = 0;
                    _pending = null;
                    continue;
                }
                if (_pending is null) continue;

                var (cfgId, activeTs) = _pending.Value;
                _pending = null;
                var tsSec = LogPatterns.TsSeconds(ts);
                var activeSec = LogPatterns.TsSeconds(activeTs);
                if (tsSec is null || activeSec is null) continue;
                if (tsSec.Value - activeSec.Value > LogPatterns.PortalToLevelSeconds) continue;

                if (cfgId == entryCfg)
                {
                    if (_portalEty.TryGetValue(cfgId, out var ety))
                    {
                        var isRepeat = _enteredEty == ety;
                        _enteredEty = ety;
                        if (isRepeat) { _depth = 0; continue; }
                    }
                    _depth = 1;
                }
                else if (cfgId == nextCfg && _depth >= 1)
                {
                    _depth += 1;
                }
                else { continue; }

                var floorKey = _depth.ToString();
                if (!floorNames.ContainsKey(floorKey)) continue;
                if (string.CompareOrdinal(ts, floorBase) <= 0) continue;
                _state.FloorCounts[floorKey] = _state.FloorCounts.GetValueOrDefault(floorKey) + 1;
                changed = true;
                if (string.CompareOrdinal(ts, floorNewest) > 0) floorNewest = ts;
            }
        }

        if (changed)
        {
            _state.LastTs = bossNewest;
            _state.LastFloorTs = floorNewest;
            _state.LastItemTs = itemNewest;
            Save();
        }
        return changed || flushed;
    }

    private bool IsWithinOverrealmWindow(double graceSeconds)
    {
        if (_inOverrealm) return true;
        if (string.IsNullOrEmpty(_lastOverrealmLeaveTs)) return false;
        var leftSec = LogPatterns.TsSeconds(_lastOverrealmLeaveTs);
        if (leftSec is null) return false;
        var nowSec = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        return nowSec - leftSec.Value <= graceSeconds;
    }

    private void CommitPending()
    {
        if (_pendingLegendary is null) return;
        var (itemId, ts) = _pendingLegendary.Value;
        _pendingLegendary = null;
        _state.ItemCounts[itemId] = _state.ItemCounts.GetValueOrDefault(itemId) + 1;
        _state.LastItemPickupTs[itemId] = ts;
        if (string.CompareOrdinal(ts, _state.LastItemTs) > 0) _state.LastItemTs = ts;
    }

    private bool FlushStalePending()
    {
        if (_pendingLegendary is null) return false;
        var sec = LogPatterns.TsSeconds(_pendingLegendary.Value.ts);
        if (sec is null) { _pendingLegendary = null; return false; }
        var now = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
        if (now - sec.Value <= AttributeWindowSeconds) return false;
        CommitPending();
        return true;
    }
}
