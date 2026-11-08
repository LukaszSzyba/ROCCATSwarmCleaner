using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("Program działa tylko w systemie Windows.");
    return;
}

using (var identity = WindowsIdentity.GetCurrent())
{
    if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
    {
        Console.WriteLine("Uruchom program jako Administrator.");
        return;
    }
}

bool dryRun = args.Contains("--dry-run");
string[] keywords = ["roccat", "swarm"];
string[] processNames = ["Swarm", "ROCCAT_Swarm", "ROCCAT Swarm", "SwarmMonitor", "RoccatService", "ROCCAT_Swarm_Monitor", "Swarm_Monitor"];

Console.WriteLine(dryRun ? "TRYB PODGLĄDU (nic nie zostanie usunięte)" : "UWAGA: zostaną usunięte pliki i wpisy rejestru ROCCAT Swarm.");
if (!dryRun)
{
    Console.Write("Kontynuować? (t/n): ");
    if (!string.Equals(Console.ReadLine()?.Trim(), "t", StringComparison.OrdinalIgnoreCase)) return;
}

bool Matches(string? s) => s != null && keywords.Any(k => s.Contains(k, StringComparison.OrdinalIgnoreCase));

foreach (var p in Process.GetProcesses())
{
    try
    {
        if (processNames.Any(n => string.Equals(p.ProcessName, n, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"[proces] {p.ProcessName}");
            if (!dryRun) { p.Kill(true); p.WaitForExit(5000); }
        }
    }
    catch { }
}

foreach (var name in new[] { "ROCCAT_Swarm_Service", "RoccatService", "ROCCAT Swarm Service", "SwarmService" })
{
    Console.WriteLine($"[usługa] {name}");
    if (!dryRun)
    {
        Run("sc.exe", $"stop \"{name}\"");
        Run("sc.exe", $"delete \"{name}\"");
    }
}

var folders = new List<string>();
var roots = new[]
{
    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow"),
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
    Path.GetTempPath(),
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
};

foreach (var root in roots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
{
    try
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(root))
        {
            if (Matches(Path.GetFileName(entry)))
                folders.Add(entry);
        }
    }
    catch { }
}

foreach (var path in folders.Distinct(StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"[plik/folder] {path}");
    if (dryRun) continue;
    try
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
        else File.Delete(path);
    }
    catch (Exception ex) { Console.WriteLine($"   błąd: {ex.Message}"); }
}

var hives = new (RegistryKey Hive, string Path)[]
{
    (Registry.CurrentUser, @"Software"),
    (Registry.LocalMachine, @"SOFTWARE"),
    (Registry.LocalMachine, @"SOFTWARE\WOW6432Node"),
    (Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services"),
};

foreach (var (hive, path) in hives)
{
    try
    {
        using var key = hive.OpenSubKey(path, !dryRun);
        if (key == null) continue;
        foreach (var sub in key.GetSubKeyNames().Where(Matches))
        {
            Console.WriteLine($"[rejestr] {hive.Name}\\{path}\\{sub}");
            if (!dryRun) key.DeleteSubKeyTree(sub, false);
        }
    }
    catch (Exception ex) { Console.WriteLine($"   błąd: {ex.Message}"); }
}

var valueKeys = new (RegistryKey Hive, string Path)[]
{
    (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"),
    (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
    (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"),
};
foreach (var (hive, path) in valueKeys)
{
    try
    {
        using var key = hive.OpenSubKey(path, !dryRun);
        if (key == null) continue;
        foreach (var v in key.GetValueNames().Where(n => Matches(n) || Matches(key.GetValue(n)?.ToString())))
        {
            Console.WriteLine($"[autostart] {hive.Name}\\{path} -> {v}");
            if (!dryRun) key.DeleteValue(v, false);
        }
    }
    catch { }
}

var uninstallPaths = new (RegistryKey Hive, string Path)[]
{
    (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
    (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
    (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
};
foreach (var (hive, path) in uninstallPaths)
{
    try
    {
        using var key = hive.OpenSubKey(path, !dryRun);
        if (key == null) continue;
        foreach (var sub in key.GetSubKeyNames())
        {
            bool match;
            using (var s = key.OpenSubKey(sub))
            {
                match = Matches(sub) || Matches(s?.GetValue("DisplayName")?.ToString()) || Matches(s?.GetValue("Publisher")?.ToString());
            }
            if (!match) continue;
            Console.WriteLine($"[odinstalowanie] {hive.Name}\\{path}\\{sub}");
            if (!dryRun) key.DeleteSubKeyTree(sub, false);
        }
    }
    catch (Exception ex) { Console.WriteLine($"   błąd: {ex.Message}"); }
}

Console.WriteLine("Gotowe. Zrestartuj komputer i zainstaluj Swarm ponownie.");

static void Run(string file, string arguments)
{
    try
    {
        using var p = Process.Start(new ProcessStartInfo(file, arguments) { CreateNoWindow = true, UseShellExecute = false });
        p?.WaitForExit(10000);
    }
    catch { }
}
