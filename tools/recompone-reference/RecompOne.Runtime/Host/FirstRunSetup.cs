using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using RecompOne.Runtime.Cdrom;

namespace RecompOne.Runtime.Host;

// Owns only the setup window. The game creates its ordinary host after this
// window and GL/ImGui context are disposed. Import never blocks the UI thread.
public static class FirstRunSetup
{
    public static unsafe bool Run(string destination, string? initialSource = null)
    {
        GlfwWindowing.Use();
        using var window = Silk.NET.Windowing.Window.Create(WindowOptions.Default with
        {
            Title = "Vigilante 8 Classic | Launcher", Size = new Vector2D<int>(920, 540),
            IsVisible = Environment.GetEnvironmentVariable("RECOMPONE_WINDOW_VISIBLE") != "0",
            VSync = true,
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(4, 5))
        });
        GL? gl = null;
        IInputContext? input = null;
        ImGuiController? ui = null;
        using var cancel = new CancellationTokenSource();
        Task? import = null;
        var gate = new object();
        var progress = new LooseImportProgress("welcome", 0, 1, "");
        string error = "", selected = initialSource ?? "";
        bool installed = V82LooseImporter.IsComplete(destination);
        bool success = false;
        int frames = 0, completionFrames = 0;
        string? fixture = Environment.GetEnvironmentVariable("RECOMPONE_SETUP_TEST_SOURCE");
        string? captures = Environment.GetEnvironmentVariable("RECOMPONE_SETUP_CAPTURE_DIR");
        var captured = new HashSet<string>();

        void Start(string source)
        {
            if (import is { IsCompleted: false }) return;
            source = source.Trim().Trim('"');
            if (source.Length == 0)
            {
                lock (gate) error = "Paste the full path to your CUE or BIN before installing.";
                return;
            }
            selected = source;
            lock (gate)
            {
                error = "";
                progress = new("validating", 0, 1, "Checking disc title ID");
            }
            import = Task.Run(() =>
            {
                try
                {
                    string cue = ResolveCue(source);
                    V82LooseImporter.Import(cue, destination, p =>
                    {
                        lock (gate) progress = p;
                        Console.Error.WriteLine($"[Setup] {p.Phase} {p.Current}/{p.Total} {p.Item}");
                    }, cancel.Token);
                    lock (gate) { success = true; progress = new("complete", 1, 1, "Starting your game..."); }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    lock (gate) { error = ex.Message; progress = new("error", 0, 1, ""); }
                    Console.Error.WriteLine($"[Setup] failed: {ex}");
                }
            });
        }

