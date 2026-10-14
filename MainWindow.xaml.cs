using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace ScreenCapsFromVideo;

public partial class MainWindow : Window
{
    private readonly FfmpegCaptureService _captureService = new();
    private CancellationTokenSource? _captureCancellation;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select a video",
            Filter = "Video files|*.avi;*.mkv;*.mov;*.mp4;*.m4v;*.mpeg;*.mpg;*.wmv;*.webm;*.flv;*.ts;*.mts;*.m2ts|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        VideoPathTextBox.Text = dialog.FileName;
        StatusTextBlock.Text = "Ready";
        ProgressTextBlock.Text = string.Empty;
        CaptureProgressBar.Visibility = Visibility.Collapsed;
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var inputPath = VideoPathTextBox.Text;
        if (!File.Exists(inputPath))
        {
            MessageBox.Show(this, "Select an existing video file before starting.", "Video required", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!int.TryParse(FrequencyTextBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var screenCapsPerMinute)
            || screenCapsPerMinute is < 1 or > 10)
        {
            MessageBox.Show(this, "Screen caps per minute must be a whole number from 1 to 10.", "Invalid capture frequency", MessageBoxButton.OK, MessageBoxImage.Warning);
            FrequencyTextBox.Focus();
            FrequencyTextBox.SelectAll();
            return;
        }

        SetCaptureRunning(true);
        _captureCancellation = new CancellationTokenSource();
        StatusTextBlock.Text = "Starting FFmpeg...";
        ProgressTextBlock.Text = "Preparing capture";
        CaptureProgressBar.Visibility = Visibility.Visible;
        CaptureProgressBar.IsIndeterminate = true;
        CaptureProgressBar.Value = 0;

        var progress = new Progress<CaptureProgress>(UpdateProgress);
        try
        {
            var result = await _captureService.CaptureAsync(
                inputPath,
                screenCapsPerMinute,
                HalfSizeRadioButton.IsChecked == true,
                progress,
                _captureCancellation.Token);

            StatusTextBlock.Text = $"Capture complete — {result.ScreenCapCount} screen cap{(result.ScreenCapCount == 1 ? "" : "s")} saved.";
            ProgressTextBlock.Text = result.OutputDirectory;
            CaptureProgressBar.IsIndeterminate = false;
            CaptureProgressBar.Value = 100;
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Capture cancelled.";
            ProgressTextBlock.Text = "Temporary output from this capture was discarded.";
            CaptureProgressBar.Visibility = Visibility.Collapsed;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
            or IOException
            or UnauthorizedAccessException
            or OverflowException
            or System.ComponentModel.Win32Exception
            or ArgumentException)
        {
            StatusTextBlock.Text = "Capture failed.";
            ProgressTextBlock.Text = "FFmpeg did not complete the capture.";
            CaptureProgressBar.Visibility = Visibility.Collapsed;
            MessageBox.Show(this, exception.Message, "Capture failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _captureCancellation?.Dispose();
            _captureCancellation = null;
            SetCaptureRunning(false);
        }
    }

    private void UpdateProgress(CaptureProgress progress)
    {
        if (_captureCancellation is null)
        {
            return;
        }

        if (progress.Duration is not { Ticks: > 0 } duration)
        {
            StatusTextBlock.Text = "Capturing screen caps...";
            ProgressTextBlock.Text = $"Processed {FormatDuration(progress.Position)}";
            CaptureProgressBar.IsIndeterminate = true;
            return;
        }

        StatusTextBlock.Text = "Capturing screen caps...";
        var percentage = Math.Clamp(progress.Position.TotalMilliseconds / duration.TotalMilliseconds * 100, 0, 100);
        CaptureProgressBar.IsIndeterminate = false;
        CaptureProgressBar.Value = percentage;
        ProgressTextBlock.Text = $"{percentage:0}% — {FormatDuration(progress.Position)} of {FormatDuration(duration)}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalHours = (long)duration.TotalHours;
        return string.Create(CultureInfo.InvariantCulture, $"{totalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}");
    }

    private void SetCaptureRunning(bool isRunning)
    {
        BrowseButton.IsEnabled = !isRunning;
        FrequencyTextBox.IsEnabled = !isRunning;
        OriginalSizeRadioButton.IsEnabled = !isRunning;
        HalfSizeRadioButton.IsEnabled = !isRunning;
        StartButton.IsEnabled = !isRunning;
        CancelButton.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.IsEnabled = isRunning;
        OpenFolderButton.IsEnabled = !isRunning;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        StatusTextBlock.Text = "Stopping FFmpeg...";
        _captureCancellation?.Cancel();
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var outputDirectory = _captureService.OutputDirectory;
            Directory.CreateDirectory(outputDirectory);
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            };
            startInfo.ArgumentList.Add(outputDirectory);
            Process.Start(startInfo);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception
            or InvalidOperationException
            or ArgumentException)
        {
            MessageBox.Show(this, exception.Message, "Unable to show caps folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FrequencyIncreaseButton_Click(object sender, RoutedEventArgs e)
    {
        AdjustFrequency(1);
    }

    private void FrequencyDecreaseButton_Click(object sender, RoutedEventArgs e)
    {
        AdjustFrequency(-1);
    }

    private void AdjustFrequency(int adjustment)
    {
        var currentValue = int.TryParse(FrequencyTextBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : 3;
        FrequencyTextBox.Text = Math.Clamp(currentValue + adjustment, 1, 10)
            .ToString(CultureInfo.InvariantCulture);
    }

    private void FrequencyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$", RegexOptions.CultureInvariant);
    }

    private void FrequencyTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText)
            || e.DataObject.GetData(DataFormats.UnicodeText) is not string pastedText
            || !Regex.IsMatch(pastedText, @"^\d+$", RegexOptions.CultureInvariant))
        {
            e.CancelCommand();
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _captureCancellation?.Cancel();
    }
}
