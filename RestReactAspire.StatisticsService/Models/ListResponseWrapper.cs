namespace RestReactAspire.StatisticsService.Models;

/// <summary>
/// Represents the HATEOAS-wrapped list response shape returned by all microservices:
/// { items: [...], pagination: {...}, sort: {...}, links: [...] }
/// Used by the StatisticsService when deserializing list responses from other services.
/// </summary>
internal record ListResponseWrapper<T>(IReadOnlyList<T> Items);
