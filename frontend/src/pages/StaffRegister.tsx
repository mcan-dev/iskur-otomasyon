import { useEffect, useState, type FormEvent } from 'react';
import { Link, Navigate, useParams } from 'react-router-dom';
import { api, ApiError, firstError } from '../api/client';
import type { Department } from '../api/types';
import { Alert, Field, PhoneInput } from '../components/ui';
import AuthShell from '../layout/AuthShell';
import { loginTypes } from './Login';

/** Danışman, bölüm başkanı ve fakülte sekreterliği başvurusu. Hesap onaydan sonra açılır. */
export default function StaffRegister() {
  const { tur } = useParams();
  const type = loginTypes.find((t) => t.slug === tur && t.role !== 'Student');
  const [departments, setDepartments] = useState<Department[]>([]);
  const [form, setForm] = useState({ fullName: '', email: '', phoneNumber: '', password: '', departmentId: '' });
  const [error, setError] = useState<ApiError | null>(null);
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    api.get<Department[]>('/api/departments').then(setDepartments).catch(() => setDepartments([]));
  }, []);

  if (!type) return <Navigate to="/giris" replace />;
  const needsDepartment = type.role !== 'FacultyAdmin';
  const set = (key: keyof typeof form, value: string) => setForm((f) => ({ ...f, [key]: value }));
  const fieldError = (name: string) => firstError(error, name);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await api.post('/api/auth/register-staff', {
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phoneNumber: form.phoneNumber,
        password: form.password,
        role: type!.role,
        departmentId: needsDepartment ? Number(form.departmentId) || null : null,
      });
      setDone(true);
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Başvuru tamamlanamadı.'));
    } finally {
      setBusy(false);
    }
  }

  if (done) {
    return (
      <AuthShell>
        <div className="auth-card">
          <h1>Başvurunuz alındı</h1>
          <p className="muted">Hesabınız fakülte sekreterliği onayladıktan sonra aktif olur. Onaydan sonra e-posta ve parolanızla giriş yapabilirsiniz.</p>
          <Link className="btn primary wide" to={`/giris/${type.slug}`}>
            Giriş sayfasına dön
          </Link>
        </div>
      </AuthShell>
    );
  }

  return (
    <AuthShell>
      <form className="auth-card" onSubmit={submit}>
        <h1>{type.title} Kaydı</h1>
        <p className="muted">Bilgilerinizi girin. Hesabınız onaylandıktan sonra giriş yapabilirsiniz.</p>
        {error && <Alert>{Object.keys(error.fieldErrors).length > 0 ? 'Lütfen işaretli alanları düzeltin.' : error.message}</Alert>}
        <Field label="Ad Soyad" error={fieldError('fullName')}>
          <input value={form.fullName} onChange={(e) => set('fullName', e.target.value)} required />
        </Field>
        <Field label="E-posta" error={fieldError('email')}>
          <input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} required />
        </Field>
        <Field label="Cep Telefonu" error={fieldError('phoneNumber')} hint="11 hane, örn. 05321234567">
          <PhoneInput value={form.phoneNumber} onChange={(v) => set('phoneNumber', v)} required />
        </Field>
        {needsDepartment && (
          <Field label="Bölüm" error={fieldError('departmentId')}>
            <select value={form.departmentId} onChange={(e) => set('departmentId', e.target.value)} required>
              <option value="">Seçiniz</option>
              {departments.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.name}
                </option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Parola" error={fieldError('password')} hint="En az 8 karakter, harf ve rakam içermelidir">
          <input type="password" value={form.password} onChange={(e) => set('password', e.target.value)} autoComplete="new-password" required />
        </Field>
        <button className="btn primary wide" disabled={busy}>
          {busy ? 'Gönderiliyor…' : 'Başvur'}
        </button>
        <p className="muted center small">
          <Link to={`/giris/${type.slug}`}>‹ Girişe dön</Link>
        </p>
      </form>
    </AuthShell>
  );
}
