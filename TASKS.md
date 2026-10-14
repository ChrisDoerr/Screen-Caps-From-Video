# Implementation Tasks

Tasks are listed in a suggested implementation order. Check items off as they are completed.

## Project foundation

- [ ] Create or confirm the C# WPF project and Windows target framework.
- [ ] Establish application structure for UI, capture workflow, FFmpeg integration, and output paths.
- [ ] Add or confirm a test project for logic that can be tested without launching the WPF UI.

## User interface

- [ ] Add a video file picker and selected-file display.
- [ ] Add screenshots-per-minute input with default 2 and inclusive limits of 1–10.
- [ ] Add original-size and half-size options, defaulting to original size.
- [ ] Add Start button, capture status, and progress display.
- [ ] Add an output-folder button that is enabled only after successful capture.
- [ ] Keep the UI responsive and prevent conflicting capture starts while FFmpeg is running.

## Capture and output

- [ ] Validate the selected video and settings before launching FFmpeg.
- [ ] Resolve the `caps` directory relative to the application directory and create it when missing.
- [ ] Calculate the capture interval as `60 / screenshots per minute`.
- [ ] Decide and implement behavior for short videos, including whether to capture at time zero.
- [ ] Implement timestamp-based JPEG filenames that include the source video name.
- [ ] Prevent silent overwrites when filenames already exist.
- [ ] Build FFmpeg arguments safely for paths containing spaces.
- [ ] Extract frames at the requested interval and apply half-size scaling only when selected.
- [ ] Verify the FFmpeg approach for frame timestamps, filtering, and output naming on representative videos.

## Process, progress, and completion

- [ ] Run FFmpeg asynchronously without blocking the WPF UI.
- [ ] Read FFmpeg progress output and update the progress display on the UI thread when reliable.
- [ ] Use indeterminate progress or an appropriate status when percentage progress is unavailable.
- [ ] Report FFmpeg startup failures, processing errors, non-zero exit codes, and useful diagnostics.
- [ ] Handle process completion and ensure failures are not reported as success.
- [ ] Decide whether to provide cancellation; if included, stop FFmpeg cleanly and report cancellation distinctly.
- [ ] Open the `caps` directory in Windows File Explorer after successful capture.

## Tests and validation

- [ ] Test input validation for values below 1, from 1 through 10, above 10, and non-integer input.
- [ ] Test interval calculations, including 2 screenshots per minute producing a 30-second interval.
- [ ] Test output-path resolution independently of the process working directory.
- [ ] Test filename formatting and collision handling.
- [ ] Test original-size and half-size output dimensions.
- [ ] Test short videos, representative frame rates, and paths containing spaces.
- [ ] Test progress, FFmpeg failure reporting, successful completion, and Explorer launch on Windows.
- [ ] Build the application and run the relevant automated tests.