        window.Load += () =>
        {
            gl = GL.GetApi(window); input = window.CreateInput();
            ui = new ImGuiController(gl, window, input);
            ImGui.GetIO().NativePtr->IniFilename = null;
            ImGui.StyleColorsDark();
        };
        window.Render += dt =>
        {
            ui!.Update((float)Math.Max(dt, 0.001));
            gl!.Viewport(0, 0, (uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y);
            gl.ClearColor(0.07f, 0.07f, 0.08f, 1); gl.Clear(ClearBufferMask.ColorBufferBit);
            LooseImportProgress p; string message; bool done;
            lock (gate) { p = progress; message = error; done = success; }
            bool busy = import != null && !import.IsCompleted && p.Phase != "error";
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(window.Size.X, window.Size.Y));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(26, 22));
            ImGui.Begin("Vigilante 8 Classic Launcher",
                ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoSavedSettings);
            ImGui.SetWindowFontScale(1.55f);
            ImGui.TextColored(new Vector4(0.98f, 0.73f, 0.30f, 1f), "VIGILANTE 8 CLASSIC");
            ImGui.SetWindowFontScale(1f);
            ImGui.TextColored(new Vector4(0.73f, 0.76f, 0.79f, 1f),
                "Second Offense (USA)  |  SLUS-00868  |  PC recompilation");
            ImGui.Separator();
            ImGui.Spacing();
            if (installed)
            {
                ImGui.TextColored(new Vector4(0.48f, 0.87f, 0.57f, 1f),
                    "GAME DATA INSTALLED");
                ImGui.TextWrapped("Your disc was imported successfully. You no longer need to select the BIN/CUE to play.");
                ImGui.Spacing();
                ImGui.TextWrapped("Graphics: use OPTIONS > Video inside the game to set widescreen, resolution and enhanced textures.");
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.55f, 0.31f, 0.13f, 1f));
                if (ImGui.Button("PLAY SECOND OFFENSE", new Vector2(280, 52)))
                {
                    success = true;
                    window.Close();
                }
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.TextColored(new Vector4(0.94f, 0.88f, 0.74f, 1f),
                    "STEP 1   SELECT YOUR ORIGINAL DISC");
                ImGui.TextWrapped("Select the USA PlayStation Vigilante 8: 2nd Offense CUE file. Keep the CUE and every BIN it references together.");
                ImGui.Spacing();
                if (!busy && !done)
                {
                    ImGui.TextDisabled("Disc image path (CUE or BIN):");
                    ImGui.SetNextItemWidth(-1f);
                    ImGui.InputText("##discPath", ref selected, 4096);
                    ImGui.Spacing();
                    if (ImGui.Button("BROWSE FILES...", new Vector2(206, 42)))
                    {
                        try
                        {
                            string? file = ChooseDisc();
                            if (file != null) { selected = file; error = ""; }
                        }
                        catch (Exception ex)
                        {
                            lock (gate) error = "File picker failed: " + ex.Message +
                                ". Paste the path above instead.";
                            Console.Error.WriteLine("[Setup] file picker failed: " + ex);
                        }
                    }
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.55f, 0.31f, 0.13f, 1f));
                    if (ImGui.Button("INSTALL FROM PATH", new Vector2(235, 42)))
                        Start(selected);
                    ImGui.PopStyleColor();
                    ImGui.TextDisabled("Tip: copy a path in Explorer and paste it here. Quotation marks are accepted.");
                }
                if (busy || done)
                {
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(0.94f, 0.88f, 0.74f, 1f),
                        "STEP 2   PREPARE GAME DATA");
                    ImGui.Text(p.Phase == "complete" ? "Installation complete" :
                        p.Phase == "music" ? "Preparing music..." :
                        p.Phase == "validating" ? "Checking disc..." :
                        "Extracting game files...");
                    ImGui.ProgressBar((float)p.Current / Math.Max(1, p.Total), new Vector2(-1, 30));
                    ImGui.TextWrapped(p.Item);
                }
                if (message.Length != 0)
                {
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(1f, 0.55f, 0.41f, 1f), "INSTALLATION ERROR");
                    ImGui.TextWrapped(message);
                    ImGui.TextDisabled("Your BIN/CUE files were not changed. Check v8_latest.log for details.");
                }
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextDisabled("Game data and saves stay in this installation folder. No PS1 disc files are bundled.");
            if (!done && ImGui.Button(busy ? "CANCEL INSTALLATION" : "EXIT", new Vector2(205, 36)))
                window.Close();
            ImGui.End();
            ImGui.PopStyleVar();
            ui.Render();
            string phase = p.Phase is "files" or "music" ? p.Phase + "-" + (p.Current * 4 / Math.Max(1, p.Total)) : p.Phase;
            if (captures != null && frames > 1 && captured.Add(phase)) Capture(gl, window, captures, phase);
            // A process-local fixture calls the same selection/import action as
            // the button. It never sends input to Windows or another process.
            if (++frames == 5 && !installed && (fixture != null || initialSource != null)) Start(fixture ?? initialSource!);
            if (done && ++completionFrames >= 30) window.Close();
            if (fixture != null && p.Phase == "error" && captured.Contains("error")) window.Close();
        };
        // Silk destroys the context before Run returns. Release GPU objects
        // while Closing still has the setup context current.
        window.Closing += () =>
        {
            ui?.Dispose(); ui = null;
            input?.Dispose(); input = null;
            gl?.Dispose(); gl = null;
        };
        try { window.Run(); }
        finally
        {
            cancel.Cancel(); import?.GetAwaiter().GetResult();
            ui?.Dispose(); input?.Dispose(); gl?.Dispose();
        }
        return success;
    }

    public static string ResolveCue(string source)
    {
        source = Path.GetFullPath(source);
        if (!File.Exists(source)) throw new FileNotFoundException("The selected disc file was not found.", source);
        if (Path.GetExtension(source).Equals(".cue", StringComparison.OrdinalIgnoreCase)) return source;
        if (!Path.GetExtension(source).Equals(".bin", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Select a CUE file or one of its BIN files.");
        var matches = Directory.EnumerateFiles(Path.GetDirectoryName(source)!, "*.cue")
            .Where(cue => File.ReadLines(cue).Any(line =>
            {
                var text = line.Trim();
                int first = text.IndexOf('"'), last = text.LastIndexOf('"');
                return text.StartsWith("FILE ", StringComparison.OrdinalIgnoreCase) && first >= 0 && last > first &&
                    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cue)!, text[(first + 1)..last])).Equals(source, StringComparison.OrdinalIgnoreCase);
            })).ToArray();
        if (matches.Length != 1) throw new InvalidDataException("Select the matching CUE file. A BIN alone does not describe all game and audio tracks.");
        return matches[0];
    }

    static unsafe void Capture(GL gl, IWindow window, string directory, string phase)
    {
        int width = window.FramebufferSize.X, height = window.FramebufferSize.Y;
        byte[] pixels = new byte[width * height * 3];
        gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
        fixed (byte* ptr = pixels) gl.ReadPixels(0, 0, (uint)width, (uint)height, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
        Directory.CreateDirectory(directory);
        using var file = File.Create(Path.Combine(directory, "setup-" + phase + ".ppm"));
        file.Write(Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n"));
        for (int y = height - 1; y >= 0; y--) file.Write(pixels, y * width * 3, width * 3);
    }

    // Blittable OPENFILENAMEW: avoids Marshal.SizeOf<OpenFileName>()
    // throwing on managed StringBuilder/string fields in the v1.0 release.
    [StructLayout(LayoutKind.Sequential)]
    struct OpenFileName
    {
        public int Size;
        public nint Owner, Instance, Filter, CustomFilter;
        public int MaxCustomFilter, FilterIndex;
        public nint File;
        public int MaxFile;
        public nint FileTitle;
        public int MaxFileTitle;
        public nint InitialDir, Title;
        public int Flags;
        public short FileOffset, FileExtension;
        public nint DefaultExtension, CustomData, Hook, TemplateName, Reserved;
        public int Reserved2, FlagsEx;
    }

    [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetOpenFileName(ref OpenFileName value);

    [DllImport("comdlg32.dll", EntryPoint = "CommDlgExtendedError")]
    static extern uint CommDlgExtendedError();

    static string? ChooseDisc()
    {
        const int pathCapacity = 32768;
        nint pathBuffer = Marshal.AllocHGlobal(pathCapacity * sizeof(char));
        nint filterBuffer = Marshal.StringToHGlobalUni(
            "PlayStation disc (BIN/CUE)\0*.cue;*.bin\0All files\0*.*\0\0");
        nint titleBuffer = Marshal.StringToHGlobalUni(
            "Select Vigilante 8: 2nd Offense (USA)");
        try
        {
            Marshal.WriteInt16(pathBuffer, 0);
            var dialog = new OpenFileName
            {
                Size = Marshal.SizeOf<OpenFileName>(),
                Filter = filterBuffer,
                FilterIndex = 1,
                File = pathBuffer,
                MaxFile = pathCapacity,
                Title = titleBuffer,
                Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008,
            };
            if (GetOpenFileName(ref dialog))
                return Marshal.PtrToStringUni(pathBuffer);

            uint error = CommDlgExtendedError();
            if (error != 0)
                throw new InvalidOperationException(
                    "Windows file dialog error 0x" + error.ToString("X8"));
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(titleBuffer);
            Marshal.FreeHGlobal(filterBuffer);
            Marshal.FreeHGlobal(pathBuffer);
        }
    }
}
