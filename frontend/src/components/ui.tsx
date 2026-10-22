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
