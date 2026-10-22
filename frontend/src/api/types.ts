export type Role = 'FacultyAdmin' | 'DepartmentHead' | 'Advisor' | 'Student';

export interface User {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  role: Role;
  departmentId?: number | null;
  departmentName?: string | null;
  isActive: boolean;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: User;
}

export interface Department {
  id: number;
  name: string;
  code: string;
  isActive: boolean;
}

export interface Paged<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface StudentSummary {
  id: number;
  fullName: string;
  studentNumber: string;
  maskedNationalId: string;
  departmentId: number;
  departmentName: string;
  registrationYear: number;
  isActive: boolean;
  currentUnitName?: string | null;
  currentAdvisorName?: string | null;
}

export interface StudentDetail {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  studentNumber: string;
  departmentName: string;
  registrationYear: number;
  isActive: boolean;
  currentUnit?: { workUnitName: string } | null;
  currentAdvisor?: { advisorName: string } | null;
}

export interface Warning {
  code: string;
  message: string;
}

export interface LeaveBalance {
  year: number;
  limit: number;
  used: number;
  pending: number;
  remaining: number;
  absenceLimit: number;
  unexcusedAbsenceDays: number;
  absenceRemaining: number;
  warnings: Warning[];
}

export interface RegistrationRequest {
  fullName: string;
  email: string;
  password: string;
  phoneNumber: string;
  nationalId: string;
  iban: string;
  studentNumber: string;
  departmentId: number;
  registrationYear: number;
}

export interface LeaveAlert {
  studentId: number;
  studentName: string;
  departmentName: string;
  balance: LeaveBalance;
}
