using OverrealmTracker.Config;
using OverrealmTracker.Tracking;
using OverrealmTracker.Utils;

namespace OverrealmTracker.UI;

public sealed class OverlayForm : Form
{
  private readonly AppConfig _config;
  private readonly BossTracker _tracker;
  private readonly System.Windows.Forms.Timer _timer;

  private readonly Dictionary<string, Label> _bossLabels = new();
  private readonly Dictionary<string, Label> _floorLabels = new();
  private readonly Dictionary<string, Label> _itemLabels = new();
  private Label _status = null!;
  private Label _overrealmStatus = null!;
  private Label? _compactBossLine;
  private Label? _compactFloorLine;
  private Label? _compactItemLine;
  private readonly ToolTip _toolTip = new();

  private string _logPath;
  private bool _dragging;
  private Point _dragStart;

  private static readonly Color BgColor = ColorTranslator.FromHtml("#1e1e1e");
  private static readonly Color GoldColor = ColorTranslator.FromHtml("#e8b84a");
  private static readonly Color TextColor = ColorTranslator.FromHtml("#d8d8e0");
  private static readonly Color ValueColor = Color.White;
  private static readonly Color ErrorColor = ColorTranslator.FromHtml("#ff7a7a");
  private static readonly Color DividerColor = ColorTranslator.FromHtml("#3a3a4a");

  public OverlayForm(AppConfig config)
  {
    _config = config;
    _tracker = new BossTracker(config);
    _logPath = FindLog(config);

    AutoScaleMode = AutoScaleMode.Dpi;
    FormBorderStyle = FormBorderStyle.None;
    BackColor = BgColor;
    StartPosition = FormStartPosition.Manual;
    Location = new Point(config.X, config.Y);
    ShowInTaskbar = true;
    Text = "Overrealm Tracker";

    BuildUi();

    _timer = new System.Windows.Forms.Timer { Interval = Math.Max(50, config.PollMs) };
    _timer.Tick += (_, _) => Tick();
    _timer.Start();

    RefreshValues();

    MouseDown += OnMouseDown;
    MouseMove += OnMouseMove;
    MouseUp += OnMouseUp;
    ContextMenuStrip = BuildContextMenu();
  }

  private void BuildUi()
  {
    SuspendLayout();
    if (_config.Compact) BuildCompactUi();
    else BuildFullUi();
    ResumeLayout(true);
  }

  private void BuildFullUi()
  {
    var layout = NewLayout();
    AddHeader(layout, "Boss Counter");
    foreach (var kv in _config.Bosses)
      AddRow(layout, _bossLabels, kv.Key, kv.Value, noSuffix: true);

    AddDivider(layout);
    AddHeader(layout, "Floors");
    foreach (var kv in _config.FloorNames)
      AddRow(layout, _floorLabels, kv.Key, kv.Value, noSuffix: false);

    AddDivider(layout);
    AddHeader(layout, "Items");
    foreach (var kv in _config.Items)
      AddRow(layout, _itemLabels, kv.Key, kv.Value, noSuffix: false);
    foreach (var kv in _config.ConditionalItems)
      AddRow(layout, _itemLabels, kv.Key, kv.Value.Name, noSuffix: false);

    AddStatus(layout);
    Controls.Add(layout);
    FinaliseLayout(layout);
  }

