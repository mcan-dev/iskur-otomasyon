import type { ReactNode } from 'react';

export function Card({ title, subtitle, children }: { title?: string; subtitle?: string; children: ReactNode }) {
  return (
    <section className="card">
      {title && <h2>{title}</h2>}
      {subtitle && <p className="muted">{subtitle}</p>}
      {children}
    </section>
  );
}

export function Stat({ label, value, hint, tone = 'blue' }: { label: string; value: ReactNode; hint?: string; tone?: 'blue' | 'amber' | 'green' | 'red' }) {
  return (
    <div className="card stat">
      <span className="muted">{label}</span>
      <strong className={`stat-value tone-${tone}`}>{value}</strong>
      {hint && <span className="muted small">{hint}</span>}
    </div>
  );
}

export function Alert({ kind = 'error', children }: { kind?: 'error' | 'warn' | 'info'; children: ReactNode }) {
  return <div className={`alert ${kind}`}>{children}</div>;
}

export function Field({ label, error, hint, children }: { label: string; error?: string; hint?: string; children: ReactNode }) {
  return (
    <label className="field">
      <span>{label}</span>
      {children}
      {error ? <small className="field-error">{error}</small> : hint ? <small className="muted">{hint}</small> : null}
    </label>
  );
}

export function Badge({ tone = 'gray', children }: { tone?: 'green' | 'amber' | 'red' | 'gray' | 'blue'; children: ReactNode }) {
  return <span className={`badge ${tone}`}>{children}</span>;
}

export function Empty({ children }: { children: ReactNode }) {
  return <p className="muted empty">{children}</p>;
}

/** Ay seçici: önceki / sonraki ay düğmeleri. */
export function MonthPicker({ year, month, onChange }: { year: number; month: number; onChange: (y: number, m: number) => void }) {
  const move = (delta: number) => {
    const d = new Date(year, month - 1 + delta, 1);
    onChange(d.getFullYear(), d.getMonth() + 1);
  };
  const names = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
  return (
    <div className="month-picker">
      <button type="button" className="btn ghost" onClick={() => move(-1)} aria-label="Önceki ay">
        ‹
      </button>
      <strong>
        {names[month - 1]} {year}
      </strong>
      <button type="button" className="btn ghost" onClick={() => move(1)} aria-label="Sonraki ay">
        ›
      </button>
    </div>
  );
}

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  return (
    <div className="modal-back" onMouseDown={onClose}>
      <div className="modal" onMouseDown={(e) => e.stopPropagation()} role="dialog" aria-label={title}>
        <div className="row between">
          <h2>{title}</h2>
          <button type="button" className="btn ghost small" onClick={onClose}>Kapat</button>
        </div>
        {children}
      </div>
    </div>
  );
}

export function Pager({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (p: number) => void }) {
  if (totalPages <= 1) return null;
  return (
    <div className="row" style={{ marginTop: 12, justifyContent: 'center' }}>
      <button className="btn small" disabled={page <= 1} onClick={() => onChange(page - 1)}>Önceki</button>
      <span className="muted">{page} / {totalPages}</span>
      <button className="btn small" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>Sonraki</button>
    </div>
  );
}
