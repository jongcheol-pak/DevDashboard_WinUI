using System.Runtime.InteropServices;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// 쉘/아이콘 API 관련 P/Invoke 선언.
/// IShellItem, IShellItemImageFactory, SHGetImageList 등을 포함합니다.
/// </summary>
internal static class ShellIconNativeMethods
{
    #region 상수

    public const uint SHGFI_ICON = 0x000000100;
    public const uint SHGFI_LARGEICON = 0x000000000;
    public const uint SHGFI_SYSICONINDEX = 0x000004000;

    public const int SHIL_LARGE = 0;      // 32x32
    public const int SHIL_EXTRALARGE = 2;  // 48x48
    public const int SHIL_JUMBO = 4;       // 256x256

    public const uint ILD_TRANSPARENT = 0x00000001;
    public const uint ILD_IMAGE = 0x00000020;

    public static readonly Guid IID_IImageList = new("46EB5926-582E-4017-9FDF-E8998DAA0950");
    public static readonly Guid IShellItemGuid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    public static readonly Guid IShellItemImageFactoryGuid = new("bcc18b79-ba16-442f-80c4-8a59c30c463b");
    public static readonly Guid FOLDERID_AppsFolder = new("1e87508d-89c2-42f0-8a7e-645a0f50ca58");

    #endregion

    #region 구조체

    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public nint hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        public int cx;
        public int cy;
        public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public ushort bmPlanes;
        public ushort bmBitsPixel;
        public nint bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGELISTDRAWPARAMS
    {
        public int cbSize;
        public nint himl;
        public int i;
        public nint hdcDst;
        public int x, y, cx, cy;
        public int xBitmap, yBitmap;
        public int rgbBk, rgbFg;
        public uint fStyle, dwRop, fState, Frame;
        public int crEffect;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGEINFO
    {
        public nint hbmImage;
        public nint hbmMask;
        public int Unused1, Unused2;
        public RECT rcImage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x, y;
    }

    #endregion

    #region 열거형

    public enum SIGDN : uint
    {
        FILESYSPATH = 0x80058000,
    }

    [Flags]
    public enum SIIGBF : uint
    {
        SIIGBF_RESIZETOFIT = 0x00000000,
        SIIGBF_BIGGERSIZEOK = 0x00000001,
        SIIGBF_ICONONLY = 0x00000004,
    }

    #endregion

    #region P/Invoke

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern uint ExtractIconEx(string szFileName, int nIconIndex,
        nint[] phiconLarge, nint[]? phiconSmall, uint nIcons);

    [DllImport("shell32.dll")]
    public static extern nint SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(nint handle);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(nint hObject);

    [DllImport("gdi32.dll")]
    public static extern int GetObject(nint hObject, int nCount, ref BITMAP lpObject);

    [DllImport("gdi32.dll")]
    public static extern int GetBitmapBits(nint hbmp, int cbBuffer, [Out] byte[] lpvBits);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        nint pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        nint pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("shell32.dll")]
    public static extern int SHGetKnownFolderIDList(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
        uint dwFlags, nint hToken, out nint ppidl);

    [DllImport("shell32.dll")]
    public static extern int SHBindToObject(
        nint pShellFolder, nint pidl, nint pbc,
        ref Guid riid, out object ppv);

    [DllImport("shell32.dll")]
    public static extern int SHCreateItemFromIDList(
        nint pidl, ref Guid riid, out IShellItemImageFactory ppv);

    [DllImport("shell32.dll")]
    public static extern nint ILCombine(nint pidl1, nint pidl2);

    [DllImport("shell32.dll", EntryPoint = "#727")]
    public static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

    #endregion

