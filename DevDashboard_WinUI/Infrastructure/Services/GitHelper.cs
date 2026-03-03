using System.Diagnostics;
using System.Text;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// git 명령어를 실행하여 브랜치명, 커밋 기록, 파일 변경 상태를 조회하는 헬퍼.
/// 동시에 실행 가능한 git 프로세스 수를 세마포어로 제한하여 리소스 고갈을 방지합니다.
/// </summary>
internal static class GitHelper
{
    private readonly record struct GitCommandResult(bool Success, string Output, string Error);

    /// <summary>git 실행 파일 후보 경로 목록 (PATH 검색 포함)</summary>
    private static readonly string[] _gitCandidates =
    [
        "git",
        @"C:\Program Files\Git\cmd\git.exe",
        @"C:\Program Files\Git\bin\git.exe",
        @"C:\Program Files (x86)\Git\cmd\git.exe",
        @"C:\Program Files (x86)\Git\bin\git.exe",
    ];

    /// <summary>동시에 실행 가능한 git 프로세스 수를 제한하는 세마포어 (최대 4개)</summary>
    private static readonly SemaphoreSlim _concurrencyLimiter = new(4, 4);

    /// <summary>Git 저장소 내부 경로인지 여부를 반환합니다.</summary>
    public static async Task<(bool IsRepository, string Error)> IsRepositoryAsync(string workingDirectory, CancellationToken ct = default)
    {
        var result = await RunGitDetailedAsync(workingDirectory, "rev-parse --is-inside-work-tree", ct);
        if (!result.Success)
            return (false, result.Error);

        return (result.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase), string.Empty);
    }

    /// <summary>저장소 루트 경로를 반환합니다. 실패 시 Error에 메시지 반환.</summary>
    public static async Task<(string RepositoryRoot, string Error)> GetRepositoryRootAsync(string workingDirectory, CancellationToken ct = default)
    {
        var result = await RunGitDetailedAsync(workingDirectory, "rev-parse --show-toplevel", ct);
        if (!result.Success)
            return (string.Empty, result.Error);

        return (result.Output.Trim(), string.Empty);
    }

    /// <summary>현재 브랜치명을 반환합니다. 실패 시 Error에 메시지 반환.</summary>
    public static async Task<(string Branch, string Error)> GetBranchAsync(string workingDirectory, CancellationToken ct = default)
    {
        var result = await RunGitDetailedAsync(workingDirectory, "branch --show-current", ct);
        if (!result.Success)
            return (string.Empty, result.Error);

        return (result.Output.Trim(), string.Empty);
    }

    /// <summary>커밋 로그를 GitCommit 목록으로 반환합니다. 실패 시 Error에 메시지 반환.</summary>
    public static async Task<(IReadOnlyList<GitCommit> Commits, string Error)> GetCommitsAsync(string workingDirectory, CancellationToken ct = default)
    {
        var resultOutput = await RunGitDetailedAsync(workingDirectory, "--no-pager log --no-color --date=iso-strict --format=%H%n%an%n%ae%n%ad%n%B%n", ct);
        if (!resultOutput.Success)
            return ([], resultOutput.Error);

        if (string.IsNullOrWhiteSpace(resultOutput.Output))
            return ([], string.Empty);

        var result = new List<GitCommit>();
        var lines = resultOutput.Output.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None);
        for (var i = 0; i < lines.Length;)
        {
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
                i++;

            if (i >= lines.Length)
                break;

            if (!TryParseCommitHash(lines[i], out var commitHash))
            {
                i++;
                continue;
            }

            if (i + 3 >= lines.Length)
                break;

            var authorName = lines[i + 1].Trim();
            var authorEmail = lines[i + 2].Trim();
            var authorDateStr = lines[i + 3].Trim();
            i += 4;

            var messageLines = new List<string>();
            while (i < lines.Length && !IsCommitHashLine(lines[i]))
            {
                messageLines.Add(lines[i]);
                i++;
            }

            while (messageLines.Count > 0 && string.IsNullOrWhiteSpace(messageLines[^1]))
                messageLines.RemoveAt(messageLines.Count - 1);

            var message = messageLines.Count == 0
                ? "(no message)"
                : string.Join("\n", messageLines);

            DateTimeOffset.TryParse(authorDateStr, out var date);
            result.Add(new GitCommit(commitHash, authorName, authorEmail, date, message));
        }

        return (result, string.Empty);
    }

    private static bool IsCommitHashLine(string line) => TryParseCommitHash(line, out _);

    /// <summary>40자리 16진수 문자열이 커밋 해시인지 판별합니다.</summary>
    private static bool TryParseCommitHash(string line, out string commitHash)
    {
        var trimmed = line.Trim().Trim('\'', '"');
        if (trimmed.Length != 40)
        {
            commitHash = string.Empty;
            return false;
        }

        foreach (var c in trimmed)
        {
            if (!Uri.IsHexDigit(c))
            {
                commitHash = string.Empty;
                return false;
            }
        }

        commitHash = trimmed;
        return true;
    }

    /// <summary>세마포어 제어 하에 git 명령을 실행하고 결과를 반환합니다. 타임아웃 30초.</summary>
    private static async Task<GitCommandResult> RunGitDetailedAsync(string workingDirectory, string arguments, CancellationToken ct = default)
    {
        await _concurrencyLimiter.WaitAsync(ct);
        try
        {
            string lastError = string.Empty;

            foreach (var gitExe in _gitCandidates)
            {
                try
                {
                    if (!gitExe.Equals("git", StringComparison.OrdinalIgnoreCase) && !File.Exists(gitExe))
                        continue;

                    var psi = new ProcessStartInfo(gitExe, arguments)
                    {
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                    };

                    using var process = Process.Start(psi);
                    if (process is null) continue;

                    var stdoutTask = process.StandardOutput.ReadToEndAsync();
                    var stderrTask = process.StandardError.ReadToEndAsync();
                    var stderrConsumed = stderrTask
                        .ContinueWith(static _ => { }, TaskScheduler.Default);

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                    try
                    {
                        await process.WaitForExitAsync(timeoutCts.Token);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        try { process.Kill(entireProcessTree: true); } catch { }
                        try { await Task.WhenAll(stdoutTask, stderrConsumed); } catch { }
                        lastError = "git 명령이 30초 안에 응답하지 않아 중단되었습니다.";
                        continue;
                    }

                    await Task.WhenAll(stdoutTask, stderrConsumed);

                    if (process.ExitCode == 0)
                        return new GitCommandResult(true, stdoutTask.Result, string.Empty);

                    var stderr = stderrTask.IsCompletedSuccessfully ? stderrTask.Result.Trim() : string.Empty;
                    var rawError = string.IsNullOrWhiteSpace(stderr)
                        ? $"{gitExe} exited with code {process.ExitCode}"
                        : $"{gitExe}: {stderr}";
                    lastError = NormalizeGitError(rawError);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    lastError = $"{gitExe}: {ex.Message}";
                }
            }

            if (string.IsNullOrWhiteSpace(lastError))
                lastError = "git 실행 파일을 찾지 못했습니다.";

            return new GitCommandResult(false, string.Empty, lastError);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GitHelper] git {arguments} 실패: {ex.Message}");
            return new GitCommandResult(false, string.Empty, ex.Message);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    /// <summary>자주 발생하는 git 오류를 사용자가 바로 조치할 수 있는 메시지로 변환합니다.</summary>
    private static string NormalizeGitError(string error)
    {
        if (error.Contains("dubious ownership", StringComparison.OrdinalIgnoreCase))
            return "Git safe.directory 설정 문제입니다. 터미널에서 다음을 실행하세요: git config --global --add safe.directory \"<저장소경로>\"";

        if (error.Contains("not a git repository", StringComparison.OrdinalIgnoreCase))
            return "Git 저장소가 아닌 경로입니다. 카드의 프로젝트 경로를 확인하세요.";

        return error;
    }
}
