using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NM = DevDashboard.Infrastructure.Services.ShellIconNativeMethods;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// shell:AppsFolder를 통해 현재 PC에 설치된 앱 목록을 가져오는 서비스.
/// </summary>
internal static class InstalledAppService
{
    /// <summary>
    /// 설치된 앱 목록을 가져옵니다 (백그라운드 스레드에서 실행).
    /// </summary>
    public static async Task<List<InstalledAppInfo>> GetInstalledAppsAsync(CancellationToken ct = default)
    {
        var rawApps = await Task.Run(() => GetAppsFromShellFolder(), ct);
        var result = new List<InstalledAppInfo>();
        var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (displayName, exePath, aumid, iconPath) in rawApps)
        {
            ct.ThrowIfCancellationRequested();

            if (addedNames.Contains(displayName)) continue;
            addedNames.Add(displayName);

            string? icon = null;

            // 1. IconPath(exe 경로)에서 아이콘 추출
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                try { icon = await IconCacheService.GetIconPathAsync(iconPath); } catch { }
            }

            // 2. exe 파일에서 아이콘 추출
            if (string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(exePath) &&
                exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(exePath))
            {
                try { icon = await IconCacheService.GetIconPathAsync(exePath); } catch { }
            }

            // 3. AUMID로 shell:AppsFolder 아이콘 추출
            if (string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(aumid))
            {
                try { icon = await IconCacheService.GetIconPathAsync($"shell:AppsFolder\\{aumid}"); } catch { }
            }

            ct.ThrowIfCancellationRequested();

            result.Add(new InstalledAppInfo
            {
                DisplayName = displayName,
                ExecutablePath = exePath ?? $"shell:AppsFolder\\{aumid}",
                AppUserModelId = aumid,
                IconPath = icon
            });
        }

        result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    /// <summary>
    /// shell:AppsFolder에서 설치된 앱 목록 가져오기 (COM Shell.Application 사용).
    /// </summary>
    private static List<(string DisplayName, string? ExecutablePath, string? AppUserModelId, string? IconPath)> GetAppsFromShellFolder()
    {
        var apps = new List<(string, string?, string?, string?)>();
        dynamic? shell = null;
        dynamic? folder = null;

        try
        {
            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null) return apps;

            shell = Activator.CreateInstance(shellType);
            if (shell is null) return apps;

            folder = shell.NameSpace("shell:AppsFolder");
            if (folder is null) return apps;

            foreach (dynamic item in folder.Items())
            {
                try
                {
                    string? name = item.Name;
                    string? path = item.Path;

                    if (string.IsNullOrEmpty(name))
                    {
                        Marshal.ReleaseComObject(item);
                        continue;
                    }

                    // 시스템 확장 항목 필터링
                    if (name.StartsWith("Microsoft.") &&
                        (name.Contains("Extension") || name.Contains("Client")))
                    {
                        Marshal.ReleaseComObject(item);
                        continue;
                    }

                    string? exePath = null;
                    string? aumid = null;
                    string? iconPath = null;

                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                        {
                            exePath = path;
                            iconPath = path;
                        }
                        else
                        {
                            // AUMID — exe 경로 추출은 아이콘 로딩 단계에서 수행
                            aumid = path;
                        }
                    }

                    apps.Add((name, exePath, aumid, iconPath));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"앱 열거 오류: {ex.Message}");
                }
                finally
                {
                    Marshal.ReleaseComObject(item);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"shell:AppsFolder 접근 오류: {ex.Message}");
        }
        finally
        {
            if (folder is not null) Marshal.ReleaseComObject(folder);
            if (shell is not null) Marshal.ReleaseComObject(shell);
        }

        return apps;
    }

    /// <summary>
    /// AUMID에서 exe 경로 추출 시도 (Shell API + 레지스트리).
    /// </summary>
    private static string? TryGetExePathFromAumid(string aumid)
    {
        if (string.IsNullOrEmpty(aumid)) return null;

        // 방법 1: IShellItem.GetDisplayName(FILESYSPATH)
        try
        {
            Guid shellItemGuid = NM.IShellItemGuid;
            int hr = NM.SHCreateItemFromParsingName(
                $"shell:AppsFolder\\{aumid}", 0, ref shellItemGuid, out NM.IShellItem shellItem);

            if (hr == 0 && shellItem is not null)
            {
                try
                {
                    shellItem.GetDisplayName(NM.SIGDN.FILESYSPATH, out string fsPath);
                    if (!string.IsNullOrEmpty(fsPath) &&
                        fsPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(fsPath))
                        return fsPath;
                }
                catch (Exception ex)
                {
                    // UWP/가상 아이템은 FILESYSPATH 미지원 — ArgumentException 등 발생
                    Debug.WriteLine($"GetDisplayName 실패 ({aumid}): {ex.Message}");
                }
                finally
                {
                    Marshal.ReleaseComObject(shellItem);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SHCreateItemFromParsingName 실패 ({aumid}): {ex.Message}");
        }

        // 방법 2: ActivatableClasses 레지스트리
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                $@"Software\Classes\ActivatableClasses\Package\{aumid.Split('!')[0]}\Server");
            if (key is not null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey?.GetValue("ExePath") is string exePath && File.Exists(exePath))
                        return exePath;
                }
            }
        }
        catch { }

        // 방법 3: App Paths 레지스트리
        try
        {
            using var appPathsKey = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
            if (appPathsKey is not null)
            {
                string? appId = aumid.Contains('!') ? aumid.Split('!').Last() : null;
                string packagePart = aumid.Contains('_') ? aumid.Split('_')[0] : aumid.Split('!')[0];
                string shortName = packagePart.Contains('.') ? packagePart.Split('.').Last() : packagePart;

                foreach (var subKeyName in appPathsKey.GetSubKeyNames())
                {
                    string keyName = Path.GetFileNameWithoutExtension(subKeyName);
                    if ((!string.IsNullOrEmpty(appId) && keyName.Equals(appId, StringComparison.OrdinalIgnoreCase)) ||
                        keyName.Equals(shortName, StringComparison.OrdinalIgnoreCase))
                    {
                        using var subKey = appPathsKey.OpenSubKey(subKeyName);
                        if (subKey?.GetValue("") is string exePath)
                        {
                            exePath = exePath.Trim('"');
                            if (File.Exists(exePath)) return exePath;
                        }
                    }
                }
            }
        }
        catch { }

        return null;
    }
}
