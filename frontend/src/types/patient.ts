import type { Link, PaginationInfo } from './hateoas';

export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string;
  phone: string;
  links: Link[];
}

export interface PatientList {
  items: Patient[];
  pagination: PaginationInfo;
  links: Link[];
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string;
  phone: string;
}

export interface UpdatePatientRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string;
  phone: string;
}