  private void BuildCompactUi()
  {
    var layout = NewLayout();

    AddHeader(layout, "Bosses");
    var bossStrip = new FlowLayoutPanel
    {
      AutoSize = true,
      AutoSizeMode = AutoSizeMode.GrowAndShrink,
      FlowDirection = FlowDirection.LeftToRight,
      WrapContents = false,
      Anchor = AnchorStyles.Left,
      Margin = new Padding(0, 1, 0, 1),
      BackColor = BackColor,
    };
    foreach (var kv in _config.Bosses)
      bossStrip.Controls.Add(BuildCompactIconCounter(kv.Key, $"boss_{kv.Key}", kv.Value, _bossLabels));
    layout.Controls.Add(bossStrip);
    layout.SetColumnSpan(bossStrip, 2);

    AddDivider(layout);
    AddHeader(layout, "Floors");
    var floorLine = new Label
    {
      ForeColor = TextColor,
      Font = new Font("Segoe UI", 11, FontStyle.Bold),
      AutoSize = true,
      Margin = new Padding(0, 1, 0, 1),
    };
    layout.Controls.Add(floorLine);
    layout.SetColumnSpan(floorLine, 2);
    _compactFloorLine = floorLine;

    AddDivider(layout);
    AddHeader(layout, "Items");
    var itemStrip = new FlowLayoutPanel
    {
      AutoSize = true,
      AutoSizeMode = AutoSizeMode.GrowAndShrink,
      FlowDirection = FlowDirection.LeftToRight,
      WrapContents = false,
      Anchor = AnchorStyles.Left,
      Margin = new Padding(0, 1, 0, 1),
      BackColor = BackColor,
    };
    foreach (var kv in _config.Items)
      itemStrip.Controls.Add(BuildCompactIconCounter(kv.Key, $"item_{kv.Key}", kv.Value, _itemLabels));
    foreach (var kv in _config.ConditionalItems)
      itemStrip.Controls.Add(BuildCompactIconCounter(kv.Key, $"item_{kv.Key}", kv.Value.Name, _itemLabels));
    layout.Controls.Add(itemStrip);
    layout.SetColumnSpan(itemStrip, 2);

    AddStatus(layout);
    Controls.Add(layout);
    FinaliseLayout(layout);
  }

