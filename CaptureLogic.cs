using System.Globalization;
using System.IO;

namespace ScreenCapsFromVideo;

public static class CaptureLogic
{
    public static string ResolveOutputDirectory() => Path.Combine(AppContext.BaseDirectory, "caps");

    public static bool IsScreenCapsPerMinuteValid(int screenCapsPerMinute) => screenCapsPerMinute is >= 1 and <= 10;

    public static string BuildVideoFilter(int screenCapsPerMinute, bool halfSize)
    {
        var intervalSeconds = (60d / screenCapsPerMinute).ToString("0.###############", CultureInfo.InvariantCulture);
        var filter = $"select=isnan(prev_selected_t)+gte(t-prev_selected_t\\,{intervalSeconds}),showinfo";
        return halfSize ? $"{filter},scale=trunc(iw/2):trunc(ih/2)" : filter;
    }

    public static string FormatTimestamp(TimeSpan timestamp)
    {
        var totalHours = (long)timestamp.TotalHours;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{totalHours:00}-{timestamp.Minutes:00}-{timestamp.Seconds:00}");
    }

    public static string GetNextAvailablePath(string outputDirectory, string baseName, string extension = ".jpg")
    {
        var candidate = Path.Combine(outputDirectory, $"{baseName}{extension}");
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var suffix = 2;
        while (true)
        {
            var nextCandidate = Path.Combine(outputDirectory, $"{baseName} ({suffix}){extension}");
            if (!File.Exists(nextCandidate))
            {
                return nextCandidate;
            }

            suffix++;
        }
    }
}
