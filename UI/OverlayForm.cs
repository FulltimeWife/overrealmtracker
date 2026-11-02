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

  private string _logPath;
  private bool _dragging;
  private Point _dragStart;

  private static readonly Color BgColor = ColorTranslator.FromHtml("#14141c");
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
    ShowInTaskbar = false;
    Text = "Overrealm Tracker";

    BuildUi();
    ApplyMode();

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

    void AddHeader(string text)
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

    void AddDivider()
    {
      var div = new Panel { Height = 1, Dock = DockStyle.Fill, BackColor = DividerColor, Margin = new Padding(0, 8, 0, 4) };
      layout.Controls.Add(div);
      layout.SetColumnSpan(div, 2);
    }

    void AddRow(string name, Dictionary<string, Label> store, string id, string suffix = "")
    {
      var nameLbl = new Label
      {
        Text = name + suffix,
        ForeColor = TextColor,
        Font = new Font("Segoe UI", 11),
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 1, 0, 1),
      };
      var valueLbl = new Label
      {
        Text = "0",
        ForeColor = ValueColor,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        AutoSize = false,
        Width = 48,
        TextAlign = ContentAlignment.MiddleRight,
        Anchor = AnchorStyles.Right,
        Margin = new Padding(0, 1, 0, 1),
      };
      layout.Controls.Add(nameLbl);
      layout.Controls.Add(valueLbl);
      store[id] = valueLbl;
    }

    AddHeader("Overrealm Tracker");
    AddHeader("Boss Counter");
    foreach (var kv in _config.Bosses)
      AddRow(kv.Value, _bossLabels, kv.Key);

    AddDivider();
    AddHeader("Floors");
    foreach (var kv in _config.FloorNames)
      AddRow(kv.Value, _floorLabels, kv.Key, ":");

    AddDivider();
    AddHeader("Items");
    foreach (var kv in _config.Items)
      AddRow(kv.Value, _itemLabels, kv.Key, ":");

    // Conditional items (e.g. Solid River, base 3811) shown alongside normal items.
    foreach (var kv in _config.ConditionalItems)
      AddRow(kv.Value.Name, _itemLabels, kv.Key, ":");

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

    Controls.Add(layout);
    AutoSize = true;
    AutoSizeMode = AutoSizeMode.GrowAndShrink;

    foreach (Control c in layout.Controls) HookDrag(c);
    HookDrag(layout);

    ResumeLayout(true);
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
    menu.Items.Add("Reset all counts", null, (_, _) => { _tracker.Reset(); RefreshValues(); });
    menu.Items.Add("Choose log file...", null, (_, _) => PickLog());

    var obsItem = new ToolStripMenuItem("OBS capture mode") { CheckOnClick = true, Checked = _config.ObsMode };
    obsItem.CheckedChanged += (_, _) =>
    {
      _config.ObsMode = obsItem.Checked;
      ConfigManager.Save(_config);
      ApplyMode();
    };
    menu.Items.Add(obsItem);

    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add("Quit", null, (_, _) => Close());
    return menu;
  }

  private void ApplyMode()
  {
    var obs = _config.ObsMode;

    TopMost = true;
    ShowInTaskbar = obs;
    Opacity = obs ? 1.0 : Math.Clamp(_config.Opacity, 0.1, 1.0);

    if (!IsHandleCreated) return;
    Win32.SetAppWindow(Handle, appWindow: obs);
    Win32.SetLayered(Handle, layered: !obs);
    TopMost = true;
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
    foreach (var kv in _bossLabels)
      kv.Value.Text = _tracker.Counts.GetValueOrDefault(kv.Key).ToString();
    foreach (var kv in _floorLabels)
      kv.Value.Text = _tracker.FloorCounts.GetValueOrDefault(kv.Key).ToString();
    foreach (var kv in _itemLabels)
      kv.Value.Text = _tracker.ItemCounts.GetValueOrDefault(kv.Key).ToString();

    _overrealmStatus!.Text = _tracker.IsInOverrealm ? "In Overrealm" : "Out of Overrealm";
    _overrealmStatus!.ForeColor = _tracker.IsInOverrealm ? GoldColor : TextColor;
    _status!.Text = string.IsNullOrEmpty(_logPath) ? "Log not found - right-click to set" : "";
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
}
