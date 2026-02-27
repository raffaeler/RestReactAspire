namespace RestReactAspire.Server.Models;

public record Link(string Rel, string Href, string Method);

public record PaginationInfo(int Page, int PageSize, int TotalCount, int TotalPages);

public static class PaginationLinks
{
    public static List<Link> Build(string basePath, int page, int pageSize, int totalPages, params Link[] additionalLinks)
    {
        var links = new List<Link>
        {
            new Link("self", $"{basePath}?page={page}&pageSize={pageSize}", "GET"),
            new Link("first", $"{basePath}?page=1&pageSize={pageSize}", "GET"),
            new Link("last", $"{basePath}?page={Math.Max(1, totalPages)}&pageSize={pageSize}", "GET"),
        };

        if (page > 1)
            links.Add(new Link("prev", $"{basePath}?page={page - 1}&pageSize={pageSize}", "GET"));
        if (page < totalPages)
            links.Add(new Link("next", $"{basePath}?page={page + 1}&pageSize={pageSize}", "GET"));

        links.AddRange(additionalLinks);

        return links;
    }
}
