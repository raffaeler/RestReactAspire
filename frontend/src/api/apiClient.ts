import type { Link, ApiRoot } from '../types/hateoas';

class ApiClient {
  private rootLinks: Link[] | null = null;

  findLink(links: Link[], rel: string): Link | undefined {
    return links.find(l => l.rel === rel);
  }

  async discoverApi(): Promise<Link[]> {
    if (this.rootLinks) return this.rootLinks;

    const response = await fetch('/api');
    if (!response.ok) throw new Error(`Failed to discover API: ${response.status}`);

    const root: ApiRoot = await response.json();
    this.rootLinks = root.links;
    return this.rootLinks;
  }

  async getLink(rel: string): Promise<Link> {
    const links = await this.discoverApi();
    const link = this.findLink(links, rel);
    if (!link) throw new Error(`Link relation '${rel}' not found in API root`);
    return link;
  }

  async get<T>(href: string): Promise<T> {
    const response = await fetch(href);
    if (!response.ok) throw new Error(`Request failed: ${response.status}`);
    return response.json();
  }

  async post<T>(href: string, body: unknown): Promise<T> {
    const response = await fetch(href, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!response.ok) throw new Error(`Request failed: ${response.status}`);
    return response.json();
  }

  async put<T>(href: string, body: unknown): Promise<T> {
    const response = await fetch(href, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!response.ok) throw new Error(`Request failed: ${response.status}`);
    return response.json();
  }

  async delete(href: string): Promise<void> {
    const response = await fetch(href, { method: 'DELETE' });
    if (!response.ok) throw new Error(`Request failed: ${response.status}`);
  }
}

export const apiClient = new ApiClient();
