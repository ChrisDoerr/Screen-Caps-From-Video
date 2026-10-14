# Implementation Plan

## Goal

Implement ScreenCapsFromVideo as a Windows 11 C# WPF application that extracts JPEG screen caps from a user-selected video using FFmpeg.

## Proposed implementation

### 1. Establish the application foundation

- Create or confirm a WPF desktop project targeting a Windows-compatible .NET version.
- Keep the UI, capture orchestration, FFmpeg process integration, and output-path handling separated so each can be tested and maintained independently.
- Use the existing project conventions if the repository already contains an application structure.

### 2. Build the user interface

- Provide a video-file picker and display the selected path or filename.
- Provide a whole-number screen-caps-per-minute input with up/down spinner buttons, defaulting to 3 and limited to 1–10.
- Provide original-size and half-size choices, defaulting to original size.
- Provide a Start button, capture status, and progress bar.
- Show a Show Caps Folder button whenever FFmpeg is not running; create the folder on demand if needed.
- Keep controls and status understandable while capture is running; prevent conflicting starts.

### 3. Implement capture configuration and output paths

- Validate the selected file and capture settings before starting FFmpeg.
- Resolve the `caps` output directory relative to the application directory, not the process working directory.
- Create the output directory when needed.
- Derive the interval in seconds from the configured rate (`60 / screen caps per minute`).
- Select the first available frame and then the first frame at or after each interval, using the video timestamps.
- Use each selected frame's timestamp in its filename. Add a numeric suffix when a generated name already exists rather than overwriting it.

### 4. Integrate FFmpeg

- Start FFmpeg as a child process using arguments that safely handle paths containing spaces.
- Confirm the selected video can be processed and determine its duration as needed for progress reporting.
- Extract JPEG frames at the configured interval with timestamp-based frame selection and variable-frame-rate output. Apply half-size scaling only when that option is selected, preserving aspect ratio.
- Report process startup errors, non-zero exit codes, and useful FFmpeg diagnostics in the UI.
- Read FFmpeg's selected-frame timestamps for output names. This avoids assuming a constant frame rate or an exact match between requested and source frame timestamps.
- Parse FFmpeg progress output when available, marshal updates back to the WPF UI thread, and avoid displaying misleading progress if duration or progress data is unavailable.
- Allow cancellation and stop the FFmpeg process tree cleanly.

### 5. Handle completion and open the output folder

- On successful completion, update the status and keep the output-folder action available.
- Open the resolved `caps` directory in Windows File Explorer.
- Do not expose success actions after a failed or cancelled capture.

### 6. Verify the implementation

- Test settings boundaries, interval calculation, output-path resolution, filename generation, and FFmpeg argument construction.
- Test with representative videos, including paths with spaces and videos of different durations and frame rates.
- Confirm both size modes, progress behavior, failure reporting, and Explorer launch on Windows.
- Build the application and run the relevant automated tests.

## Validated behavior and remaining considerations

- The first available frame is selected, followed by frames at least one configured interval apart. A short video still produces its first frame.
- Timestamp-based selection was checked at 1, 7, and 10 screen caps per minute, including a 61-second video to exercise captures near the end.
- FFmpeg's `-progress` output is used with the input duration for determinate progress; the UI remains indeterminate until a duration is known.
- `caps` is resolved beneath the running application's directory (`AppContext.BaseDirectory`).
- For odd input dimensions, halving each dimension necessarily rounds down to an integer pixel.
