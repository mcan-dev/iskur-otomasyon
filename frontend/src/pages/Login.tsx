import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import type { Role } from '../api/types';
import { Icon } from '../components/Icon';
import { Alert, Field } from '../components/ui';
import AuthShell from '../layout/AuthShell';

/** Her rolün kendi giriş sayfası vardır; hesap rolü sayfayla eşleşmezse giriş yapılamaz. */
export const loginTypes: { slug: string; role: Role; title: string; text: string; glyph: string }[] = [
  { slug: 'ogrenci', role: 'Student', title: 'Öğrenci', text: 'Çalışma planı, izin, karekodla devam ve görevleriniz', glyph: 'user' },
  { slug: 'sorumlu-personel', role: 'Advisor', title: 'Sorumlu Personel', text: 'Sorumlu olduğunuz öğrencilerin devam, izin ve görev takibi', glyph: 'users' },
  { slug: 'birim-yetkilisi', role: 'DepartmentHead', title: 'Birim Yetkilisi', text: 'Biriminizdeki öğrenciler, sorumlu personel atama ve izleme', glyph: 'building' },
  { slug: 'fakulte', role: 'FacultyAdmin', title: 'Fakülte Sekreterliği / Dekanlık', text: 'Fakülte geneli yönetim, onaylar, raporlar ve tanımlar', glyph: 'settings' },
];

/** Giriş türü seçim sayfası. */
export function LoginChooser() {
  const { user } = useAuth();
  if (user) return <Navigate to="/" replace />;

  return (
    <AuthShell wide>
      <div className="auth-card chooser">
        <h1>Giriş Türünüzü Seçin</h1>
        <div className="chooser-grid">
          {loginTypes.map((t) => (
            <Link key={t.slug} to={`/giris/${t.slug}`} className="chooser-item">
              <span className="chooser-icon">
                <Icon name={t.glyph} size={24} />
              </span>
              <span className="chooser-text">
                <strong>{t.title}</strong>
              </span>
            </Link>
          ))}
        </div>
      </div>
    </AuthShell>
  );
}

/** Seçilen role ait giriş formu. */
export default function Login() {
  const { tur } = useParams();
  const type = loginTypes.find((t) => t.slug === tur);
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to="/" replace />;
  if (!type) return <Navigate to="/giris" replace />;
  const isStudent = type.role === 'Student';

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await login(username.trim(), password, type!.role);
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Giriş yapılamadı.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthShell>
      <form className="auth-card" onSubmit={submit}>
        <h1>{type.title} Girişi</h1>
        <p className="muted">Selçuk Üniversitesi Teknoloji Fakültesi İŞKUR Otomasyonu</p>
        {error && <Alert>{error}</Alert>}
        {isStudent ? (
          <>
            <Field label="Öğrenci e-postası">
              <input type="email" value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username"
                placeholder="ogrencino@ogr.selcuk.edu.tr" required />
            </Field>
            <Field label="OBİS şifresi">
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" required />
            </Field>
            <p className="small muted">OBİS şifrenizi unuttuysanız OBİS üzerinden ya da bilgi işlemden yenileyin.</p>
          </>
        ) : (
          <>
            <Field label="Sicil numarası">
              <input value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" inputMode="numeric" required />
            </Field>
            <Field label="Şifre">
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" required />
            </Field>
            <p className="small right">
              <Link to="/parolami-unuttum">Şifremi unuttum</Link>
            </p>
          </>
        )}
        <button className="btn primary wide" disabled={busy}>
          {busy ? 'Giriş yapılıyor…' : 'Giriş Yap'}
        </button>
        <p className="muted center small">
          <Link to="/giris">‹ Başka bir giriş türü seç</Link>
        </p>
      </form>
    </AuthShell>
  );
}
