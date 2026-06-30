namespace LoadTests;

internal static class PerformanceSla
{
    public const double MaxP95Milliseconds = 200;
    public const double MaxP99Milliseconds = 500;
    public const double MaxErrorPercentage = 1;
    public const double MinReadRequestsPerSecond = 20;
    public const double MinWriteRequestsPerSecond = 1;
}
