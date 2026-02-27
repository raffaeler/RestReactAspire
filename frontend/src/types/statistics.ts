import type { Link } from './hateoas';

export interface AgeGroupItem {
  ageGroup: string;
  count: number;
}

export interface PatientsByAgeGroupResponse {
  items: AgeGroupItem[];
  links: Link[];
}

export interface ExamsPerDoctorItem {
  doctorName: string;
  specialty: string;
  examCount: number;
}

export interface ExamsPerDoctorResponse {
  items: ExamsPerDoctorItem[];
  links: Link[];
}

export interface ExamsOverTimeItem {
  month: string;
  examCount: number;
}

export interface ExamsOverTimeResponse {
  items: ExamsOverTimeItem[];
  links: Link[];
}

export interface AvgDurationByExamTypeItem {
  month: string;
  examType: string;
  avgDurationMinutes: number;
}

export interface AvgDurationByExamTypeResponse {
  items: AvgDurationByExamTypeItem[];
  links: Link[];
}
