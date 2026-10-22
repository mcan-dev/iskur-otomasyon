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
  { slug: 'danisman', role: 'Advisor', title: 'Danışman Öğretim Elemanı', text: 'Öğrencilerinizin devam, izin ve görev takibi', glyph: 'users' },
  { slug: 'bolum-baskani', role: 'DepartmentHead', title: 'Bölüm Başkanı', text: 'Bölümünüzdeki öğrenciler, danışman atama ve raporlar', glyph: 'building' },
  { slug: 'fakulte', role: 'FacultyAdmin', title: 'Fakülte Sekreterliği / Dekanlık', text: 'Fakülte geneli yönetim, onaylar, raporlar ve tanımlar', glyph: 'settings' },
];

/** Giriş türü seçim sayfası. */
export function LoginChooser() {
  const { user } = useAuth();
  if (user) return <Navigate to="/" replace />;

  return (
    <AuthShell side={false} wide>
      <div className="auth-card chooser">
        <h1>Giriş Türünüzü Seçin</h1>
        <p className="muted">İŞKUR Otomasyonu, Selçuk Üniversitesi Teknoloji Fakültesi. Hesabınızın türüne uygun girişi kullanın.</p>
        <div className="chooser-grid">
          {loginTypes.map((t) => (
            <Link key={t.slug} to={`/giris/${t.slug}`} className="chooser-item">
              <span className="chooser-icon">
                <Icon name={t.glyph} size={24} />
              </span>
              <span className="chooser-text">
                <strong>{t.title}</strong>
                <span className="muted small">{t.text}</span>
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
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to="/" replace />;
  if (!type) return <Navigate to="/giris" replace />;

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await login(email.trim(), password, type!.role);
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
        <Field label="E-posta">
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="username" required />
        </Field>
        <Field label="Parola">
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" required />
        </Field>
        <p className="small right">
          <Link to="/parolami-unuttum">Parolamı unuttum</Link>
        </p>
        <button className="btn primary wide" disabled={busy}>
          {busy ? 'Giriş yapılıyor…' : 'Giriş Yap'}
        </button>
        {type.role === 'Student' && (
          <p className="muted center">
            Henüz kaydınız yok mu? <Link to="/kayit">Kayıt olun</Link>
          </p>
        )}
        <p className="muted center small">
          <Link to="/giris">‹ Başka bir giriş türü seç</Link>
        </p>
      </form>
    </AuthShell>
  );
}
