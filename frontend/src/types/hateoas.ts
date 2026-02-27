export interface Link {
  rel: string;
  href: string;
  method: string;
}

export interface ApiRoot {
  links: Link[];
}
