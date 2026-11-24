import { useEffect, useState, type FormEvent } from 'react';
import { api, ApiError, firstError } from '../../api/client';
import type { StudentProfileFull } from '../../api/types';
import { Alert, Badge, Card, Field, PhoneInput } from '../../components/ui';
import { fmtDate } from '../../format';
import { useLoad } from '../../useLoad';

export default function ProfilePage() {
  const profile = useLoad(() => api.get<StudentProfileFull>('/api/students/me'), []);
  const p = profile.data;

  return (
    <div className="two-col">
      <div className="stack">
        <Card title="Kişisel bilgilerim">
          {profile.error && <Alert>{profile.error}</Alert>}
          {p && (
            <dl className="kv">
              <dt>Ad soyad</dt><dd>{p.fullName}</dd>
              <dt>E-posta</dt><dd>{p.email}</dd>
              <dt>T.C. kimlik no</dt><dd>{p.nationalId}</dd>
              <dt>Öğrenci numarası</dt><dd>{p.studentNumber}</dd>
              <dt>Bölüm</dt><dd>{p.departmentName}</dd>
              <dt>Kayıt yılı</dt><dd>{p.registrationYear}</dd>
              <dt>Durum</dt><dd><Badge tone={p.isActive ? 'green' : 'gray'}>{p.isActive ? 'Aktif' : 'Pasif'}</Badge></dd>
              <dt>Çalıştığı birim</dt><dd>{p.currentUnit?.workUnitName ?? 'Henüz atanmadı'}</dd>
              <dt>Sorumlu personel</dt><dd>{p.currentAdvisor?.advisorName ?? 'Henüz atanmadı'}</dd>
            </dl>
          )}
        </Card>
        {p && <EditContact profile={p} onSaved={profile.reload} />}
        {p && (
          <Card title="Geçmiş" subtitle="Programa katılım, birim ve sorumlu personel geçmişiniz.">
            <h3 className="sub">Programa katılım</h3>
            <ul className="plain">
              {p.participations.map((x) => (
                <li key={x.id}>
                  {fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}
                  {x.endReason && <span className="muted"> ({x.endReason})</span>}
                </li>
              ))}
            </ul>
            <h3 className="sub">Çalışma birimi</h3>
            <ul className="plain">
              {p.unitHistory.length === 0 && <li className="muted">Kayıt yok</li>}
              {p.unitHistory.map((x) => (
                <li key={x.id}>{x.workUnitName}: {fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}</li>
              ))}
            </ul>
            <h3 className="sub">Sorumlu personel</h3>
            <ul className="plain">
              {p.advisorHistory.length === 0 && <li className="muted">Kayıt yok</li>}
              {p.advisorHistory.map((x) => (
                <li key={x.id}>{x.advisorName}: {fmtDate(x.startDate)} – {x.endDate ? fmtDate(x.endDate) : 'devam ediyor'}</li>
              ))}
            </ul>
          </Card>
        )}
      </div>
      <Card title="Şifre">
        <p className="muted">
          Bu sisteme OBİS şifrenizle giriş yapıyorsunuz. Şifrenizi değiştirmek veya yenilemek için OBİS'i kullanın ya da bilgi işleme başvurun.
        </p>
      </Card>
    </div>
  );
}

function EditContact({ profile, onSaved }: { profile: StudentProfileFull; onSaved: () => void }) {
  const [phone, setPhone] = useState(profile.phoneNumber ?? '');
  const [iban, setIban] = useState(profile.iban);
  const [holder, setHolder] = useState(profile.fullName);
  const [confirm, setConfirm] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [done, setDone] = useState(false);

  useEffect(() => {
    setPhone(profile.phoneNumber ?? '');
    setIban(profile.iban);
  }, [profile]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setDone(false);
    try {
      await api.put('/api/students/me', { phoneNumber: phone, iban, ibanHolderName: holder, confirmOwnIban: confirm });
      setDone(true);
      onSaved();
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Kaydedilemedi.'));
    }
  }

  const general = error && Object.keys(error.fieldErrors).length === 0 ? error.message : undefined;
  return (
    <Card
      title="İletişim ve IBAN"
      subtitle="IBAN kendi adınıza, şubesi olan bir bankadaki hesabınıza ait olmalıdır. Enpara, Papara gibi dijital/sanal banka hesapları kabul edilmez."
    >
      <form onSubmit={submit}>
        {general && <Alert>{general}</Alert>}
        {done && <Alert kind="info">Bilgileriniz güncellendi.</Alert>}
        <div className="grid-2">
          <Field label="Cep telefonu" error={firstError(error, 'phoneNumber')}>
            <PhoneInput value={phone} onChange={setPhone} required />
          </Field>
          <Field label="IBAN" error={firstError(error, 'iban')}>
            <input value={iban} onChange={(e) => setIban(e.target.value.toUpperCase())} required />
          </Field>
          <Field label="Hesap sahibinin adı soyadı" error={firstError(error, 'ibanHolderName')}>
            <input value={holder} onChange={(e) => setHolder(e.target.value)} required />
          </Field>
        </div>
        <label className="row" style={{ margin: '8px 0 12px' }}>
          <input type="checkbox" checked={confirm} onChange={(e) => setConfirm(e.target.checked)} />
          Bu IBAN bana aittir ve şubesi olan bir bankadaki hesabımdır.
        </label>
        {firstError(error, 'confirmOwnIban') && <Alert>{firstError(error, 'confirmOwnIban')}</Alert>}
        <button className="btn primary">Kaydet</button>
      </form>
    </Card>
  );
}
