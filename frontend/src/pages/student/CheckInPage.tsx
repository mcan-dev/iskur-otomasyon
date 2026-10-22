import { useEffect, useRef, useState, type FormEvent } from 'react';
import { api, ApiError } from '../../api/client';
import type { Attendance, AttendanceComparison } from '../../api/types';
import { Alert, Badge, Card, Empty, MonthPicker, Stat } from '../../components/ui';
import { fmtDate, fmtDateTime, fmtTime, outcomeLabels, outcomeTone } from '../../format';
import { useLoad } from '../../useLoad';

// Tarayıcının yerleşik karekod okuyucusu (Chrome/Edge, Android). Yoksa elle giriş kullanılır.
interface Detector {
  detect(source: HTMLVideoElement): Promise<{ rawValue: string }[]>;
}
type DetectorCtor = new (options: { formats: string[] }) => Detector;
const Supported = (window as unknown as { BarcodeDetector?: DetectorCtor }).BarcodeDetector;

export default function CheckInPage() {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const records = useLoad(() => api.get<Attendance[]>(`/api/attendance/me?year=${year}&month=${month}`), [year, month]);
  const comparison = useLoad(() => api.get<AttendanceComparison>(`/api/attendance/me/comparison?year=${year}&month=${month}`), [year, month]);
  const [result, setResult] = useState<{ ok: boolean; text: string } | null>(null);

  async function checkIn(token: string) {
    setResult(null);
    try {
      await api.post('/api/attendance/check-in', { token: token.trim() });
      setResult({ ok: true, text: 'Devam kaydınız alındı.' });
      records.reload();
      comparison.reload();
    } catch (err) {
      setResult({ ok: false, text: err instanceof ApiError ? err.message : 'Devam kaydı alınamadı.' });
    }
  }

  const c = comparison.data;
  return (
    <div className="stack">
      <div className="two-col">
        <Card title="Karekod ile devam" subtitle="Çalışma günü fakültedeki ekranda gösterilen karekodu okutun. Kod her 30 saniyede yenilenir.">
          {result && <Alert kind={result.ok ? 'info' : 'error'}>{result.text}</Alert>}
          <Scanner onToken={checkIn} />
        </Card>
        <ManualToken onSubmit={checkIn} />
      </div>

      <Card title="Devam durumum">
        <div className="row between" style={{ marginBottom: 12 }}>
          <MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} />
        </div>
        {comparison.error && <Alert>{comparison.error}</Alert>}
        {c && !c.hasApprovedPlan && <Alert kind="info">Bu ay için onaylı planınız yok; devam karşılaştırması yapılamıyor.</Alert>}
        {c && c.hasApprovedPlan && (
          <>
            <div className="stats">
              <Stat label="Planlanan gün" value={c.plannedDays} />
              <Stat label="Gelinen gün" value={c.attendedDays} tone="green" />
              <Stat label="İzinli" value={c.leaveDays} tone="blue" />
              <Stat label="Devamsızlık" value={c.recordedAbsenceDays} tone="red" />
              <Stat label="Kayıt yok" value={c.missingDays} tone="amber" />
            </div>
            <table className="table" style={{ marginTop: 14 }}>
              <thead><tr><th>Tarih</th><th>Durum</th></tr></thead>
              <tbody>
                {c.days.map((d) => (
                  <tr key={d.date}>
                    <td>{fmtDate(d.date)}</td>
                    <td><Badge tone={outcomeTone[d.outcome]}>{outcomeLabels[d.outcome]}</Badge></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        )}
      </Card>

      <Card title="Devam kayıtlarım">
        {records.data?.length === 0 && <Empty>Bu ay devam kaydınız yok.</Empty>}
        {records.data && records.data.length > 0 && (
          <table className="table">
            <thead><tr><th>Tarih</th><th>Oturum</th><th>Kayıt zamanı</th><th></th></tr></thead>
            <tbody>
              {records.data.map((r) => (
                <tr key={r.id}>
                  <td>{fmtDate(r.date)}</td>
                  <td>{fmtTime(r.sessionStart)} – {fmtTime(r.sessionEnd)}</td>
                  <td>{fmtDateTime(r.checkedInAt)}</td>
                  <td>
                    {r.isVoided ? <Badge tone="gray">İptal edildi</Badge> : r.isManual ? <Badge tone="blue">Elle girildi</Badge> : <Badge tone="green">Karekod</Badge>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}

function ManualToken({ onSubmit }: { onSubmit: (token: string) => void }) {
  const [token, setToken] = useState('');
  function submit(e: FormEvent) {
    e.preventDefault();
    if (token.trim()) onSubmit(token);
    setToken('');
  }
  return (
    <Card title="Kodu elle girin" subtitle="Kamera kullanamıyorsanız ekrandaki kodu buraya yazın.">
      <form onSubmit={submit}>
        <input className="wide-input" value={token} onChange={(e) => setToken(e.target.value)} placeholder="Karekod içeriği" />
        <button className="btn wide" style={{ marginTop: 10 }}>Gönder</button>
      </form>
    </Card>
  );
}

function Scanner({ onToken }: { onToken: (token: string) => void }) {
  const video = useRef<HTMLVideoElement>(null);
  const [active, setActive] = useState(false);
  const [error, setError] = useState('');
  const handler = useRef(onToken);
  handler.current = onToken;

  useEffect(() => {
    if (!active || !Supported) return;
    let stream: MediaStream | null = null;
    let timer = 0;
    let stopped = false;

    (async () => {
      try {
        stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
        if (stopped || !video.current) return;
        video.current.srcObject = stream;
        await video.current.play();
        const detector = new Supported({ formats: ['qr_code'] });
        timer = window.setInterval(async () => {
          if (!video.current) return;
          const found = await detector.detect(video.current).catch(() => []);
          if (found.length > 0) {
            setActive(false);
            handler.current(found[0].rawValue);
          }
        }, 300);
      } catch {
        setError('Kameraya erişilemedi. Tarayıcı iznini kontrol edin; kamera yalnızca güvenli bağlantıda (https) çalışır.');
        setActive(false);
      }
    })();

    return () => {
      stopped = true;
      window.clearInterval(timer);
      stream?.getTracks().forEach((t) => t.stop());
    };
  }, [active]);

  if (!Supported) {
    return <Alert kind="info">Bu tarayıcı karekod okumayı desteklemiyor. Sağdaki alana kodu elle girin ya da Chrome kullanın.</Alert>;
  }
  return (
    <div className="stack">
      {error && <Alert>{error}</Alert>}
      {active ? (
        <>
          <div className="scan-box"><video ref={video} muted playsInline /></div>
          <button className="btn" onClick={() => setActive(false)}>Kamerayı kapat</button>
        </>
      ) : (
        <button className="btn primary" onClick={() => { setError(''); setActive(true); }}>Kamerayı aç ve okut</button>
      )}
    </div>
  );
}
