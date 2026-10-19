using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Vigilante8Launcher;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new LauncherForm());
    }
}

internal sealed class LauncherForm : Form
{
    static readonly Color Background = Color.FromArgb(17, 21, 27);
    static readonly Color Surface = Color.FromArgb(28, 34, 42);
    static readonly Color Foreground = Color.FromArgb(237, 229, 214);
    static readonly Color Muted = Color.FromArgb(157, 169, 179);
    static readonly Color Accent = Color.FromArgb(190, 118, 49);
    static readonly Color Input = Color.FromArgb(43, 50, 60);
    static readonly Color Good = Color.FromArgb(119, 205, 144);

    readonly string _directory = AppContext.BaseDirectory;
    readonly string _gameExe;
    readonly string _iniPath;
    readonly Label _status;
    readonly Label _installStatus;
    readonly TextBox _cuePath;
    readonly Button _play;
    readonly Button _install;
    readonly ComboBox _resolution;
    readonly ComboBox _antialiasing;
    readonly ComboBox _msaa;
    readonly ComboBox _lod;
    readonly CheckBox _widescreen;
    readonly CheckBox _hdTextures;
    readonly CheckBox _smooth;
    readonly CheckBox _fullscreen;
    readonly CheckBox _drawDistance;
    readonly CheckBox _mipmaps;
    bool _installing;

    public LauncherForm()
    {
        _gameExe = Path.Combine(_directory, "Vigilante8PC.exe");
        _iniPath = Path.Combine(_directory, "interface.ini");

        Text = "Vigilante 8 Classic — Launcher";
        ClientSize = new Size(1040, 655);
        MinimumSize = new Size(1040, 655);
        MaximumSize = new Size(1040, 655);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Background;
        ForeColor = Foreground;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Font;

        AddLabel(this, "VIGILANTE 8 CLASSIC", 28, 18, 800, 44, 25, true, Accent);
        AddLabel(this, "SECOND OFFENSE  •  PC RECOMPILATION  •  COMMUNITY LAUNCHER", 30, 64, 970, 23, 10, false, Muted);
        AddLabel(this, "Manage the original game, graphics and local files — no third-party downloads.", 30, 91, 980, 24, 10, false, Muted);

        var left = AddPanel(28, 133, 474, 443);
        var right = AddPanel(520, 133, 492, 443);
        AddLabel(left, "GAME", 22, 18, 410, 29, 16, true, Foreground);
        _installStatus = AddLabel(left, "Checking game data...", 23, 54, 425, 44, 11, false, Muted);

        _play = AddButton(left, "PLAY SECOND OFFENSE", 22, 103, 429, 55, Accent);
        _play.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
        _play.Click += (_, _) => PlayGame();

        AddLabel(left, "Original PlayStation disc (.CUE / .BIN)", 23, 175, 428, 26, 10, false, Muted);
        _cuePath = new TextBox
        {
            Location = new Point(22, 205),
            Size = new Size(429, 29),
            BackColor = Input,
            ForeColor = Foreground,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f)
        };
        left.Controls.Add(_cuePath);
        AddButton(left, "BROWSE CUE...", 22, 249, 198, 41, Input).Click += (_, _) => PickCue();
        _install = AddButton(left, "IMPORT DISC", 233, 249, 218, 41, Accent);
        _install.Click += async (_, _) => await ImportDiscAsync();

        AddLabel(left, "Only the USA Second Offense disc (SLUS-00868) is supported.", 23, 304, 425, 40, 9, false, Muted);
        AddButton(left, "OPEN LOG", 22, 361, 198, 42, Input).Click += (_, _) => OpenLog();
        AddButton(left, "GAME FOLDER", 233, 361, 218, 42, Input).Click += (_, _) => OpenFolder();

        AddLabel(right, "GRAPHICS", 22, 18, 450, 29, 16, true, Foreground);
        AddLabel(right, "OUTPUT RESOLUTION", 22, 63, 205, 24, 9, false, Muted);
        _resolution = AddCombo(right, 22, 88, 209, "1280x720", "1920x1080", "2560x1440", "3840x2160");
        AddLabel(right, "ANTI-ALIASING", 248, 63, 220, 24, 9, false, Muted);
        _antialiasing = AddCombo(right, 248, 88, 219, "Off", "FXAA");

        _widescreen = AddToggle(right, "Widescreen (in-game)", 22, 145);
        _hdTextures = AddToggle(right, "High-res textures", 248, 145);
        _smooth = AddToggle(right, "Texture smoothing", 22, 187);
        _fullscreen = AddToggle(right, "Fullscreen", 248, 187);
        _drawDistance = AddToggle(right, "Extended draw distance", 22, 229);
        _mipmaps = AddToggle(right, "Mipmaps", 248, 229);

        AddLabel(right, "MSAA", 22, 273, 206, 24, 9, false, Muted);
        _msaa = AddCombo(right, 22, 298, 209, "0", "2", "4", "8");
        AddLabel(right, "LEVEL OF DETAIL", 248, 273, 215, 24, 9, false, Muted);
        _lod = AddCombo(right, 248, 298, 219, "Stock", "Maximum");

