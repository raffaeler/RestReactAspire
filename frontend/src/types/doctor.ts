import type { Link, PaginationInfo, SortInfo } from './hateoas';

export interface Doctor {
  id: string;
  firstName: string;
  lastName: string;
  specialty: string;
  email: string;
  phone: string;
  links: Link[];
}

export interface DoctorList {
  items: Doctor[];
  pagination: PaginationInfo;
  sort: SortInfo;
  links: Link[];
}

export interface CreateDoctorRequest {
  firstName: string;
  lastName: string;
  specialty: string;
  email: string;
  phone: string;
}

export interface UpdateDoctorRequest {
  firstName: string;
  lastName: string;
  specialty: string;
  email: string;
  phone: string;
}
