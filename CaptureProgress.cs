namespace ScreenCapsFromVideo;

public sealed record CaptureProgress(TimeSpan? Duration, TimeSpan Position);

public sealed record CaptureResult(int ScreenCapCount, string OutputDirectory);
