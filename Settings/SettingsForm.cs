using WorldMapWallpaper.Shared;

namespace WorldMapWallpaper.Settings;

/// <summary>
/// The main settings form for World Map Wallpaper.
/// Provides a modern-looking interface for configuring wallpaper options.
/// </summary>
public partial class SettingsForm : Form
{
    private WallpaperMonitor? _wallpaperMonitor;

    private CheckBox _issCheckBox = null!;
    private CheckBox _timeZonesCheckBox = null!;
    private CheckBox _politicalMapCheckBox = null!;
    private ComboBox _updateIntervalCombo = null!;
    private Button _previewButton = null!;
    private Button _resetButton = null!;
    private Button _closeButton = null!;

    public SettingsForm()
    {
        InitializeComponent();
        InitializeControls();
        LoadSettings();
        StartWallpaperMonitoring();
    }

    private void InitializeComponent()
    {
        this.Text = "World Map Wallpaper Settings";
        this.Size = new Size(480, 520);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowIcon = true;
        this.ShowInTaskbar = true;

        // Set modern appearance
        this.Font = new Font("Segoe UI", 9F);
        this.BackColor = SystemColors.Window;
    }

    private void InitializeControls()
    {
        var padding = 20;
        var currentY = padding;

        // Header
        var headerLabel = new Label
        {
            Text = "World Map Wallpaper Settings",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 32),
            ForeColor = Color.FromArgb(0, 120, 215) // Windows blue
        };
        this.Controls.Add(headerLabel);
        currentY += 50;