  private Control BuildCompactIconCounter(string id, string iconBaseName, string tooltipText, Dictionary<string, Label> store)
  {
    var container = new FlowLayoutPanel
    {
      AutoSize = true,
      AutoSizeMode = AutoSizeMode.GrowAndShrink,
      FlowDirection = FlowDirection.LeftToRight,
      WrapContents = false,
      Margin = new Padding(0, 0, 12, 0),
      BackColor = BackColor,
    };

    var icon = TryLoadIcon(iconBaseName);
    if (icon is not null)
    {
      var pic = new PictureBox
      {
        Image = icon,
        SizeMode = PictureBoxSizeMode.Zoom,
        Size = new Size(_config.IconSize, _config.IconSize),
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 0, 4, 0),
      };
      _toolTip.SetToolTip(pic, tooltipText);
      container.Controls.Add(pic);
    }
    else
    {
      container.Controls.Add(new Label
      {
        Text = tooltipText + ":",
        ForeColor = TextColor,
        Font = new Font("Segoe UI", 11),
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 0, 4, 0),
      });
    }

    var valueLbl = new Label
    {
      Text = "0",
      ForeColor = ValueColor,
      Font = new Font("Segoe UI", 12, FontStyle.Bold),
      AutoSize = true,
      Anchor = AnchorStyles.Left,
      Margin = new Padding(0, 0, 0, 0),
    };

    container.Controls.Add(valueLbl);
    store[id] = valueLbl;
    return container;
  }

  private TableLayoutPanel NewLayout()
  {
    var layout = new TableLayoutPanel
    {
      Dock = DockStyle.Fill,
      AutoSize = true,
      AutoSizeMode = AutoSizeMode.GrowAndShrink,
      ColumnCount = 2,
      Padding = new Padding(12, 8, 12, 8),
      BackColor = BgColor,
    };
    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
    layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    return layout;
  }

  private static void AddHeader(TableLayoutPanel layout, string text)
  {
    var lbl = new Label
    {
      Text = text,
      ForeColor = GoldColor,
      Font = new Font("Segoe UI", 10, FontStyle.Bold),
      AutoSize = true,
      Margin = new Padding(0, 4, 0, 2),
    };
    layout.Controls.Add(lbl);
    layout.SetColumnSpan(lbl, 2);
  }

  private static void AddDivider(TableLayoutPanel layout)
  {
    var div = new Panel
    {
      Height = 1,
      Dock = DockStyle.Fill,
      BackColor = DividerColor,
      Margin = new Padding(0, 8, 0, 4),
    };
    layout.Controls.Add(div);
    layout.SetColumnSpan(div, 2);
  }

  private void AddRow(
    TableLayoutPanel layout,
    Dictionary<string, Label> store,
    string id,
    string displayName,
    bool noSuffix,
    string? iconBaseName = null)
  {
    var icon = iconBaseName is null ? null : TryLoadIcon(iconBaseName);

    var nameCell = new TableLayoutPanel
    {
      AutoSize = true,
      AutoSizeMode = AutoSizeMode.GrowAndShrink,
      ColumnCount = 2,
      RowCount = 1,
      Anchor = AnchorStyles.Left,
      Margin = new Padding(0, 2, 0, 2),
      BackColor = BackColor,
    };
    nameCell.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    nameCell.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

    if (icon is not null)
    {
      nameCell.Controls.Add(new PictureBox
      {
        Image = icon,
        SizeMode = PictureBoxSizeMode.Zoom,
        Size = new Size(_config.IconSize, _config.IconSize),
        Anchor = AnchorStyles.None,
        Margin = new Padding(0, 0, 8, 0),
      }, 0, 0);
    }

    nameCell.Controls.Add(new Label
    {
      Text = noSuffix ? displayName : displayName + ":",
      ForeColor = TextColor,
      Font = new Font("Segoe UI", 11),
      AutoSize = true,
      Anchor = AnchorStyles.Left,
      Margin = new Padding(0, 0, 0, 0),
    }, 1, 0);

    var valueLbl = new Label
    {
      Text = "0",
      ForeColor = ValueColor,
      Font = new Font("Segoe UI", 12, FontStyle.Bold),
      AutoSize = false,
      Width = 48,
      TextAlign = ContentAlignment.MiddleRight,
      Anchor = AnchorStyles.Right,
      Margin = new Padding(0, 4, 0, 0),
    };

    layout.Controls.Add(nameCell);
    layout.Controls.Add(valueLbl);
    store[id] = valueLbl;
  }

  private void AddStatus(TableLayoutPanel layout)
  {
    _overrealmStatus = new Label
    {
      Text = "Out of Overrealm",
      ForeColor = TextColor,
      Font = new Font("Segoe UI", 9, FontStyle.Italic),
      AutoSize = true,
      Margin = new Padding(0, 4, 0, 0),
    };
    layout.Controls.Add(_overrealmStatus);
    layout.SetColumnSpan(_overrealmStatus, 2);

    _status = new Label
    {
      Text = "",
      ForeColor = ErrorColor,
      Font = new Font("Segoe UI", 8),
      AutoSize = true,
      Margin = new Padding(0, 2, 0, 0),
    };
    layout.Controls.Add(_status);
    layout.SetColumnSpan(_status, 2);
  }

  private void FinaliseLayout(TableLayoutPanel layout)
  {
    AutoSize = true;
    AutoSizeMode = AutoSizeMode.GrowAndShrink;
    foreach (Control c in layout.Controls) HookDrag(c);
    HookDrag(layout);
  }

  private void HookDrag(Control c)
  {
    c.MouseDown += OnMouseDown;
    c.MouseMove += OnMouseMove;
    c.MouseUp += OnMouseUp;
  }

  private void OnMouseDown(object? s, MouseEventArgs e)
  {
    if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; }
  }

  private void OnMouseMove(object? s, MouseEventArgs e)
  {
    if (!_dragging) return;
    if (s is Control c && c != this)
    {
      var screenPt = c.PointToScreen(e.Location);
      Location = new Point(screenPt.X - _dragStart.X, screenPt.Y - _dragStart.Y);
    }
    else
    {
      Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y);
    }
  }

  private void OnMouseUp(object? s, MouseEventArgs e)
  {
    if (e.Button != MouseButtons.Left) return;
    _dragging = false;
    _config.X = Location.X;
    _config.Y = Location.Y;
    ConfigManager.Save(_config);
  }

  private ContextMenuStrip BuildContextMenu()
  {
    var menu = new ContextMenuStrip();

    menu.Items.Add("Choose log file...", null, (_, _) => PickLog());


    var compactItem = new ToolStripMenuItem("Compact mode") { CheckOnClick = true, Checked = _config.Compact };
    compactItem.CheckedChanged += (_, _) =>
    {
      _config.Compact = compactItem.Checked;
      ConfigManager.Save(_config);
      RebuildUi();
    };
    menu.Items.Add(compactItem);

    menu.Items.Add(new ToolStripSeparator());
    //menu.Items.Add("Reset all counts", null, (_, _) => { _tracker.Reset(); RefreshValues(); });
    menu.Items.Add("Quit", null, (_, _) => Close());
    return menu;
  }

  private void PickLog()
  {
    using var dlg = new OpenFileDialog
    {
      Title = "Select Torchlight Infinite log file (UE_game.log)",
      Filter = "Log files (*.log)|*.log|All files (*.*)|*.*",
    };
    if (dlg.ShowDialog(this) == DialogResult.OK)
    {
      _config.LogPath = dlg.FileName;
      ConfigManager.Save(_config);
      _logPath = dlg.FileName;
      _tracker.ResetPositionAndFloorState();
      RefreshValues();
    }
  }

  private void RefreshValues()
  {
    if (_config.Compact) RefreshCompact();
    else RefreshFull();

    _overrealmStatus!.Text = _tracker.IsInOverrealm ? "In Overrealm" : "Out of Overrealm";
    _overrealmStatus!.ForeColor = _tracker.IsInOverrealm ? GoldColor : TextColor;
    _status!.Text = string.IsNullOrEmpty(_logPath) ? "Log not found - right-click to set" : "";
  }

  private void RefreshFull()
  {
    foreach (var kv in _bossLabels)
      kv.Value.Text = _tracker.Counts.GetValueOrDefault(kv.Key).ToString();
    foreach (var kv in _floorLabels)
      kv.Value.Text = _tracker.FloorCounts.GetValueOrDefault(kv.Key).ToString();
    foreach (var kv in _itemLabels)
      kv.Value.Text = _tracker.ItemCounts.GetValueOrDefault(kv.Key).ToString();
  }

  private void RefreshCompact()
  {
    foreach (var kv in _bossLabels)
      kv.Value.Text = _tracker.Counts.GetValueOrDefault(kv.Key).ToString();

    foreach (var kv in _itemLabels)
      kv.Value.Text = _tracker.ItemCounts.GetValueOrDefault(kv.Key).ToString();

    if (_compactFloorLine is not null)
    {
      var parts = _config.FloorNames.Keys.Select(k =>
          _tracker.FloorCounts.GetValueOrDefault(k).ToString());
      _compactFloorLine.Text = string.Join(" / ", parts);
    }
  }

  private void RebuildUi()
  {
    SuspendLayout();
    Controls.Clear();
    _bossLabels.Clear();
    _floorLabels.Clear();
    _itemLabels.Clear();
    _compactBossLine = null;
    _compactItemLine = null;
    _compactFloorLine = null;

    BuildUi();
    ResumeLayout(true);
    PerformLayout();
    RefreshValues();
  }

  private static string StripSuffix(string s, char suffix)
  {
    var t = s.TrimEnd();
    return t.EndsWith(suffix) ? t[..^1].TrimEnd() : t;
  }

  private void Tick()
  {
    if (string.IsNullOrEmpty(_logPath)) _logPath = FindLog(_config);
    if (!string.IsNullOrEmpty(_logPath) && _tracker.Poll(_logPath)) RefreshValues();
  }

  private static string FindLog(AppConfig config)
  {
    var p = config.LogPath;
    return !string.IsNullOrEmpty(p) && File.Exists(p) ? p : "";
  }

  private Image? TryLoadIcon(string iconBaseName)
  {
    if (string.IsNullOrEmpty(iconBaseName)) return null;

    // Build from assembly by default
    var assembly = typeof(OverlayForm).Assembly;
    var resourceName = $"OverrealmTracker.icons.{iconBaseName}.png";

    var stream = assembly.GetManifestResourceStream(resourceName);
    if (stream is not null)
    {
      using (stream)
      {
        using var src = Image.FromStream(stream);
        return new Bitmap(src, _config.IconSize, _config.IconSize);
      }
    }

    // Build from local as a fallback
    if (!string.IsNullOrEmpty(_config.IconsDir))
    {
      var path = Path.Combine(ConfigManager.AppDir, _config.IconsDir, iconBaseName + ".png");
      if (File.Exists(path))
      {
        try
        {
          using var src = Image.FromFile(path);
          return new Bitmap(src, _config.IconSize, _config.IconSize);
        }
        catch { return null; }
      }
    }

    return null;
  }
}
