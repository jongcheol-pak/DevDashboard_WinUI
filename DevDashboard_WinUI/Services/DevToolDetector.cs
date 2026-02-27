using DevDashboard.Models;

namespace DevDashboard.Services;

/// <summary>알려진 개발 도구의 설치 여부를 파일 시스템에서 감지합니다.</summary>
internal static class DevToolDetector
{
    /// <summary>감지 대상 도구 정의 (특수 폴더 + 상대 경로)</summary>
    private static readonly (string Name, Environment.SpecialFolder Folder, string RelativePath)[] KnownTools =
    [
        ("Android Studio", Environment.SpecialFolder.ProgramFiles, @"Android\Android Studio\bin\studio64.exe"),
        ("Antigravity",    Environment.SpecialFolder.LocalApplicationData, @"Programs\Antigravity\Antigravity.exe"),
        ("Unity Hub",      Environment.SpecialFolder.ProgramFiles, @"Unity Hub\Unity Hub.exe"),
        ("VS 2022",        Environment.SpecialFolder.ProgramFiles, @"Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe"),
        ("VS 2026",        Environment.SpecialFolder.ProgramFiles, @"Microsoft Visual Studio\18\Professional\Common7\IDE\devenv.exe"),
        ("VS Code",        Environment.SpecialFolder.LocalApplicationData, @"Programs\Microsoft VS Code\Code.exe"),
    ];

    // 반복 파일 시스템 탐색 방지를 위한 감지 결과 캐시
    private static volatile List<ExternalTool>? _cache;

    // 멀티스레드 캐시 접근 보호용 락 객체
    private static readonly object _lock = new();

    /// <summary>실행 파일이 존재하는 도구만 반환합니다. 이전 감지 결과를 캐시하여 재사용합니다.</summary>
    public static List<ExternalTool> DetectInstalledTools()
    {
        var cached = _cache;
        if (cached is not null)
            return cached;

        lock (_lock)
        {
            // double-checked locking: lock 진입 후 다시 확인
            if (_cache is not null)
                return _cache;

            var result = new List<ExternalTool>();
            foreach (var (name, folder, relativePath) in KnownTools)
            {
                var basePath = Environment.GetFolderPath(folder);
                if (string.IsNullOrEmpty(basePath))
                    continue;

                var fullPath = Path.Combine(basePath, relativePath);
                if (File.Exists(fullPath))
                {
                    result.Add(new ExternalTool
                    {
                        Name = name,
                        ExecutablePath = fullPath
                    });
                }
            }

            _cache = result;
            return _cache;
        }
    }

    /// <summary>캐시를 무효화합니다. 새로고침 시 다음 호출에서 파일 시스템을 재탐색합니다.</summary>
    public static void InvalidateCache()
    {
        lock (_lock) { _cache = null; }
    }
}
