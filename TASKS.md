# Implementation Tasks

Tasks are listed in a suggested implementation order. Check items off as they are completed.

## Project foundation

- [x] Create or confirm the C# WPF project and Windows target framework.
- [x] Establish application structure for UI, capture workflow, FFmpeg integration, and output paths.
- [ ] Add or confirm a test project for logic that can be tested without launching the WPF UI.

## User interface

- [x] Add a video file picker and selected-file display.
- [x] Add screen-caps-per-minute input with up/down controls, default 3, and inclusive limits of 1–10.
- [x] Add original-size and half-size options, defaulting to original size.
- [x] Add Start button, capture status, and progress display.
- [x] Add an output-folder button that is shown whenever capture is not running and creates the folder if needed.
- [x] Keep the UI responsive and prevent conflicting capture starts while FFmpeg is running.

## Capture and output

- [x] Validate the selected video and settings before launching FFmpeg.
- [x] Resolve the `caps` directory relative to the application directory and create it when missing.
- [x] Calculate the capture interval as `60 / screen caps per minute`.
- [x] Select the first available frame and continue at intervals until the video ends.
- [x] Implement timestamp-based JPEG filenames that include the source video name.
- [x] Prevent silent overwrites when filenames already exist.
- [x] Build FFmpeg arguments safely for paths containing spaces.
- [x] Extract frames at the requested interval and apply half-size scaling only when selected.
- [x] Verify the FFmpeg approach for frame timestamps, filtering, and output naming on representative videos.

## Process, progress, and completion

- [x] Run FFmpeg asynchronously without blocking the WPF UI.
- [x] Read FFmpeg progress output and update the progress display on the UI thread when reliable.
- [x] Use indeterminate progress or an appropriate status when percentage progress is unavailable.
- [x] Report FFmpeg startup failures, processing errors, non-zero exit codes, and useful diagnostics.
- [x] Handle process completion and ensure failures are not reported as success.
- [x] Stop FFmpeg cleanly on cancellation and report cancellation distinctly.
- [x] Open the `caps` directory in Windows File Explorer after successful capture.

## Tests and validation

- [ ] Test input validation for values below 1, from 1 through 10, above 10, and non-integer input.
- [x] Test interval calculations, including 2 screen caps per minute producing a 30-second interval.
- [ ] Test output-path resolution independently of the process working directory.
- [ ] Test filename formatting and collision handling.
- [x] Test original-size and half-size output dimensions with FFmpeg command validation.
- [x] Test short videos, representative frame rates, and paths containing spaces with FFmpeg command validation.
- [x] Verify progress output and timestamp selection on Windows.
- [ ] Test FFmpeg failure reporting, successful UI completion, and Explorer launch interactively.
- [x] Build the application.
- [ ] Add and run automated tests for the capture logic.
