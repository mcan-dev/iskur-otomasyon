import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import { Alert, Field } from '../components/ui';
import AuthShell from '../layout/AuthShell';

export default function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await api.post('/api/auth/forgot-password', { email: email.trim() });
      setSent(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'İstek gönderilemedi.');
    } finally {
      setBusy(false);
    }
  }

  if (sent) {
    return (
      <AuthShell>
        <div className="auth-card">
          <h1>E-postanızı kontrol edin</h1>
          <p className="muted">
            Bu adres sistemde kayıtlıysa parola sıfırlama bağlantısı gönderildi. Bağlantı 30 dakika geçerlidir ve tek kez kullanılabilir.
          </p>
          <Link className="btn primary wide" to="/giris">
            Giriş sayfasına dön
          </Link>
        </div>
      </AuthShell>
    );
  }

  return (
    <AuthShell>
      <form className="auth-card" onSubmit={submit}>
        <h1>Parolamı unuttum</h1>
        <p className="muted">Hesabınızın e-posta adresini girin, size bir sıfırlama bağlantısı gönderelim.</p>
        {error && <Alert>{error}</Alert>}
        <Field label="E-posta">
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="username" required />
        </Field>
        <button className="btn primary wide" disabled={busy}>
          {busy ? 'Gönderiliyor…' : 'Bağlantı Gönder'}
        </button>
        <p className="muted center">
          <Link to="/giris">Giriş sayfasına dön</Link>
        </p>
      </form>
    </AuthShell>
  );
}
