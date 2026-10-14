using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ScreenCapsFromVideo;

public sealed class FfmpegCaptureService
{
    public string OutputDirectory => CaptureLogic.ResolveOutputDirectory();

    private static readonly Regex DurationPattern = new(
        @"Duration:\s*(\d+):(\d{2}):(\d{2}(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex FrameTimestampPattern = new(
        @"pts_time:\s*(-?\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<CaptureResult> CaptureAsync(
        string inputPath,
        int screenCapsPerMinute,
        bool halfSize,
        IProgress<CaptureProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentNullException.ThrowIfNull(progress);
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("The selected video file could not be found.", inputPath);
        }

        if (!CaptureLogic.IsScreenCapsPerMinuteValid(screenCapsPerMinute))
        {
            throw new ArgumentOutOfRangeException(nameof(screenCapsPerMinute), "Capture frequency must be between 1 and 10 screen caps per minute.");
        }

        var outputDirectory = OutputDirectory;
        Directory.CreateDirectory(outputDirectory);

        var stagingDirectory = Path.Combine(outputDirectory, $".capture-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            processStartInfo.ArgumentList.Add("-hide_banner");
            processStartInfo.ArgumentList.Add("-nostats");
            processStartInfo.ArgumentList.Add("-loglevel");
            processStartInfo.ArgumentList.Add("info");
            processStartInfo.ArgumentList.Add("-progress");
            processStartInfo.ArgumentList.Add("pipe:1");
            processStartInfo.ArgumentList.Add("-i");
            processStartInfo.ArgumentList.Add(inputPath);
            processStartInfo.ArgumentList.Add("-vf");
            processStartInfo.ArgumentList.Add(CaptureLogic.BuildVideoFilter(screenCapsPerMinute, halfSize));
            processStartInfo.ArgumentList.Add("-fps_mode");
            processStartInfo.ArgumentList.Add("vfr");
            processStartInfo.ArgumentList.Add("-q:v");
            processStartInfo.ArgumentList.Add("2");
            processStartInfo.ArgumentList.Add("-start_number");
            processStartInfo.ArgumentList.Add("0");
            processStartInfo.ArgumentList.Add("-y");
            processStartInfo.ArgumentList.Add(Path.Combine(stagingDirectory, "frame_%08d.jpg"));

            using var process = new Process { StartInfo = processStartInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException("FFmpeg could not be started.");
            }

            long durationTicks = 0;
            long positionTicks = 0;
            var diagnostics = new List<string>();
            var frameTimestamps = new List<TimeSpan>();
            var stdoutTask = ReadProgressAsync(
                process,
                progress,
                () => Interlocked.Read(ref durationTicks),
                ticks => Interlocked.Exchange(ref positionTicks, ticks));
            var stderrTask = ReadDiagnosticsAsync(
                process,
                diagnostics,
                frameTimestamps,
                ticks =>
                {
                    Interlocked.Exchange(ref durationTicks, ticks);
                    progress.Report(new CaptureProgress(TimeSpan.FromTicks(ticks), TimeSpan.FromTicks(Interlocked.Read(ref positionTicks))));
                });

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                await process.WaitForExitAsync();
                await Task.WhenAll(stdoutTask, stderrTask);
                throw;
            }

            await Task.WhenAll(stdoutTask, stderrTask);

            if (process.ExitCode != 0)
            {
                var detail = diagnostics.Count == 0
                    ? "FFmpeg returned no diagnostic details."
                    : string.Join(Environment.NewLine, diagnostics.TakeLast(12));
                throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}.{Environment.NewLine}{detail}");
            }

            var stagedImages = Directory.GetFiles(stagingDirectory, "frame_*.jpg")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            if (stagedImages.Length == 0)
            {
                throw new InvalidOperationException("FFmpeg completed without producing any screen caps.");
            }

            if (frameTimestamps.Count != stagedImages.Length)
            {
                throw new InvalidOperationException("FFmpeg did not report timestamps for every screen cap.");
            }

            var videoName = Path.GetFileNameWithoutExtension(inputPath);
            for (var index = 0; index < stagedImages.Length; index++)
            {
                var baseName = $"{videoName}_{CaptureLogic.FormatTimestamp(frameTimestamps[index])}";
                MoveWithoutOverwriting(stagedImages[index], outputDirectory, baseName);
            }

            return new CaptureResult(stagedImages.Length, outputDirectory);
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
    }

    private static async Task ReadProgressAsync(
        Process process,
        IProgress<CaptureProgress> progress,
        Func<long> getDurationTicks,
        Action<long> onPosition)
    {
        string? line;
        while ((line = await process.StandardOutput.ReadLineAsync()) is not null)
        {
            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator];
            var value = line[(separator + 1)..];
            if (key == "out_time_us"
                && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var microseconds))
            {
                var positionTicks = Math.Clamp(microseconds, 0, TimeSpan.MaxValue.Ticks / 10) * 10;
                onPosition(positionTicks);
                var durationTicks = getDurationTicks();
                progress.Report(new CaptureProgress(
                    durationTicks > 0 ? TimeSpan.FromTicks(durationTicks) : null,
                    TimeSpan.FromTicks(positionTicks)));
            }
        }
    }

    private static async Task ReadDiagnosticsAsync(
        Process process,
        List<string> diagnostics,
        List<TimeSpan> frameTimestamps,
        Action<long> onDuration)
    {
        string? line;
        while ((line = await process.StandardError.ReadLineAsync()) is not null)
        {
            if (diagnostics.Count == 80)
            {
                diagnostics.RemoveAt(0);
            }

            diagnostics.Add(line);
            var timestampMatch = FrameTimestampPattern.Match(line);
            if (timestampMatch.Success
                && double.TryParse(timestampMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var frameSeconds)
                && double.IsFinite(frameSeconds)
                && frameSeconds <= TimeSpan.MaxValue.TotalSeconds)
            {
                frameTimestamps.Add(TimeSpan.FromSeconds(Math.Max(0, frameSeconds)));
            }

            var match = DurationPattern.Match(line);
            if (match.Success
                && long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
                && int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
                && double.TryParse(match.Groups[3].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var durationSeconds))
            {
                var duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(durationSeconds);
                onDuration(duration.Ticks);
            }
        }
    }

    private static void MoveWithoutOverwriting(string sourcePath, string outputDirectory, string baseName)
    {
        var destination = CaptureLogic.GetNextAvailablePath(outputDirectory, baseName, ".jpg");
        File.Move(sourcePath, destination);
    }
}
