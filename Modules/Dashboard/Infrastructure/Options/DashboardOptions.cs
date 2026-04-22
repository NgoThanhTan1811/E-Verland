namespace Modules.Dashboard.Infrastructure.Options;

public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    public int RefreshIntervalMinutes { get; set; } = 15;
    public int SnapshotTtlMinutes { get; set; } = 30;
    public int StaleAfterMinutes { get; set; } = 15;
}
