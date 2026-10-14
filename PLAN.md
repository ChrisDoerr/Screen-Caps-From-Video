# Implementation Plan

## Goal

Implement ScreenCapsFromVideo as a Windows 11 C# WPF application that extracts JPEG screenshots from a user-selected video using FFmpeg.

## Proposed implementation

### 1. Establish the application foundation

- Create or confirm a WPF desktop project targeting a Windows-compatible .NET version.
- Keep the UI, capture orchestration, FFmpeg process integration, and output-path handling separated so each can be tested and maintained independently.
- Use the existing project conventions if the repository already contains an application structure.

### 2. Build the user interface

- Provide a video-file picker and display the selected path or filename.
- Provide a whole-number screenshots-per-minute input, defaulting to 2 and limited to 1–10.
- Provide original-size and half-size choices, defaulting to original size.
- Provide a Start button, capture status, and progress bar.
- Show an Open Output Folder button only after a successful capture.
- Keep controls and status understandable while capture is running; prevent conflicting starts.

### 3. Implement capture configuration and output paths

- Validate the selected file and capture settings before starting FFmpeg.
- Resolve the `caps` output directory relative to the application directory, not the process working directory.
- Create the output directory when needed.
- Derive the interval in seconds from the configured rate (`60 / screenshots per minute`).
- Define and consistently apply the timestamp-based filename format. Ensure files from separate videos and repeated runs are not silently overwritten.

### 4. Integrate FFmpeg

- Start FFmpeg as a child process using arguments that safely handle paths containing spaces.
- Confirm the selected video can be processed and determine its duration as needed for progress reporting.
- Extract JPEG frames at the configured interval. Apply half-size scaling only when that option is selected, preserving aspect ratio.
- Report process startup errors, non-zero exit codes, and useful FFmpeg diagnostics in the UI.
- Prototype and verify the FFmpeg filter/output naming approach for interval selection and timestamps before finalizing it; behavior can vary with timestamps, frame rates, and media formats.
- Parse FFmpeg progress output when available, marshal updates back to the WPF UI thread, and avoid displaying misleading progress if duration or progress data is unavailable.
- Ensure the process can be stopped cleanly if cancellation is included in the final UX.

### 5. Handle completion and open the output folder

- On successful completion, update the status and enable the output-folder action.
- Open the resolved `caps` directory in Windows File Explorer.
- Do not expose success actions after a failed or cancelled capture.

### 6. Verify the implementation

- Test settings boundaries, interval calculation, output-path resolution, filename generation, and FFmpeg argument construction.
- Test with representative videos, including paths with spaces and videos of different durations and frame rates.
- Confirm both size modes, progress behavior, failure reporting, and Explorer launch on Windows.
- Build the application and run the relevant automated tests.

## Key design decisions to validate

- Confirm the exact timestamp format and how collisions are handled across repeated runs.
- Confirm how the capture schedule handles very short videos and whether a frame at time zero is included.
- Confirm FFmpeg progress availability for the chosen extraction command and present indeterminate progress when a trustworthy percentage cannot be calculated.
- Confirm the application's deployment/output directory is the intended base for the `caps` folder.
