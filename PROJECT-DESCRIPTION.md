# ScreenCapsFromVideo

## Overview

ScreenCapsFromVideo is a Windows desktop application for extracting JPEG screen caps at regular intervals from a locally stored video. The application provides a simple WPF graphical interface and uses FFmpeg, which is assumed to be installed and available on the system `PATH`.

## Platform and technology

- Operating system: Windows 11
- Application: C# with Windows Presentation Foundation (WPF)
- Video processing: FFmpeg invoked as an external process
- Output format: JPEG (`.jpg`)

## User workflow

1. Select a video file from the local filesystem.
2. Choose how many screen caps to capture per minute.
3. Choose whether to keep the video's original dimensions or scale screen caps to half width and half height.
4. Select **Start** to begin extracting screen caps.
5. Monitor capture progress, when FFmpeg provides usable progress information.
6. When capture completes successfully, select the button to open the output folder in Windows File Explorer.

## Functional requirements

### Video selection

- Let the user select a video file from the local filesystem.
- Use the selected video's total duration to determine the capture interval and report progress.
- If the file cannot be read or processed, report the error to the user.

### Capture frequency

- Provide a numeric setting for screen caps per minute.
- Default to **3 screen caps per minute**.
- Accept whole-number values from **1 through 10**, inclusive; reject or prevent values outside this range.
- Convert the selected frequency to a capture interval in seconds using `60 / screen caps per minute`. For example, 2 per minute means one screen cap every 30 seconds.
- Capture frames at regular intervals across the video's runtime, without requesting frames beyond its duration.

### Image dimensions

- Offer two choices:
  - **Original size** — preserve the video's original image dimensions.
  - **Half size** — scale both image dimensions to half their original values while preserving the aspect ratio. For odd dimensions, round down to the nearest whole pixel.
- Default to **Original size**.

### Output

- Save all screen caps in a `caps` directory under the application directory.
- Create the directory automatically if it does not already exist.
- Name each JPEG using the source video's filename (without its extension) and the screen cap's position in the video, formatted as a timestamp. For example: `ExampleVideo_00-01-30.jpg`.
- Do not remove unrelated or previously generated files from the output directory. Avoid silently overwriting existing screen caps.

### Capture progress and completion

- Run FFmpeg without blocking the graphical interface.
- Show a progress bar during capture when progress can be obtained from FFmpeg. If progress is unavailable, keep the interface responsive and communicate the capture state without presenting misleading progress.
- Show a button to open the `caps` directory in Windows File Explorer whenever capture is not running. Create the directory if needed when the button is clicked.
- Report FFmpeg failures and unsuccessful captures to the user; do not present the output-folder action as a successful completion.

## Operational considerations

- Resolve FFmpeg from `PATH`; the application does not bundle FFmpeg.
- Pass the selected input and output paths safely when launching FFmpeg, including paths containing spaces.
- Keep the output location consistent regardless of the process's current working directory by resolving `caps` relative to the application directory.
- Keep the interface usable while FFmpeg is running, and allow the user to see when capture has started and finished.

## Acceptance criteria

- A user can select a local video and start a capture using the default settings.
- Frequencies 1 and 10 are accepted; values below 1, above 10, and non-integer values are not accepted.
- At 2 screen caps per minute, the requested capture interval is 30 seconds.
- The original-size option preserves input dimensions, and the half-size option outputs images at half width and height.
- The application creates and uses its application-relative `caps` directory, and generated filenames include both the video name and the frame's position in the video.
- The interface remains responsive during capture and reports progress when available.
- After successful capture, the user can open the output directory in File Explorer.
- Processing errors are communicated clearly.
