using OrderManagement.Application.Orders.DTOs;

namespace OrderManagement.Application.Orders.Caching;

public static class OrderCacheKeys
{
    public const string ListVersion = "orders:list:version";

    public static string Detail(Guid id) => $"orders:detail:{id:N}";

    public static string List(GetOrdersRequest request, long version)
    {
        var customer = request.CustomerId?.ToString("N") ?? "all";
        var status = Normalize(request.Status, "all");
        var sortBy = Normalize(request.SortBy, "createdat");
        var direction = Normalize(request.SortDirection, "desc");

        return $"orders:list:v{version}:p{request.Page}:s{request.PageSize}:c{customer}:st{status}:o{sortBy}:{direction}";
    }

    private static string Normalize(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
}
