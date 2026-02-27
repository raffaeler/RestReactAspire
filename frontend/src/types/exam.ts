import type { Link } from './hateoas';

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
