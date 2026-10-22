import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError, firstError } from '../api/client';
import type { Department, RegistrationRequest } from '../api/types';
import { Alert, Field } from '../components/ui';

const empty: RegistrationRequest = {
  fullName: '',
  email: '',
  password: '',
  phoneNumber: '',
  nationalId: '',
  iban: '',
  studentNumber: '',
  departmentId: 0,
  registrationYear: new Date().getFullYear(),
};

export default function Register() {
  const [form, setForm] = useState<RegistrationRequest>(empty);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [error, setError] = useState<ApiError | null>(null);
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    api.get<Department[]>('/api/departments').then(setDepartments).catch(() => setDepartments([]));
  }, []);

  const set = <K extends keyof RegistrationRequest>(key: K, value: RegistrationRequest[K]) => setForm((f) => ({ ...f, [key]: value }));
  const fieldError = (name: string) => firstError(error, name);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await api.post('/api/auth/register-student', { ...form, fullName: form.fullName.trim(), email: form.email.trim() });
      setDone(true);
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Kayıt tamamlanamadı.'));
    } finally {
      setBusy(false);
    }
  }

  if (done) {
    return (
      <div className="auth-page">
        <div className="auth-card">
          <h1>Kaydınız alındı</h1>
          <p className="muted">Hesabınız oluşturuldu. E-posta ve parolanızla giriş yapabilirsiniz.</p>
          <Link className="btn primary wide" to="/giris/ogrenci">
            Giriş sayfasına git
          </Link>
        </div>
      </div>
    );
  }

  const hasFieldErrors = error && Object.keys(error.fieldErrors).length > 0;

  return (
    <div className="auth-page">
      <form className="auth-card wide-card" onSubmit={submit}>
        <h1>Öğrenci Kaydı</h1>
        <p className="muted">İŞKUR programı için temel bilgilerinizi girin.</p>
        {error && <Alert>{hasFieldErrors ? 'Lütfen işaretli alanları düzeltin.' : error.message}</Alert>}
        <div className="grid-2">
          <Field label="Ad Soyad" error={fieldError('fullName')}>
            <input value={form.fullName} onChange={(e) => set('fullName', e.target.value)} required />
          </Field>
          <Field label="E-posta" error={fieldError('email')}>
            <input type="email" value={form.email} onChange={(e) => set('email', e.target.value)} required />
          </Field>
          <Field label="T.C. Kimlik No" error={fieldError('nationalId')} hint="11 haneli kimlik numaranız">
            <input value={form.nationalId} onChange={(e) => set('nationalId', e.target.value.replace(/\D/g, '').slice(0, 11))} inputMode="numeric" required />
          </Field>
          <Field label="Öğrenci Numarası" error={fieldError('studentNumber')}>
            <input value={form.studentNumber} onChange={(e) => set('studentNumber', e.target.value)} required />
          </Field>
          <Field label="Bölüm" error={fieldError('departmentId')}>
            <select value={form.departmentId} onChange={(e) => set('departmentId', Number(e.target.value))} required>
              <option value={0}>Bölüm seçin</option>
              {departments.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Kayıt Yılı" error={fieldError('registrationYear')}>
            <input type="number" value={form.registrationYear} onChange={(e) => set('registrationYear', Number(e.target.value))} required />
          </Field>
          <Field label="Cep Telefonu" error={fieldError('phoneNumber')} hint="Örn: 0532 123 45 67">
            <input value={form.phoneNumber} onChange={(e) => set('phoneNumber', e.target.value)} required />
          </Field>
          <Field label="IBAN" error={fieldError('iban')} hint="TR ile başlayan 26 karakter">
            <input value={form.iban} onChange={(e) => set('iban', e.target.value.toUpperCase())} required />
          </Field>
          <Field label="Parola" error={fieldError('password')}>
            <input type="password" value={form.password} onChange={(e) => set('password', e.target.value)} autoComplete="new-password" required />
          </Field>
        </div>
        <button className="btn primary wide" disabled={busy}>
          {busy ? 'Kaydediliyor…' : 'Kaydol'}
        </button>
        <p className="muted center">
          Zaten hesabınız var mı? <Link to="/giris/ogrenci">Giriş yapın</Link>
        </p>
      </form>
    </div>
  );
}
