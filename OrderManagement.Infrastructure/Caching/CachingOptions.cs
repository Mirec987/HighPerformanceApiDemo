namespace OrderManagement.Infrastructure.Caching;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    public bool Enabled { get; set; } = true;

    public int OrderDetailSeconds { get; set; } = 60;

    public int OrderListSeconds { get; set; } = 20;
}