    #region COM 인터페이스

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    public interface IShellItem
    {
        void BindToHandler(nint pbc, ref Guid bhid, ref Guid riid, out nint ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    public interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(SIZE size, SIIGBF flags, out nint phbm);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214E6-0000-0000-C000-000000000046")]
    public interface IShellFolder
    {
        [PreserveSig]
        int ParseDisplayName(nint hwnd, nint pbc,
            [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
            out uint pchEaten, out nint ppidl, ref uint pdwAttributes);

        [PreserveSig]
        int EnumObjects(nint hwnd, uint grfFlags, out nint ppenumIDList);

        [PreserveSig]
        int BindToObject(nint pidl, nint pbc, ref Guid riid, out object ppv);

        [PreserveSig]
        int BindToStorage(nint pidl, nint pbc, ref Guid riid, out object ppv);

        [PreserveSig]
        int CompareIDs(nint lParam, nint pidl1, nint pidl2);

        [PreserveSig]
        int CreateViewObject(nint hwndOwner, ref Guid riid, out object ppv);

        [PreserveSig]
        int GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray)] nint[] apidl, ref uint rgfInOut);

        [PreserveSig]
        int GetUIObjectOf(nint hwndOwner, uint cidl,
            [MarshalAs(UnmanagedType.LPArray)] nint[] apidl,
            ref Guid riid, nint rgfReserved, out object ppv);

        [PreserveSig]
        int GetDisplayNameOf(nint pidl, uint uFlags, out nint pName);

        [PreserveSig]
        int SetNameOf(nint hwnd, nint pidl,
            [MarshalAs(UnmanagedType.LPWStr)] string pszName,
            uint uFlags, out nint ppidlOut);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214FA-0000-0000-C000-000000000046")]
    public interface IExtractIconW
    {
        int GetIconLocation(uint uFlags,
            [MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder szIconFile,
            int cchMax, out int piIndex, out uint pwFlags);

        int Extract([MarshalAs(UnmanagedType.LPWStr)] string pszFile,
            uint nIconIndex, out nint phiconLarge, out nint phiconSmall, uint nIconSize);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    public interface IImageList
    {
        [PreserveSig] int Add(nint hbmImage, nint hbmMask, out int pi);
        [PreserveSig] int ReplaceIcon(int i, nint hicon, out int pi);
        [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
        [PreserveSig] int Replace(int i, nint hbmImage, nint hbmMask);
        [PreserveSig] int AddMasked(nint hbmImage, int crMask, out int pi);
        [PreserveSig] int Draw(ref IMAGELISTDRAWPARAMS pimldp);
        [PreserveSig] int Remove(int i);
        [PreserveSig] int GetIcon(int i, uint flags, out nint picon);
        [PreserveSig] int GetImageInfo(int i, out IMAGEINFO pImageInfo);
        [PreserveSig] int Copy(int iDst, IImageList punkSrc, int iSrc, uint uFlags);
        [PreserveSig] int Merge(int i1, IImageList punk2, int i2, int dx, int dy, ref Guid riid, out nint ppv);
        [PreserveSig] int Clone(ref Guid riid, out nint ppv);
        [PreserveSig] int GetImageRect(int i, out RECT prc);
        [PreserveSig] int GetIconSize(out int cx, out int cy);
        [PreserveSig] int SetIconSize(int cx, int cy);
        [PreserveSig] int GetImageCount(out int pi);
        [PreserveSig] int SetImageCount(uint uNewCount);
        [PreserveSig] int SetBkColor(int clrBk, out int pclr);
        [PreserveSig] int GetBkColor(out int pclr);
        [PreserveSig] int BeginDrag(int iTrack, int dxHotspot, int dyHotspot);
        [PreserveSig] int EndDrag();
        [PreserveSig] int DragEnter(nint hwndLock, int x, int y);
        [PreserveSig] int DragLeave(nint hwndLock);
        [PreserveSig] int DragMove(int x, int y);
        [PreserveSig] int SetDragCursorImage(IImageList punk, int iDrag, int dxHotspot, int dyHotspot);
        [PreserveSig] int DragShowNolock(bool fShow);
        [PreserveSig] int GetDragImage(out POINT ppt, out POINT pptHotspot, ref Guid riid, out nint ppv);
        [PreserveSig] int GetItemFlags(int i, out uint dwFlags);
        [PreserveSig] int GetOverlayImage(int iOverlay, out int piIndex);
    }

    #endregion

    #region 바로가기(.lnk) resolve

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile,
            int cch, nint pfd, uint fFlags);
        void GetIDList(out nint ppidl);
        void SetIDList(nint pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath,
            int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(nint hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    /// <summary>바로가기(.lnk) 파일의 대상 경로를 반환합니다.
    /// resolve 실패 시 null을 반환합니다.</summary>
    public static string? ResolveShortcutTarget(string lnkPath)
    {
        try
        {
            var link = (IShellLinkW)new ShellLink();
            ((IPersistFile)link).Load(lnkPath, 0);
            // SLR_NO_UI(0x1) | SLR_NOUPDATE(0x8): UI 없이, 링크 파일 갱신 없이 resolve
            link.Resolve(nint.Zero, 0x1 | 0x8);

            var sb = new System.Text.StringBuilder(260);
            link.GetPath(sb, sb.Capacity, nint.Zero, 0);
            var target = sb.ToString();
            return string.IsNullOrEmpty(target) ? null : target;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
