import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Paged, StudentSummary } from '../api/types';

/** Ad veya öğrenci numarasıyla aranıp seçilen öğrenci alanı. */
export default function StudentPicker({ value, onChange }: { value: StudentSummary | null; onChange: (s: StudentSummary | null) => void }) {
  const [text, setText] = useState('');
  const [results, setResults] = useState<StudentSummary[]>([]);

  useEffect(() => {
    if (value || text.trim().length < 2) {
      setResults([]);
      return;
    }
    const handle = window.setTimeout(() => {
      api
        .get<Paged<StudentSummary>>(`/api/students?page=1&pageSize=8&isActive=true&text=${encodeURIComponent(text.trim())}`)
        .then((r) => setResults(r.items))
        .catch(() => setResults([]));
    }, 250);
    return () => window.clearTimeout(handle);
  }, [text, value]);

  if (value) {
    return (
      <div className="row">
        <span className="badge blue">{value.fullName} · {value.studentNumber}</span>
        <button type="button" className="btn small" onClick={() => onChange(null)}>Değiştir</button>
      </div>
    );
  }
  return (
    <div className="picker">
      <input value={text} onChange={(e) => setText(e.target.value)} placeholder="Ad soyad veya öğrenci no yazın" />
      {results.length > 0 && (
        <ul className="picker-list">
          {results.map((s) => (
            <li key={s.id}>
              <button type="button" onClick={() => onChange(s)}>
                {s.fullName} <span className="muted small">{s.studentNumber} · {s.departmentName}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
