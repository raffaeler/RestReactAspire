import type { Link, PaginationInfo, SortInfo } from './hateoas';

export interface Exam {
  id: string;
  patientId: string;
  doctorId: string | null;
  type: string;
  scheduledDate: string;
  status: string;
  results: string | null;
  notes: string | null;
  links: Link[];
}

export interface ExamList {
  items: Exam[];
  pagination: PaginationInfo;
  sort: SortInfo;
  links: Link[];
}

export interface CreateExamRequest {
  patientId: string;
  doctorId: string | null;
  type: string;
  scheduledDate: string;
  status: string;
  results: string | null;
  notes: string | null;
}

export interface UpdateExamRequest {
  doctorId: string | null;
  type: string;
  scheduledDate: string;
  status: string;
  results: string | null;
  notes: string | null;
}

export interface AssignDoctorRequest {
  doctorId: string | null;
}