        var subtitleLabel = new Label
        {
            Text = "Configure your dynamic wallpaper preferences",
            Font = new Font("Segoe UI", 9F),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 20),
            ForeColor = SystemColors.GrayText
        };
        this.Controls.Add(subtitleLabel);
        currentY += 40;

        // Visual Elements Group
        var visualGroup = new GroupBox
        {
            Text = "Visual Elements",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 120)
        };
        this.Controls.Add(visualGroup);

        _issCheckBox = new CheckBox
        {
            Text = "Show International Space Station position and orbit",
            Location = new Point(15, 25),
            Size = new Size(visualGroup.Width - 30, 23),
            Checked = true,
            Font = new Font("Segoe UI", 9F)
        };
        _issCheckBox.CheckedChanged += OnSettingChanged;
        visualGroup.Controls.Add(_issCheckBox);

        _timeZonesCheckBox = new CheckBox
        {
            Text = "Show time zone clocks around the world",
            Location = new Point(15, 50),
            Size = new Size(visualGroup.Width - 30, 23),
            Checked = true,
            Font = new Font("Segoe UI", 9F)
        };
        _timeZonesCheckBox.CheckedChanged += OnSettingChanged;
        visualGroup.Controls.Add(_timeZonesCheckBox);

        _politicalMapCheckBox = new CheckBox
        {
            Text = "Show political boundaries and country borders",
            Location = new Point(15, 75),
            Size = new Size(visualGroup.Width - 30, 23),
            Checked = true,
            Font = new Font("Segoe UI", 9F)
        };
        _politicalMapCheckBox.CheckedChanged += OnSettingChanged;
        visualGroup.Controls.Add(_politicalMapCheckBox);

        currentY += 140;

        // Update Settings Group
        var updateGroup = new GroupBox
        {
            Text = "Update Settings",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 100)
        };
        this.Controls.Add(updateGroup);

        var intervalLabel = new Label
        {
            Text = "Update Frequency:",
            Location = new Point(15, 30),
            Size = new Size(120, 23),
            Font = new Font("Segoe UI", 9F)
        };
        updateGroup.Controls.Add(intervalLabel);

        _updateIntervalCombo = new ComboBox
        {
            Location = new Point(140, 27),
            Size = new Size(updateGroup.Width - 155, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F)
        };

        // Populate combo box
        foreach (UpdateInterval interval in Enum.GetValues<UpdateInterval>())
        {
            _updateIntervalCombo.Items.Add(new ComboBoxItem(interval.ToDisplayString(), interval));
        }
        _updateIntervalCombo.SelectedIndexChanged += OnUpdateIntervalChanged;
        updateGroup.Controls.Add(_updateIntervalCombo);

        var infoLabel = new Label
        {
            Text = "More frequent updates ensure accurate day/night cycles.",
            Location = new Point(15, 60),
            Size = new Size(updateGroup.Width - 30, 15),
            Font = new Font("Segoe UI", 8F),
            ForeColor = SystemColors.GrayText
        };
        updateGroup.Controls.Add(infoLabel);

        // Add task status label
        var taskStatusLabel = new Label
        {
            Text = GetTaskStatusText(),
            Location = new Point(15, 75),
            Size = new Size(updateGroup.Width - 30, 15),
            Font = new Font("Segoe UI", 8F),
            ForeColor = TaskManager.IsTaskEnabled() ? Color.Green : Color.Red
        };
        updateGroup.Controls.Add(taskStatusLabel);

        currentY += 120;

        // Preview Button
        _previewButton = new Button
        {
            Text = "Update Wallpaper Now",
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 35),
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _previewButton.FlatAppearance.BorderSize = 0;
        _previewButton.Click += OnPreviewClick;
        this.Controls.Add(_previewButton);
        currentY += 50;

        // Bottom buttons
        _resetButton = new Button
        {
            Text = "Reset to Defaults",
            Location = new Point(padding, currentY),
            Size = new Size(150, 30),
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        _resetButton.Click += OnResetClick;
        this.Controls.Add(_resetButton);

        _closeButton = new Button
        {
            Text = "Close",
            Location = new Point(this.ClientSize.Width - padding - 80, currentY),
            Size = new Size(80, 30),
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        _closeButton.Click += (s, e) => this.Close();
        this.Controls.Add(_closeButton);
    }

    private void LoadSettings()
    {
        _issCheckBox.Checked = Shared.Settings.ShowISS;
        _timeZonesCheckBox.Checked = Shared.Settings.ShowTimeZones;
        _politicalMapCheckBox.Checked = Shared.Settings.ShowPoliticalMap;

        var currentInterval = Shared.Settings.UpdateInterval;
        for (int i = 0; i < _updateIntervalCombo.Items.Count; i++)
        {
            if (_updateIntervalCombo.Items[i] is ComboBoxItem item && 
                item.Value.Equals(currentInterval))
            {
                _updateIntervalCombo.SelectedIndex = i;
                break;
            }
        }
    }

    private void SaveSettings()
    {
        Shared.Settings.ShowISS = _issCheckBox.Checked;
        Shared.Settings.ShowTimeZones = _timeZonesCheckBox.Checked;
        Shared.Settings.ShowPoliticalMap = _politicalMapCheckBox.Checked;

        if (_updateIntervalCombo.SelectedItem is ComboBoxItem item)
        {
            Shared.Settings.UpdateInterval = (UpdateInterval)item.Value;
            TaskManager.UpdateTaskSchedule((UpdateInterval)item.Value);
        }
    }

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        SaveSettings();
    }

    private void OnUpdateIntervalChanged(object? sender, EventArgs e)
    {
        SaveSettings();
    }

    private async void OnPreviewClick(object? sender, EventArgs e)
    {
        _previewButton.Enabled = false;
        _previewButton.Text = "Updating...";

        try
        {
            SaveSettings();
            
            if (TaskManager.RunTaskNow())
            {
                _previewButton.Text = "Updated!";
                await Task.Delay(2000);
            }
            else
            {
                _previewButton.Text = "Failed to update";
                await Task.Delay(2000);
            }
        }
        finally
        {
            _previewButton.Text = "Update Wallpaper Now";
            _previewButton.Enabled = true;
        }
    }

    private void OnResetClick(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all settings to their default values?",
            "Reset Settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            Shared.Settings.ResetToDefaults();
            LoadSettings();
        }
    }

    private void StartWallpaperMonitoring()
    {
        _wallpaperMonitor = new WallpaperMonitor();
        _wallpaperMonitor.WallpaperChanged += OnWallpaperChanged;
        _wallpaperMonitor.Start();
    }

    private void OnWallpaperChanged(bool isOurWallpaper)
    {
        if (!isOurWallpaper)
        {
            // User switched to a different wallpaper - disable our task and exit
            TaskManager.EnableTask(false);
            Shared.Settings.IsActive = false;
            
            // Close on UI thread
            if (InvokeRequired)
                Invoke(new Action(() => this.Close()));
            else
                this.Close();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _wallpaperMonitor?.Stop();
        _wallpaperMonitor?.Dispose();
        base.OnFormClosed(e);
    }

    /// <summary>
    /// Gets the current task status as a user-friendly string.
    /// </summary>
    /// <returns>A string describing the current task status.</returns>
    private static string GetTaskStatusText()
    {
        if (!TaskManager.TaskExists())
            return "Task not found - reinstall may be required";

        if (!TaskManager.IsTaskEnabled())
            return "Task is disabled";

        var taskInfo = TaskManager.GetTaskInfo();
        if (taskInfo.HasValue)
        {
            var (state, nextRun) = taskInfo.Value;
            var nextRunText = nextRun?.ToString("MMM d, h:mm tt") ?? "Not scheduled";
            
            // Get trigger count for additional info
            var triggers = TaskManager.GetTriggerInfo();
            var triggerCount = triggers.Count;
            
            return $"Task active with {triggerCount} triggers - Next: {nextRunText}";
        }

        return "Task status unknown";
    }

    // Helper class for ComboBox items
    private class ComboBoxItem(string display, object value)
    {
        public string Display { get; } = display;
        public object Value { get; } = value;

        public override string ToString() => Display;
    }
}