---
name: HATEOAS REST Design
description: Ensure all APIs follow HATEOAS REST principles with proper link relations, HTTP methods, and status codes.
globs:
  - "**/Endpoints/**"
  - "**/Models/Link.cs"
  - "frontend/src/api/apiClient.ts"
  - "frontend/src/types/hateoas.ts"
---

# HATEOAS REST Design

## Principles
This project strictly follows **HATEOAS (Hypermedia as the Engine of Application State)** REST architecture:
- Clients discover available actions through links embedded in API responses.
- No URL is hard-coded on the client side (except the initial `GET /api` entry point).
- Every response includes navigational `links` describing what the client can do next.
- **All HATEOAS links point to the gateway URL** (e.g., `http://localhost:5000/api/patients`). The gateway routes to the appropriate microservice transparently.
- Microservices generate links using the gateway's base URL, ensuring the client never needs to know about internal service addresses.

## Link Structure
```csharp
public record Link(string Rel, string Href, string Method);
```
- `Rel`: Relation name (e.g., `self`, `update`, `delete`, `collection`, `next`, `prev`).
- `Href`: The URL to follow.
- `Method`: HTTP method to use (`GET`, `POST`, `PUT`, `DELETE`).

## API Root (`GET /api`)
- Entry point for API discovery, served by the gateway.
- Returns all available top-level link relations pointing to gateway URLs.
- When adding a new microservice feature, register its link relations in the gateway's root endpoint.

## HTTP Methods & Status Codes
| Operation | Method | Success Code | Notes |
|-----------|--------|-------------|-------|
| List      | GET    | 200 OK      | Includes pagination links |
| Get by ID | GET    | 200 OK / 404 Not Found | |
| Create    | POST   | 201 Created | `Location` header set via `Results.Created()` |
| Update    | PUT    | 200 OK / 404 Not Found | |
| Delete    | DELETE | 204 No Content / 404 Not Found | |

## Pagination Links
Use `PaginationLinks.Build()` for list responses:
- Always includes: `self`, `first`, `last`
- Conditionally includes: `prev` (if page > 1), `next` (if page < totalPages)
- Preserves `search`, `sortBy`, `sortDirection` query parameters in links.

## Single Item Links
Include at minimum:
- `self` — GET the item
- `update` — PUT to modify
- `delete` — DELETE the item
- `collection` — GET back to the list
- Related resources (e.g., `exams` for a patient)

## Frontend HATEOAS Client
- `apiClient.discoverApi()` fetches and caches root links.
- `apiClient.getLink(rel)` resolves a relation from the root.
- `apiClient.findLink(links, rel)` resolves a relation from any response's links.
- Pages follow links from previous responses to navigate the API.
