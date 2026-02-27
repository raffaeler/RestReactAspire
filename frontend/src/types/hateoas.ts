export interface Link {
  rel: string;
  href: string;
  method: string;
}

export interface PaginationInfo {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiRoot {
  links: Link[];
}
