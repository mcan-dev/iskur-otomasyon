import { useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api, ApiError, firstError } from '../api/client';
import { Alert, Field } from '../components/ui';
import AuthShell from '../layout/AuthShell';

export default function ResetPassword() {
  const [params] = useSearchParams();
  const token = params.get('token') ?? '';
  const [password, setPassword] = useState('');
  const [again, setAgain] = useState('');
  const [error, setError] = useState<ApiError | null>(null);
  const [mismatch, setMismatch] = useState(false);
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setMismatch(password !== again);
    if (password !== again) return;
    setBusy(true);
    try {
      await api.post('/api/auth/reset-password', { token, newPassword: password });
      setDone(true);
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Parola değiştirilemedi.'));
    } finally {
      setBusy(false);
    }
  }

  if (done) {
    return (
      <AuthShell>
        <div className="auth-card">
          <h1>Parolanız değişti</h1>
          <p className="muted">Yeni parolanızla giriş yapabilirsiniz.</p>
          <Link className="btn primary wide" to="/giris">
            Giriş yap
          </Link>
        </div>
      </AuthShell>
    );
  }

  if (!token) {
    return (
      <AuthShell>
        <div className="auth-card">
          <h1>Bağlantı eksik</h1>
          <p className="muted">Bu sayfa e-postadaki bağlantıyla açılmalıdır.</p>
          <Link className="btn primary wide" to="/parolami-unuttum">
            Yeni bağlantı iste
          </Link>
        </div>
      </AuthShell>
    );
  }

  const tokenError = firstError(error, 'token');
  const passwordError = firstError(error, 'newPassword');
  const generalError = error && !tokenError && !passwordError ? error.message : undefined;

  return (
    <AuthShell>
      <form className="auth-card" onSubmit={submit}>
        <h1>Yeni parola belirle</h1>
        {tokenError && (
          <Alert>
            {tokenError} <Link to="/parolami-unuttum">Yeni bağlantı iste</Link>
          </Alert>
        )}
        {generalError && <Alert>{generalError}</Alert>}
        <Field label="Yeni parola" error={passwordError}>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" required />
        </Field>
        <Field label="Yeni parola (tekrar)" error={mismatch ? 'Parolalar aynı değil.' : undefined}>
          <input type="password" value={again} onChange={(e) => setAgain(e.target.value)} autoComplete="new-password" required />
        </Field>
        <button className="btn primary wide" disabled={busy}>
          {busy ? 'Kaydediliyor…' : 'Parolayı Değiştir'}
        </button>
      </form>
    </AuthShell>
  );
}
