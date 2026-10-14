# ScreenCapsFromVideo

ScreenCapsFromVideo is a Windows desktop app that extracts JPEG screenshots from a local video at regular intervals. It uses a simple WPF interface and calls FFmpeg to generate still frames from the selected video.

## Features

- Select a local video file
- Set screenshot frequency from 1 to 10 per minute
- Choose between original size and half-size output
- Save generated JPEG files into a `caps` folder next to the app
- Show live processing status and progress when FFmpeg provides it
- Open the output folder in Windows File Explorer after a successful run

## Requirements

- Windows 11
- .NET 10 SDK or compatible runtime for building/running the project
- FFmpeg installed and available on your `PATH`

## Project structure

- `App.xaml` / `App.xaml.cs` — application startup
- `MainWindow.xaml` / `MainWindow.xaml.cs` — WPF UI and capture workflow
- `FfmpegCaptureService.cs` — FFmpeg invocation and frame extraction
- `CaptureProgress.cs` — progress/state tracking
- `ScreenCapsFromVideo.csproj` — project configuration

## Build

From the repository root, run:

```powershell
dotnet build
```

To create a Release build:

```powershell
dotnet publish -c Release -o .\release --self-contained false
```

## Run the app

```powershell
dotnet run
```

Or run the generated executable from the publish folder:

```powershell
.\release\ScreenCapsFromVideo.exe
```

## Usage

1. Click Browse and choose a video file.
2. Set the number of screenshots per minute (default: 3).
3. Choose whether to keep the original video size or generate half-size images.
4. Click Start capture.
5. Wait for the process to finish.
6. Use Show caps folder to open the output directory in File Explorer.

## Output location

The app creates and uses a `caps` directory under the application base directory, so output is stable regardless of the current working directory.

Example:

```text
<app-directory>\caps\
```

Generated files use the source video name and the frame timestamp in the filename, for example:

```text
ExampleVideo_00-01-30.jpg
```

## Notes

- FFmpeg is not bundled with the app; it must already be installed and available in `PATH`.
- Existing files are not overwritten silently; the app avoids clobbering previously generated captures.
- If FFmpeg reports an error or the capture is cancelled, the UI will show the failure state clearly.

## License

This project is provided as-is for local desktop use.
