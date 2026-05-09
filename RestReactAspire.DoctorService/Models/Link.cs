namespace RestReactAspire.DoctorService.Models;

public record Link(string Rel, string Href, string Method);

public record PaginationInfo(int Page, int PageSize, int TotalCount, int TotalPages);

public record SortInfo(string SortBy, string SortDirection);

public static class PaginationLinks
{
    public static List<Link> Build(string basePath, int page, int pageSize, int totalPages, params Link[] additionalLinks)
        => Build(basePath, page, pageSize, totalPages, search: null, sortBy: null, sortDirection: null, additionalLinks);

    public static List<Link> Build(string basePath, int page, int pageSize, int totalPages, string? search, params Link[] additionalLinks)
        => Build(basePath, page, pageSize, totalPages, search, sortBy: null, sortDirection: null, additionalLinks);

    public static List<Link> Build(string basePath, int page, int pageSize, int totalPages, string? search, string? sortBy, string? sortDirection, params Link[] additionalLinks)
    {
        var searchParam = string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search)}";
        var sortParams = "";
        if (!string.IsNullOrWhiteSpace(sortBy))
            sortParams += $"&sortBy={Uri.EscapeDataString(sortBy)}";
        if (!string.IsNullOrWhiteSpace(sortDirection))
            sortParams += $"&sortDirection={Uri.EscapeDataString(sortDirection)}";

        var links = new List<Link>
        {
            new Link("self", $"{basePath}?page={page}&pageSize={pageSize}{searchParam}{sortParams}", "GET"),
            new Link("first", $"{basePath}?page=1&pageSize={pageSize}{searchParam}{sortParams}", "GET"),
            new Link("last", $"{basePath}?page={Math.Max(1, totalPages)}&pageSize={pageSize}{searchParam}{sortParams}", "GET"),
        };

        if (page > 1)
            links.Add(new Link("prev", $"{basePath}?page={page - 1}&pageSize={pageSize}{searchParam}{sortParams}", "GET"));
        if (page < totalPages)
            links.Add(new Link("next", $"{basePath}?page={page + 1}&pageSize={pageSize}{searchParam}{sortParams}", "GET"));

        links.AddRange(additionalLinks);

        return links;
    }
}
