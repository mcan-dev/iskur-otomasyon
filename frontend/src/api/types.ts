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
  leaveDays: number;
  absenceDays: number;
  warnings: Warning[];
}

export interface RegistrationRequest {
  fullName: string;
  email: string;
  password: string;
  phoneNumber: string;
  nationalId: string;
  iban?: string;
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

export type PlanStatus = 'Pending' | 'Approved' | 'Rejected';
export type CourseSession = 'FullDay' | 'Morning' | 'Afternoon';

export interface CourseDay {
  day: string;
  session: CourseSession;
}

export type CalendarColor = 'Green' | 'Red' | 'Yellow' | 'Gray';

export interface CalendarDay {
  date: string;
  color: CalendarColor;
  label: string;
  isSelectable: boolean;
  isSelected: boolean;
  isLessonDay: boolean;
  lessonSession?: CourseSession | null;
  isExamPeriod: boolean;
  note?: string | null;
}

export interface PlanCalendar {
  year: number;
  month: number;
  planStatus?: PlanStatus | null;
  canEdit: boolean;
  editDeadline: string;
  dailyHours: number;
  days: CalendarDay[];
}

export interface WorkPlanDay {
  date: string;
  isLessonDay: boolean;
  note?: string | null;
  hours: number;
}

export interface WorkPlan {
  id: number;
  studentId?: number;
  studentName?: string;
  departmentName?: string;
  year: number;
  month: number;
  status: PlanStatus;
  studentNote?: string | null;
  lastSavedAt: string;
  reviewedByName?: string | null;
  reviewedAt?: string | null;
  reviewNote?: string | null;
  plannedDays: number;
  plannedHours: number;
  days: WorkPlanDay[];
}

export type LeaveType = 'Excuse' | 'Health' | 'Other';
export type LeaveStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface Leave {
  id: number;
  studentId?: number;
  studentName?: string;
  departmentName?: string;
  type: LeaveType;
  status: LeaveStatus;
  startDate: string;
  endDate: string;
  hours?: number | null;
  daysCounted: number;
  reason: string;
  description?: string | null;
  appliedOn: string;
  reviewedByName?: string | null;
  reviewedAt?: string | null;
  reviewNote?: string | null;
  hasDocument: boolean;
  documentFileName?: string | null;
}

export interface LeaveResult {
  leave: Leave;
  balance: LeaveBalance;
}

export interface Absence {
  id: number;
  studentId?: number;
  studentName?: string;
  departmentName?: string;
  recordedByName?: string;
  revokeReason?: string | null;
  date: string;
  note?: string | null;
  isRevoked: boolean;
}

export interface Attendance {
  id: number;
  studentId?: number;
  studentName?: string;
  departmentName?: string;
  voidReason?: string | null;
  sessionId: number;
  date: string;
  sessionStart: string;
  sessionEnd: string;
  checkedInAt: string;
  isManual: boolean;
  manualReason?: string | null;
  isVoided: boolean;
}

export type PlannedDayOutcome = 'Attended' | 'OnLeave' | 'RecordedAbsence' | 'Missing' | 'Pending' | 'Upcoming';

export interface AttendanceComparison {
  year: number;
  month: number;
  hasApprovedPlan: boolean;
  plannedDays: number;
  attendedDays: number;
  leaveDays: number;
  recordedAbsenceDays: number;
  missingDays: number;
  workedHours: number;
  dailyHours: number;
  days: { date: string; outcome: PlannedDayOutcome }[];
}

export type TaskStatus = 'Open' | 'Completed' | 'NotCompleted';

export interface TaskEvaluation {
  id: number;
  authorName: string;
  note?: string | null;
  newStatus?: TaskStatus | null;
  createdAt: string;
}

export interface StudentTask {
  id: number;
  studentId?: number;
  studentName?: string;
  departmentName?: string;
  title: string;
  description?: string | null;
  assignedDate: string;
  dueDate: string;
  assignedByName: string;
  status: TaskStatus;
  isOverdue: boolean;
  evaluationCount: number;
  evaluations: TaskEvaluation[];
}

export interface StudentProfileFull extends StudentDetail {
  nationalId: string;
  iban: string;
  isPersonalDataMasked: boolean;
  participations: { id: number; startDate: string; endDate?: string | null; endReason?: string | null; isOpen: boolean }[];
  unitHistory: { id: number; workUnitName: string; startDate: string; endDate?: string | null }[];
  advisorHistory: { id: number; advisorName: string; startDate: string; endDate?: string | null }[];
}

export interface WorkUnit {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface Session {
  id: number;
  date: string;
  startTime: string;
  endTime: string;
  title?: string | null;
  departmentId?: number | null;
  departmentName?: string | null;
  createdByName: string;
  isClosed: boolean;
  isActiveNow: boolean;
}

export interface QrCode {
  sessionId: number;
  token: string;
  refreshAtUtc: string;
  windowSeconds: number;
}

export interface AbsenceNotice {
  id: number;
  studentId: number;
  studentName: string;
  departmentName: string;
  date: string;
  message: string;
  advisorName: string;
  createdAt: string;
  isRead: boolean;
  readAt?: string | null;
  readByName?: string | null;
}

export interface StudentDetailStaff extends StudentProfileFull {
  departmentId: number;
  leaveDayAllowance?: number | null;
  currentUnit?: { workUnitId: number; workUnitName: string } | null;
  currentAdvisor?: { advisorUserId: number; advisorName: string } | null;
}

export interface ReportTable {
  title: string;
  columns: string[];
  rows: (string | number | boolean | null)[][];
}

export interface WorkUnit {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
}

export type TerminationBasis = 'LeaveLimitExceeded' | 'StudentRequest' | 'Other';

export interface Termination {
  id: number;
  studentId: number;
  studentName: string;
  departmentName: string;
  basis: TerminationBasis;
  decisionDate: string;
  note: string;
  leaveDaysUsed: number;
  absenceDays: number;
  decidedByName: string;
  createdAt: string;
}

export interface Holiday {
  id: number;
  date: string;
  name: string;
}

export interface ExamPeriod {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
}

export interface LeavePolicy {
  leaveDayLimit: number;
  leaveWarningRemainingDays: number;
}

export interface AuditLog {
  id: number;
  occurredAt: string;
  userId?: number | null;
  userEmail?: string | null;
  action: string;
  entityName: string;
  entityId?: number | null;
  details?: string | null;
  ipAddress?: string | null;
}
