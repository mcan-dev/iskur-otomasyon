import type { AbsenceType, LeaveStatus, LeaveType, PlanStatus, PlannedDayOutcome, TaskStatus, TerminationBasis } from './api/types';

const months = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
export const monthName = (m: number) => months[m - 1] ?? '';
export const monthYear = (y: number, m: number) => `${monthName(m)} ${y}`;

/** "2026-10-21" -> "21.10.2026" (saat dilimi kaymasını önlemek için metin üzerinden çevrilir). */
export function fmtDate(iso?: string | null): string {
  if (!iso) return '';
  const [y, m, d] = iso.slice(0, 10).split('-');
  return `${d}.${m}.${y}`;
}

export function fmtDateTime(iso?: string | null): string {
  if (!iso) return '';
  const date = new Date(iso.endsWith('Z') || iso.includes('+') ? iso : `${iso}Z`);
  return date.toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' });
}

export const fmtTime = (t: string) => t.slice(0, 5);

/** Yerel takvim gününü "yyyy-MM-dd" yapar. */
export function isoDate(d: Date): string {
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mm}-${dd}`;
}

export const todayIso = () => isoDate(new Date());

export const planStatusLabels: Record<PlanStatus, string> = { Pending: 'Onay bekliyor', Approved: 'Onaylandı', Rejected: 'Reddedildi' };
export const leaveTypeLabels: Record<LeaveType, string> = { Excuse: 'Mazeret', Health: 'Sağlık', Other: 'Diğer' };
export const leaveStatusLabels: Record<LeaveStatus, string> = { Pending: 'Onay bekliyor', Approved: 'Onaylandı', Rejected: 'Reddedildi', Cancelled: 'İptal edildi' };
export const absenceTypeLabels: Record<AbsenceType, string> = { Unexcused: 'Mazeretsiz', HealthReport: 'Sağlık raporlu', Excused: 'Mazeretli' };
export const taskStatusLabels: Record<TaskStatus, string> = { Open: 'Açık', Completed: 'Tamamlandı', NotCompleted: 'Tamamlanmadı' };
export const outcomeLabels: Record<PlannedDayOutcome, string> = {
  Attended: 'Geldi',
  OnLeave: 'İzinli',
  RecordedAbsence: 'Devamsız',
  Missing: 'Kayıt yok',
  Pending: 'Bekliyor',
  Upcoming: 'Yaklaşan',
};

export const weekdays = [
  { value: 'Monday', label: 'Pazartesi' },
  { value: 'Tuesday', label: 'Salı' },
  { value: 'Wednesday', label: 'Çarşamba' },
  { value: 'Thursday', label: 'Perşembe' },
  { value: 'Friday', label: 'Cuma' },
  { value: 'Saturday', label: 'Cumartesi' },
  { value: 'Sunday', label: 'Pazar' },
] as const;

type Tone = 'green' | 'amber' | 'red' | 'gray' | 'blue';
export const planTone: Record<PlanStatus, Tone> = { Pending: 'amber', Approved: 'green', Rejected: 'red' };
export const leaveTone: Record<LeaveStatus, Tone> = { Pending: 'amber', Approved: 'green', Rejected: 'red', Cancelled: 'gray' };
export const taskTone: Record<TaskStatus, Tone> = { Open: 'blue', Completed: 'green', NotCompleted: 'red' };
export const outcomeTone: Record<PlannedDayOutcome, Tone> = {
  Attended: 'green', OnLeave: 'blue', RecordedAbsence: 'red', Missing: 'amber', Pending: 'gray', Upcoming: 'gray',
};

export const terminationBasisLabels: Record<TerminationBasis, string> = {
  LeaveLimitExceeded: 'İzin hakkının aşılması',
  AbsenceLimitExceeded: 'Devamsızlık sınırının aşılması',
  StudentRequest: 'Öğrencinin talebi',
  Other: 'Diğer',
};

export const reportKinds: { id: string; label: string; needsMonth?: boolean; facultyOnly?: boolean; noAdvisor?: boolean }[] = [
  { id: 'MonthlyWork', label: 'Aylık çalışma (planlanan / gerçekleşen)', needsMonth: true },
  { id: 'YearlyWork', label: 'Yıllık çalışma saati' },
  { id: 'LeaveAndAbsence', label: 'İzin ve devamsızlık' },
  { id: 'Advisor', label: 'Danışman özeti' },
  { id: 'StudentList', label: 'Öğrenci listesi (aktif / pasif)' },
  { id: 'NearLeaveLimit', label: 'İzin sınırına yaklaşanlar' },
  { id: 'TerminationReview', label: 'İlişik kesme incelemesi', noAdvisor: true },
  { id: 'MonthlyStatement', label: 'Ay sonu dökümü (TC ve IBAN içerir)', needsMonth: true, facultyOnly: true },
];
