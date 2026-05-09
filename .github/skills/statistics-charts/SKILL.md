---
name: Statistics and Charts
description: Add or modify statistics API endpoints and frontend chart visualizations using recharts.
globs:
  - "RestReactAspire.StatisticsService/Endpoints/StatisticsEndpoints.cs"
  - "**/Models/StatisticsDto.cs"
  - "RestReactAspire.StatisticsService/Telemetry/StatisticsTelemetry.cs"
  - "frontend/src/pages/StatisticsPage.tsx"
  - "frontend/src/types/statistics.ts"
---

# Statistics and Charts

## Backend Statistics Endpoints
Located in `RestReactAspire.StatisticsService/Endpoints/StatisticsEndpoints.cs`, registered under `/api/statistics` (routed via YARP gateway).

### Existing Endpoints
| Endpoint | Description |
|----------|-------------|
| `GET /patients-by-age-group` | Pie chart data: patient distribution by age bracket |
| `GET /exams-per-doctor` | Bar chart data: exam count per doctor |
| `GET /exams-over-time` | Line chart data: monthly exam counts |
| `GET /avg-duration-by-exam-type` | Line chart data: average duration per exam type per month |

### Response Pattern
```csharp
public record {StatName}Response(
    IReadOnlyList<{StatName}Item> Items,
    IReadOnlyList<Link> Links);
```
- Each response includes HATEOAS links to all statistics endpoints plus main resource lists (via gateway URLs).
- DTOs in each service's own `Models/` directory (e.g., `StatisticsService.Models.StatisticsDto`).

### Adding a New Statistic
1. Add DTO records to the service's `Models/` directory (e.g., `StatisticsService/Models/StatisticsDto.cs`).
2. Add endpoint method in `StatisticsService/Endpoints/StatisticsEndpoints.cs`.
3. Add telemetry counter in `StatisticsService/Telemetry/StatisticsTelemetry.cs`.
4. Register the link in the gateway root endpoint and in `GetStatisticsLinks()`.
5. Add frontend type in `frontend/src/types/statistics.ts`.
6. Add chart component in `StatisticsPage.tsx`.

## Frontend Charts
- Uses **recharts v3** (`https://recharts.org/en-US/`).
- Each chart is wrapped in a MUI `Paper` component with a title.
- Chart components used: `PieChart`, `BarChart`, `LineChart`, `ResponsiveContainer`.
- Colors use `#8884d8`, `#82ca9d`, and similar palette constants.
- Data is fetched via `apiClient.get<T>()` using HATEOAS link discovery.

## TypeScript Types
In `frontend/src/types/statistics.ts`, mirror the backend DTOs:
```typescript
export interface {StatName}Item { /* fields */ }
export interface {StatName}Response {
  items: {StatName}Item[];
  links: Link[];
}
```
