using System;
using System.IO;

namespace ScreenCapsFromVideo.Tests;

public class LogicTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(0, false)]
    [InlineData(11, false)]
    public void IsScreenCapsPerMinuteValid_ReturnsExpectedValues(int screenCapsPerMinute, bool expected)
    {
        Assert.Equal(expected, CaptureLogic.IsScreenCapsPerMinuteValid(screenCapsPerMinute));
    }

    [Fact]
    public void BuildVideoFilter_UsesExpectedIntervalAndOriginalSize()
    {
        var filter = CaptureLogic.BuildVideoFilter(2, false);

        Assert.Contains("select=isnan(prev_selected_t)+gte(t-prev_selected_t\\,30),showinfo", filter);
        Assert.DoesNotContain("scale=trunc(iw/2):trunc(ih/2)", filter);
    }

    [Fact]
    public void BuildVideoFilter_UsesHalfSizeScaling_WhenRequested()
    {
        var filter = CaptureLogic.BuildVideoFilter(3, true);

        Assert.Contains("select=isnan(prev_selected_t)+gte(t-prev_selected_t\\,20),showinfo", filter);
        Assert.Contains("scale=trunc(iw/2):trunc(ih/2)", filter);
    }

    [Fact]
    public void ResolveOutputDirectory_IsBasedOnAppBaseDirectory()
    {
        var expected = Path.Combine(AppContext.BaseDirectory, "caps");

        Assert.Equal(expected, CaptureLogic.ResolveOutputDirectory());
    }

    [Fact]
    public void FormatTimestamp_UsesHoursMinutesSecondsFormatting()
    {
        Assert.Equal("00-00-00", CaptureLogic.FormatTimestamp(TimeSpan.Zero));
        Assert.Equal("00-01-05", CaptureLogic.FormatTimestamp(TimeSpan.FromSeconds(65)));
        Assert.Equal("01-02-03", CaptureLogic.FormatTimestamp(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(2)).Add(TimeSpan.FromSeconds(3))));
    }

    [Fact]
    public void GetNextAvailablePath_UsesTheNextAvailableSuffix()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var baseName = "clip_01-02-03";
            var existingFirst = Path.Combine(directory, $"{baseName}.jpg");
            var existingSecond = Path.Combine(directory, $"{baseName} (2).jpg");
            File.WriteAllText(existingFirst, "first");
            File.WriteAllText(existingSecond, "second");

            var nextPath = CaptureLogic.GetNextAvailablePath(directory, baseName, ".jpg");

            Assert.Equal(Path.Combine(directory, $"{baseName} (3).jpg"), nextPath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
