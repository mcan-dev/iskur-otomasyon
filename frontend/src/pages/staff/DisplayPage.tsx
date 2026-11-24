import QRCode from 'qrcode';
import { useEffect, useRef, useState, type FormEvent } from 'react';
import { api, ApiError } from '../../api/client';
import type { AttendanceDisplay, AttendanceSchedule } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Alert, Card, Field } from '../../components/ui';
import { fmtTime } from '../../format';

/**
 * Fakültedeki monitörlerde açık tutulan mesai karekodu ekranı. Sekreterliğin belirlediği sabah (giriş) ve
 * akşam (çıkış) saatlerinde karekod gösterilir ve 30 saniyede bir değişir; diğer saatlerde sıradaki saat yazar.
 * Öğrenci yalnızca çalışma gününde (onaylı planındaki günde) okutabilir.
 */
export default function DisplayPage() {
  const { user } = useAuth();
  const [full, setFull] = useState(false);
  return (
    <div className="stack">
      <Card
        title="Mesai karekod ekranı"
        subtitle="Bu sayfayı fakültedeki monitörde açık bırakın. Karekod saatleri içinde kendiliğinden görünür ve 30 saniyede bir değişir."
      >
        <div className="row">
          <button className="btn primary" onClick={() => setFull(true)}>Tam ekran göster</button>
        </div>
      </Card>
      <div className={full ? 'qr-fullscreen' : 'card'}>
        {full && (
          <div className="row" style={{ justifyContent: 'flex-end', padding: 12 }}>
            <button className="btn" onClick={() => setFull(false)}>Kapat</button>
          </div>
        )}
        <LiveQr big={full} />
      </div>
      {user?.role === 'FacultyAdmin' && <ScheduleForm />}
    </div>
  );
}

function LiveQr({ big }: { big: boolean }) {
  const [display, setDisplay] = useState<AttendanceDisplay | null>(null);
  const [error, setError] = useState('');
  const [left, setLeft] = useState(1);
  const canvas = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    let cancelled = false;
    let timer = 0;
    async function load() {
      try {
        const next = await api.get<AttendanceDisplay>('/api/attendance/display');
        if (cancelled) return;
        setDisplay(next);
        setError('');
        // Karekod varken yenileme anında, yokken dakikada bir yeniden sorulur (pencere açıldığında görünsün).
        const wait = next.qr ? Math.max(1000, new Date(next.qr.refreshAtUtc).getTime() - Date.now()) : 30000;
        timer = window.setTimeout(load, wait);
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof ApiError ? err.message : 'Ekran bilgisi alınamadı.');
        timer = window.setTimeout(load, 10000);
      }
    }
    load();
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, []);

  const qr = display?.qr;
  useEffect(() => {
    if (qr && canvas.current) QRCode.toCanvas(canvas.current, qr.token, { width: big ? 520 : 320, margin: 1 });
  }, [qr, big]);

  useEffect(() => {
    if (!qr) return;
    const id = window.setInterval(() => {
      const total = qr.windowSeconds * 1000;
      setLeft(Math.max(0, Math.min(1, (new Date(qr.refreshAtUtc).getTime() - Date.now()) / total)));
    }, 250);
    return () => window.clearInterval(id);
  }, [qr]);

  return (
    <div className="qr-display">
      {error && <Alert>{error}</Alert>}
      {display && <div className="qr-title">{display.title}</div>}
      {display?.windowStart && display.windowEnd && (
        <p className="muted">{fmtTime(display.windowStart)} – {fmtTime(display.windowEnd)} arasında okutabilirsiniz.</p>
      )}
      {qr ? (
        <>
          <div className="qr-box"><canvas ref={canvas} /></div>
          <div className="qr-bar" style={{ width: big ? 520 : 320 }}><i style={{ width: `${left * 100}%` }} /></div>
          <p className="muted small">Kod {qr.windowSeconds} saniyede bir değişir. Yalnızca bugün çalışma günü olan öğrenciler okutabilir.</p>
        </>
      ) : (
        display && (
          <>
            {display.nextWindow && <p>{display.nextWindow}</p>}
            <p className="muted small">
              Sabah {fmtTime(display.schedule.morningStart)}–{fmtTime(display.schedule.morningEnd)} · Akşam {fmtTime(display.schedule.eveningStart)}–{fmtTime(display.schedule.eveningEnd)}
            </p>
          </>
        )
      )}
    </div>
  );
}

function ScheduleForm() {
  const [v, setV] = useState<AttendanceSchedule | null>(null);
  const [message, setMessage] = useState<{ kind: 'info' | 'error'; text: string } | null>(null);

  useEffect(() => {
    api.get<AttendanceSchedule>('/api/attendance/schedule')
      .then((s) => setV({
        morningStart: fmtTime(s.morningStart), morningEnd: fmtTime(s.morningEnd),
        eveningStart: fmtTime(s.eveningStart), eveningEnd: fmtTime(s.eveningEnd),
      }))
      .catch(() => setMessage({ kind: 'error', text: 'Saatler yüklenemedi.' }));
  }, []);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!v) return;
    setMessage(null);
    try {
      const withSeconds = (t: string) => (t.length === 5 ? `${t}:00` : t);
      await api.put('/api/attendance/schedule', {
        morningStart: withSeconds(v.morningStart), morningEnd: withSeconds(v.morningEnd),
        eveningStart: withSeconds(v.eveningStart), eveningEnd: withSeconds(v.eveningEnd),
      });
      setMessage({ kind: 'info', text: 'Karekod saatleri kaydedildi.' });
    } catch (err) {
      const text = err instanceof ApiError ? (Object.values(err.fieldErrors).flat().join(' ') || err.message) : 'Kaydedilemedi.';
      setMessage({ kind: 'error', text });
    }
  }

  const time = (k: keyof AttendanceSchedule, label: string) => (
    <Field label={label}>
      <input type="time" value={v?.[k] ?? ''} onChange={(e) => v && setV({ ...v, [k]: e.target.value })} required />
    </Field>
  );

  return (
    <Card title="Karekod saatleri" subtitle="Sabah mesai başlangıcında ve akşam mesai bitişinde karekodun monitörde görüneceği saat aralıkları.">
      {message && <Alert kind={message.kind}>{message.text}</Alert>}
      {v && (
        <form onSubmit={submit}>
          <div className="filters">
            {time('morningStart', 'Sabah başlangıç')}
            {time('morningEnd', 'Sabah bitiş')}
            {time('eveningStart', 'Akşam başlangıç')}
            {time('eveningEnd', 'Akşam bitiş')}
          </div>
          <button className="btn primary">Kaydet</button>
        </form>
      )}
    </Card>
  );
}
