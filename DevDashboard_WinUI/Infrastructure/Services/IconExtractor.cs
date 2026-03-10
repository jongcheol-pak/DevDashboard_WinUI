using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using NM = DevDashboard.Infrastructure.Services.ShellIconNativeMethods;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// 파일 및 UWP 앱에서 아이콘을 추출하여 PNG로 저장하는 유틸리티.
/// AppGroup 프로젝트의 IconHelper를 DevDashboard용으로 포팅했습니다.
/// </summary>
internal static class IconExtractor
{
    /// <summary>
    /// 파일에서 아이콘을 추출하여 PNG로 저장합니다.
    /// </summary>
    public static async Task<string?> ExtractIconAndSaveAsync(
        string filePath, string outputDirectory, int size = 48, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(3);
        if (string.IsNullOrEmpty(filePath)) return null;

        bool isUwpApp = filePath.StartsWith("shell:AppsFolder", StringComparison.OrdinalIgnoreCase);
        if (!isUwpApp && !File.Exists(filePath)) return null;

        try
        {
            using var cts = new CancellationTokenSource(timeout.Value);
            return await Task.Run(() =>
            {
                Bitmap? iconBitmap = null;

                if (isUwpApp)
                    iconBitmap = ExtractUwpAppIcon(filePath);
                else
                    iconBitmap = ExtractIconWithoutArrow(filePath, size);

                if (iconBitmap is null) return null;

                try
                {
                    Directory.CreateDirectory(outputDirectory);
                    string iconFileName = GenerateUniqueIconFileName(filePath, iconBitmap);
                    string iconFilePath = Path.Combine(outputDirectory, iconFileName);

                    if (File.Exists(iconFilePath))
                    {
                        iconBitmap.Dispose();
                        return iconFilePath;
                    }

                    cts.Token.ThrowIfCancellationRequested();
                    using var stream = new FileStream(iconFilePath, FileMode.Create);
                    iconBitmap.Save(stream, ImageFormat.Png);
                    return iconFilePath;
                }
                finally
                {
                    iconBitmap.Dispose();
                }
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"아이콘 추출 타임아웃: {filePath}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"아이콘 추출 오류: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// IShellItemImageFactory → SHGetImageList → ExtractIconEx → SHGetFileInfo 순서로 시도
    /// </summary>
    private static Bitmap? ExtractIconWithoutArrow(string targetPath, int size = 48)
    {
        // Method 1: IShellItemImageFactory
        var bmp = TryExtractViaShellItemImageFactory(targetPath, size);
        if (bmp is not null) return bmp;

        // Method 2: SHGetImageList
        bmp = TryExtractViaSHGetImageList(targetPath, size);
        if (bmp is not null) return bmp;

        // Method 3: ExtractIconEx (32x32)
        try
        {
            nint[] hIcons = new nint[1];
            uint count = NM.ExtractIconEx(targetPath, 0, hIcons, null, 1);
            if (count > 0 && hIcons[0] != 0)
            {
                try
                {
                    using var icon = Icon.FromHandle(hIcons[0]);
                    return new Bitmap(icon.ToBitmap());
                }
                finally
                {
                    NM.DestroyIcon(hIcons[0]);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ExtractIconEx 실패: {ex.Message}");
        }

        // Method 4: SHGetFileInfo (32x32)
        return TryExtractViaSHGetFileInfo(targetPath);
    }

    private static Bitmap? TryExtractViaShellItemImageFactory(string filePath, int size = 48)
    {
        try
        {
            Guid guid = NM.IShellItemImageFactoryGuid;
            int hr = NM.SHCreateItemFromParsingName(filePath, 0, ref guid, out NM.IShellItemImageFactory factory);
            if (hr != 0 || factory is null) return null;

            try
            {
                int[] sizes = size <= 32 ? [32] : size <= 48 ? [48, 32] : [256, 128, 64, 48, 32];
                nint hBitmap = 0;

                foreach (int trySize in sizes)
                {
                    var iconSize = new NM.SIZE(trySize, trySize);
                    hr = factory.GetImage(iconSize, NM.SIIGBF.SIIGBF_BIGGERSIZEOK | NM.SIIGBF.SIIGBF_ICONONLY, out hBitmap);
                    if (hr == 0 && hBitmap != 0) break;
                    hr = factory.GetImage(iconSize, NM.SIIGBF.SIIGBF_BIGGERSIZEOK, out hBitmap);
                    if (hr == 0 && hBitmap != 0) break;
                }

                if (hr != 0 || hBitmap == 0) return null;

                try { return ConvertHBitmapToArgbBitmap(hBitmap); }
                finally { NM.DeleteObject(hBitmap); }
            }
            finally
            {
                Marshal.ReleaseComObject(factory);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IShellItemImageFactory 실패 ({filePath}): {ex.Message}");
            return null;
        }
    }

    private static Bitmap? TryExtractViaSHGetImageList(string filePath, int size = 48)
    {
        try
        {
            var shfi = new NM.SHFILEINFO();
            nint result = NM.SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), NM.SHGFI_SYSICONINDEX);
            if (result == 0) return null;

            int iconIndex = shfi.iIcon;
            int[] shilSizes = size <= 32 ? [NM.SHIL_LARGE]
                : size <= 48 ? [NM.SHIL_EXTRALARGE, NM.SHIL_LARGE]
                : [NM.SHIL_JUMBO, NM.SHIL_EXTRALARGE, NM.SHIL_LARGE];

            foreach (int shilSize in shilSizes)
            {
                Guid iid = NM.IID_IImageList;
                int hr = NM.SHGetImageList(shilSize, ref iid, out NM.IImageList imageList);
                if (hr != 0 || imageList is null) continue;

                try
                {
                    nint hIcon = 0;
                    hr = imageList.GetIcon(iconIndex, NM.ILD_TRANSPARENT, out hIcon);
                    if (hr != 0 || hIcon == 0)
                        hr = imageList.GetIcon(iconIndex, NM.ILD_IMAGE, out hIcon);

                    if (hr == 0 && hIcon != 0)
                    {
                        try
                        {
                            using var icon = Icon.FromHandle(hIcon);
                            return new Bitmap(icon.ToBitmap());
                        }
                        finally
                        {
                            NM.DestroyIcon(hIcon);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(imageList);
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SHGetImageList 실패 ({filePath}): {ex.Message}");
            return null;
        }
    }

    private static Bitmap? TryExtractViaSHGetFileInfo(string filePath)
    {
        try
        {
            var shfi = new NM.SHFILEINFO();
            nint result = NM.SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), NM.SHGFI_ICON | NM.SHGFI_LARGEICON);
            if (result == 0 || shfi.hIcon == 0) return null;

            try
            {
                using var icon = Icon.FromHandle(shfi.hIcon);
                return new Bitmap(icon.ToBitmap());
            }
            finally
            {
                NM.DestroyIcon(shfi.hIcon);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SHGetFileInfo 실패 ({filePath}): {ex.Message}");
            return null;
        }
    }

    #region UWP 앱 아이콘 추출

    private static Bitmap? ExtractUwpAppIcon(string shellPath)
    {
        try
        {
            string aumid = shellPath.Replace("shell:AppsFolder\\", "", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(aumid)) return null;

            string packageFamilyName = aumid.Contains('!') ? aumid[..aumid.IndexOf('!')] : aumid;
            string packageName = packageFamilyName.Contains('_') ? packageFamilyName[..packageFamilyName.IndexOf('_')] : packageFamilyName;
            if (string.IsNullOrEmpty(packageName)) return null;

            // PackageManager로 패키지 검색
            var pm = new Windows.Management.Deployment.PackageManager();
            var packages = pm.FindPackagesForUser("");

            var appPackage = packages.FirstOrDefault(p =>
                p.Id.FamilyName.Equals(packageFamilyName, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Name.StartsWith(packageName, StringComparison.OrdinalIgnoreCase));

            if (appPackage is not null)
            {
                var icon = ExtractUwpIconFromPackage(appPackage);
                if (icon is not null) return icon;
            }

            // 폴백: Shell API
            return ExtractIconFromShellPath(shellPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UWP 아이콘 추출 실패: {ex.Message}");
            try { return ExtractIconFromShellPath(shellPath); }
            catch { return null; }
        }
    }

    private static Bitmap? ExtractUwpIconFromPackage(Windows.ApplicationModel.Package package)
    {
        try
        {
            string installPath = package.InstalledLocation.Path;
            string manifestPath = Path.Combine(installPath, "AppxManifest.xml");
            if (!File.Exists(manifestPath)) return null;

            var manifest = new XmlDocument();
            manifest.Load(manifestPath);

            var nsManager = new XmlNamespaceManager(manifest.NameTable);
            nsManager.AddNamespace("ns", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
            nsManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

            string[] logoXPaths =
            [
                "/ns:Package/ns:Applications/ns:Application/uap:VisualElements/@Square44x44Logo",
                "/ns:Package/ns:Applications/ns:Application/uap:VisualElements/@Square150x150Logo",
                "/ns:Package/ns:Properties/ns:Logo"
            ];

            foreach (string xpath in logoXPaths)
            {
                var logoNode = manifest.SelectSingleNode(xpath, nsManager);
                string? logoRelativePath = logoNode?.InnerText ?? logoNode?.Value;
                if (string.IsNullOrEmpty(logoRelativePath)) continue;

                string logoBaseName = Path.GetFileNameWithoutExtension(logoRelativePath);
                string logoDir = Path.Combine(installPath, Path.GetDirectoryName(logoRelativePath) ?? "");
                if (!Directory.Exists(logoDir))
                {
                    logoDir = Path.Combine(installPath, "Assets");
                    if (!Directory.Exists(logoDir)) continue;
                }

                // 가장 큰 해상도 로고 파일 검색
                string? bestFile = null;
                long bestSize = 0;

                foreach (string pattern in new[] { $"{logoBaseName}*.png", $"*{logoBaseName}*.png" })
                {
                    try
                    {
                        foreach (string file in Directory.GetFiles(logoDir, pattern, SearchOption.AllDirectories))
                        {
                            string fileName = Path.GetFileName(file).ToLowerInvariant();
                            if (fileName.Contains("contrast-") || fileName.Contains("_contrast")) continue;

                            var fi = new FileInfo(file);
                            if (fi.Length > bestSize)
                            {
                                bestSize = fi.Length;
                                bestFile = file;
                            }
                        }
                    }
                    catch { /* 접근 권한 없는 폴더 무시 */ }

                    if (bestFile is not null) break;
                }

                if (bestFile is not null && File.Exists(bestFile))
                {
                    using var stream = new FileStream(bestFile, FileMode.Open, FileAccess.Read);
                    using var original = new Bitmap(stream);
                    return new Bitmap(original);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"패키지 아이콘 추출 실패: {ex.Message}");
            return null;
        }
    }

    private static Bitmap? ExtractIconFromShellPath(string shellPath)
    {
        string aumid = shellPath.Replace("shell:AppsFolder\\", "", StringComparison.OrdinalIgnoreCase);
        string appsFolderClsid = "{1e87508d-89c2-42f0-8a7e-645a0f50ca58}";

        string[] pathFormats = [shellPath, $"shell:::{appsFolderClsid}\\{aumid}", $"::{appsFolderClsid}\\{aumid}"];

        foreach (var path in pathFormats)
        {
            var result = TryExtractViaShellItemImageFactory(path, 48);
            if (result is not null) return result;
        }

        // IShellFolder를 통한 추출 시도
        return TryExtractViaShellFolder(aumid);
    }

    private static Bitmap? TryExtractViaShellFolder(string aumid)
    {
        try
        {
            int hr = NM.SHGetKnownFolderIDList(NM.FOLDERID_AppsFolder, 0, 0, out nint appsFolderPidl);
            if (hr != 0 || appsFolderPidl == 0) return null;

            try
            {
                Guid shellFolderGuid = typeof(NM.IShellFolder).GUID;
                hr = NM.SHBindToObject(0, appsFolderPidl, 0, ref shellFolderGuid, out object shellFolderObj);
                if (hr != 0) return null;

                var shellFolder = (NM.IShellFolder)shellFolderObj;
                uint eaten = 0, attrs = 0;
                hr = shellFolder.ParseDisplayName(0, 0, aumid, out eaten, out nint itemPidl, ref attrs);
                if (hr != 0 || itemPidl == 0) return null;

                try
                {
                    return ExtractIconFromPidl(shellFolder, itemPidl, appsFolderPidl);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(itemPidl);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(appsFolderPidl);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ShellFolder 아이콘 추출 실패: {ex.Message}");
            return null;
        }
    }

    private static Bitmap? ExtractIconFromPidl(NM.IShellFolder folder, nint pidl, nint parentAbsolutePidl)
    {
        try
        {
            // IExtractIcon 시도
            Guid extractIconGuid = typeof(NM.IExtractIconW).GUID;
            int hr = folder.GetUIObjectOf(0, 1, [pidl], ref extractIconGuid, 0, out object extractIconObj);
            if (hr == 0 && extractIconObj is NM.IExtractIconW extractIcon)
            {
                var iconFile = new StringBuilder(260);
                hr = extractIcon.GetIconLocation(0, iconFile, 260, out int iconIndex, out _);
                if (hr == 0)
                {
                    hr = extractIcon.Extract(iconFile.ToString(), (uint)iconIndex, out nint largeIcon, out nint smallIcon, (48 << 16) | 16);
                    if (hr == 0 && largeIcon != 0)
                    {
                        try
                        {
                            using var icon = Icon.FromHandle(largeIcon);
                            return new Bitmap(icon.ToBitmap());
                        }
                        finally
                        {
                            NM.DestroyIcon(largeIcon);
                            if (smallIcon != 0) NM.DestroyIcon(smallIcon);
                        }
                    }
                }
            }

            // 폴백: IShellItemImageFactory via PIDL
            nint absolutePidl = NM.ILCombine(parentAbsolutePidl, pidl);
            if (absolutePidl != 0)
            {
                try
                {
                    Guid imageFactoryGuid = NM.IShellItemImageFactoryGuid;
                    hr = NM.SHCreateItemFromIDList(absolutePidl, ref imageFactoryGuid, out NM.IShellItemImageFactory factory);
                    if (hr == 0 && factory is not null)
                    {
                        try
                        {
                            var size = new NM.SIZE(48, 48);
                            hr = factory.GetImage(size, NM.SIIGBF.SIIGBF_BIGGERSIZEOK, out nint hBitmap);
                            if (hr == 0 && hBitmap != 0)
                            {
                                try { return ConvertHBitmapToArgbBitmap(hBitmap); }
                                finally { NM.DeleteObject(hBitmap); }
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(factory);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(absolutePidl);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PIDL 아이콘 추출 실패: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region HBITMAP 변환

    private static Bitmap? ConvertHBitmapToArgbBitmap(nint hBitmap)
    {
        try
        {
            var bmp = new NM.BITMAP();
            NM.GetObject(hBitmap, Marshal.SizeOf(typeof(NM.BITMAP)), ref bmp);

            if (bmp.bmBitsPixel != 32)
                return new Bitmap(Image.FromHbitmap(hBitmap));

            var result = new Bitmap(bmp.bmWidth, bmp.bmHeight, PixelFormat.Format32bppArgb);
            var bmpData = result.LockBits(
                new Rectangle(0, 0, bmp.bmWidth, bmp.bmHeight),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                int stride = bmp.bmWidth * 4;
                int totalSize = stride * bmp.bmHeight;
                byte[] bits = new byte[totalSize];
                NM.GetBitmapBits(hBitmap, totalSize, bits);
                Marshal.Copy(bits, 0, bmpData.Scan0, totalSize);
            }
            finally
            {
                result.UnlockBits(bmpData);
            }

            // 알파 채널이 모두 0이면 불투명으로 설정
            var alphaData = result.LockBits(
                new Rectangle(0, 0, result.Width, result.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int pixelCount = result.Width * result.Height;
                byte[] pixelData = new byte[pixelCount * 4];
                Marshal.Copy(alphaData.Scan0, pixelData, 0, pixelData.Length);

                bool hasAlpha = false;
                for (int i = 3; i < pixelData.Length; i += 4)
                {
                    if (pixelData[i] != 0) { hasAlpha = true; break; }
                }

                if (!hasAlpha)
                {
                    for (int i = 3; i < pixelData.Length; i += 4)
                        pixelData[i] = 255;
                    Marshal.Copy(pixelData, 0, alphaData.Scan0, pixelData.Length);
                }
            }
            finally
            {
                result.UnlockBits(alphaData);
            }

            return result;
        }
        catch
        {
            try { return new Bitmap(Image.FromHbitmap(hBitmap)); }
            catch { return null; }
        }
    }

    #endregion

    #region 유틸리티

    private static string GenerateUniqueIconFileName(string filePath, Bitmap iconBitmap)
    {
        using var md5 = MD5.Create();
        byte[] filePathBytes = Encoding.UTF8.GetBytes(filePath);
        using var ms = new MemoryStream();
        iconBitmap.Save(ms, ImageFormat.Png);
        byte[] bitmapBytes = ms.ToArray();

        byte[] combined = new byte[filePathBytes.Length + bitmapBytes.Length];
        filePathBytes.CopyTo(combined, 0);
        bitmapBytes.CopyTo(combined, filePathBytes.Length);

        byte[] hash = md5.ComputeHash(combined);
        string hashStr = BitConverter.ToString(hash).Replace("-", "")[..16].ToLowerInvariant();
        return $"{Path.GetFileNameWithoutExtension(filePath)}_{hashStr}.png";
    }

    #endregion
}