        AddButton(right, "APPLY GRAPHICS SETTINGS", 22, 367, 445, 53, Accent)
            .Click += (_, _) => SaveSettings(showMessage: true);

        _status = AddLabel(this, "Ready. This launcher works with an existing official v1.0 installation.",
            30, 594, 970, 44, 10, false, Muted);
        RefreshStatus();
        LoadSettings();
        FormClosing += (_, e) =>
        {
            if (_installing)
            {
                e.Cancel = true;
                MessageBox.Show(this, "Disc import is running. Finish the import before closing the launcher.",
                    "Vigilante 8 Classic", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
    }

    Panel AddPanel(int x, int y, int width, int height)
    {
        var panel = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Surface
        };
        Controls.Add(panel);
        return panel;
    }

    static Label AddLabel(Control parent, string text, int x, int y, int width,
        int height, float size, bool bold, Color color)
    {
        var label = new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular)
        };
        parent.Controls.Add(label);
        return label;
    }

    static Button AddButton(Control parent, string text, int x, int y,
        int width, int height, Color color)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = color,
            ForeColor = Foreground,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TabStop = true
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(222, 150, 74);
        parent.Controls.Add(button);
        return button;
    }

    static ComboBox AddCombo(Control parent, int x, int y, int width,
        params string[] options)
    {
        var combo = new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 31),
            BackColor = Input,
            ForeColor = Foreground,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };
        combo.Items.AddRange(options);
        combo.SelectedIndex = 0;
        parent.Controls.Add(combo);
        return combo;
    }

    static CheckBox AddToggle(Control parent, string text, int x, int y)
    {
        var check = new CheckBox
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(220, 28),
            ForeColor = Foreground,
            BackColor = Surface,
            Cursor = Cursors.Hand
        };
        parent.Controls.Add(check);
        return check;
    }

    bool Installed =>
        File.Exists(Path.Combine(_directory, "game_data", ".recompone-import-complete")) &&
        File.Exists(Path.Combine(_directory, "game_data", "SYSTEM.CNF")) &&
        File.Exists(Path.Combine(_directory, "game_data", "SLUS_008.68"));

    void RefreshStatus()
    {
        bool exe = File.Exists(_gameExe);
        _installStatus.ForeColor = exe && Installed ? Good : Muted;
        _installStatus.Text = !exe
            ? "Missing Vigilante8PC.exe — copy this launcher into the game folder."
            : Installed ? "GAME DATA READY  •  Press Play" : "Game found. Disc import required.";
        _play.Enabled = exe && Installed;
        _install.Enabled = exe && !Installed && !_installing;
    }

    void PickCue()
    {
        using var picker = new OpenFileDialog
        {
            Title = "Choose Vigilante 8: 2nd Offense (USA) CUE",
            Filter = "PlayStation CUE (*.cue)|*.cue|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            RestoreDirectory = true
        };
        if (picker.ShowDialog(this) == DialogResult.OK)
            _cuePath.Text = picker.FileName;
    }

    async Task ImportDiscAsync()
    {
        if (!File.Exists(_gameExe))
        {
            ShowError("Copy the launcher next to Vigilante8PC.exe.");
            return;
        }
        if (Installed)
        {
            ShowError("Game data is already installed. You do not need to import the disc again.");
            return;
        }

        string source;
        try { source = Path.GetFullPath(_cuePath.Text.Trim().Trim('"')); }
        catch (Exception e) { ShowError("Invalid disc path: " + e.Message); return; }
        if (!File.Exists(source) || !Path.GetExtension(source).Equals(".cue", StringComparison.OrdinalIgnoreCase))
        {
            ShowError("Select the matching .CUE file. All referenced BIN files must be beside it.");
            return;
        }

        _installing = true;
        RefreshStatus();
        _status.Text = "Importing game files and preparing audio. This may take a while...";
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = _gameExe,
                WorkingDirectory = _directory,
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "--extract-loose", source, Path.Combine(_directory, "game_data")
                }
            }) ?? throw new InvalidOperationException("Could not start game importer.");
            await process.WaitForExitAsync();
            if (Installed)
            {
                _status.Text = "Disc import completed. You can now press Play.";
                MessageBox.Show(this, "Game data imported successfully.", "Vigilante 8 Classic",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _status.Text = "Disc import did not complete. Inspect v8_latest.log.";
                ShowError("The importer exited with code " + process.ExitCode +
                    " but game_data is incomplete. Click OPEN LOG for details.");
            }
        }
        catch (Exception ex)
        {
            _status.Text = "Disc import failed.";
            ShowError(ex.Message);
        }
        finally
        {
            _installing = false;
            RefreshStatus();
        }
    }

    void PlayGame()
    {
        try
        {
            if (!File.Exists(_gameExe) || !Installed)
                throw new InvalidOperationException("Install the game data before playing.");
            WriteSettings();
            Process.Start(new ProcessStartInfo
            {
                FileName = _gameExe,
                WorkingDirectory = _directory,
                UseShellExecute = true
            });
            _status.Text = "Game launched. Graphics settings saved.";
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    void OpenLog()
    {
        string log = Path.Combine(_directory, "v8_latest.log");
        try
        {
            if (!File.Exists(log)) throw new FileNotFoundException("No log exists yet.", log);
            Process.Start(new ProcessStartInfo { FileName = log, UseShellExecute = true });
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    void OpenFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _directory,
                UseShellExecute = true
            });
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    void LoadSettings()
    {
        var config = ReadRecompOneSection();
        Choose(_resolution, Read(config, "OutputResolution", "1920x1080"));
        Choose(_antialiasing, Read(config, "AntiAliasing", "FXAA"));
        Choose(_msaa, Read(config, "MsaaSamples", "2"));
        Choose(_lod, Read(config, "LevelOfDetail", "Maximum"));
        _widescreen.Checked = ReadBool(config, "Widescreen", true);
        _hdTextures.Checked = ReadBool(config, "HighResolutionTextures", true);
        _smooth.Checked = ReadBool(config, "TextureSmoothing", true);
        _fullscreen.Checked = ReadBool(config, "Fullscreen", false);
        _drawDistance.Checked = ReadBool(config, "ExtendedDrawDistance", true);
        _mipmaps.Checked = ReadBool(config, "TextureMipmaps", true);
    }

    static string Read(Dictionary<string, string> dict, string key, string fallback) =>
        dict.TryGetValue(key, out var value) ? value : fallback;

    static bool ReadBool(Dictionary<string, string> dict, string key, bool fallback) =>
        dict.TryGetValue(key, out var value) && bool.TryParse(value, out bool parsed)
            ? parsed : fallback;

    static void Choose(ComboBox combo, string option)
    {
        foreach (var item in combo.Items)
        {
            if (string.Equals(item?.ToString(), option, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    Dictionary<string, string> ReadRecompOneSection()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_iniPath)) return values;
        bool section = false;
        foreach (string raw in File.ReadLines(_iniPath))
        {
            string line = raw.Trim();
            if (line.StartsWith('['))
            {
                section = line.Equals("[RecompOne]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!section) continue;
            int eq = line.IndexOf('=');
            if (eq > 0) values[line[..eq]] = line[(eq + 1)..];
        }
        return values;
    }

    void SaveSettings(bool showMessage)
    {
        try
        {
            WriteSettings();
            _status.Text = "Graphics settings saved to interface.ini.";
            if (showMessage)
                MessageBox.Show(this, "Settings saved. They take effect on the next game launch.",
                    "Vigilante 8 Classic", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    void WriteSettings()
    {
        if (!File.Exists(_gameExe))
            throw new FileNotFoundException("Vigilante8PC.exe not found in the launcher folder.");

        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GraphicsPreset"] = "Custom",
            ["OutputResolution"] = _resolution.Text,
            ["AntiAliasing"] = _antialiasing.Text,
            ["MsaaSamples"] = _msaa.Text,
            ["LevelOfDetail"] = _lod.Text,
            ["Widescreen"] = _widescreen.Checked.ToString(),
            ["HighResolutionTextures"] = _hdTextures.Checked.ToString(),
            ["TextureSmoothing"] = _smooth.Checked.ToString(),
            ["Fullscreen"] = _fullscreen.Checked.ToString(),
            ["ExtendedDrawDistance"] = _drawDistance.Checked.ToString(),
            ["TextureMipmaps"] = _mipmaps.Checked.ToString()
        };

        // Replace only [RecompOne] keys; preserve all other settings and
        // ImGui docking/layout sections. Keep one original backup.
        var lines = File.Exists(_iniPath)
            ? File.ReadAllLines(_iniPath).ToList()
            : new List<string>();
        if (File.Exists(_iniPath))
        {
            string backup = _iniPath + ".before-launcher.bak";
            if (!File.Exists(backup)) File.Copy(_iniPath, backup);
        }

        int start = lines.FindIndex(line =>
            line.Trim().Equals("[RecompOne]", StringComparison.OrdinalIgnoreCase));
        if (start < 0)
        {
            lines.Insert(0, "[RecompOne]");
            start = 0;
        }
        int end = start + 1;
        while (end < lines.Count && !lines[end].TrimStart().StartsWith('[')) end++;
        var keysWritten = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = start + 1; i < end; i++)
        {
            int eq = lines[i].IndexOf('=');
            if (eq <= 0) continue;
            string key = lines[i][..eq].Trim();
            if (updates.TryGetValue(key, out string? value))
            {
                lines[i] = key + "=" + value;
                keysWritten.Add(key);
            }
        }
        foreach (var (key, value) in updates)
        {
            if (!keysWritten.Contains(key))
                lines.Insert(end++, key + "=" + value);
        }
        File.WriteAllLines(_iniPath, lines, new UTF8Encoding(false));
    }

    void ShowError(string message)
    {
        _status.Text = "Error: " + message;
        MessageBox.Show(this, message, "Vigilante 8 Classic", MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}
