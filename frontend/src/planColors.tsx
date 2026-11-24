import type { CalendarDay } from './api/types';

const isHalfDay = (d: CalendarDay) => d.lessonSession === 'Morning' || d.lessonSession === 'Afternoon';

function weekOf(date: string): string {
  const d = new Date(date + 'T00:00:00');
  d.setDate(d.getDate() - ((d.getDay() + 6) % 7));
  return `${d.getFullYear()}-${d.getMonth() + 1}-${d.getDate()}`;
}

/**
 * Hücre rengi (sunucudaki kuralın aynısı, seçim değiştikçe anında güncellenir):
 * yarım gün dersli seçilen gün sarı, boş seçilen gün yeşil; aynı haftada yarım gün dersli gün seçilip
 * tamamen boş bir gün seçilmemişse o boş gün kırmızı. Seçilemeyen günler (hafta sonu, tatil, tam gün ders) kırmızı.
 */
export function dayTone(d: CalendarDay, picked: Set<string>, days: CalendarDay[]): { cls: string; label: string } {
  if (!d.isSelectable) return { cls: 'Red', label: d.label };
  const chosen = picked.has(d.date);
  if (chosen && isHalfDay(d)) return { cls: 'pickYellow', label: 'Seçildi, yarım gün dersli' };
  if (chosen) return { cls: 'pickGreen', label: 'Seçildi, boş gün' };
  const free = !d.lessonSession;
  if (free) {
    const week = weekOf(d.date);
    const halfDayPicked = days.some((x) => weekOf(x.date) === week && picked.has(x.date) && isHalfDay(x));
    if (halfDayPicked) return { cls: 'warnRed', label: 'Boş gün seçilmedi; bu hafta yarım gün dersli gün seçildi' };
  }
  return { cls: 'Gray', label: isHalfDay(d) ? 'Yarım gün dersli gün' : 'Boş gün' };
}


const headers = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];

/** Planın salt okunur aylık takvimi: seçilen günler ve sarı/yeşil/kırmızı renkler (personel ekranları için). */
export function PlanMonthView({ days }: { days: CalendarDay[] }) {
  const picked = new Set(days.filter((d) => d.isSelected).map((d) => d.date));
  const offset = days.length ? (new Date(days[0].date + 'T00:00:00').getDay() + 6) % 7 : 0;
  return (
    <>
      <div className="cal">
        {headers.map((h) => (
          <div key={h} className="cal-head">{h}</div>
        ))}
        {Array.from({ length: offset }).map((_, i) => (
          <div key={`o${i}`} className="cal-cell empty-cell" />
        ))}
        {days.map((d) => {
          const tone = dayTone(d, picked, days);
          return (
            <div key={d.date} className={`cal-cell ${tone.cls}`} title={tone.label}>
              <span className="num">{Number(d.date.slice(8))}</span>
              <span className="lbl">{tone.label}</span>
            </div>
          );
        })}
      </div>
      <PlanLegend />
    </>
  );
}

export function PlanLegend() {
  return (
    <div className="legend" style={{ marginTop: 12 }}>
      <span><i className="dot" style={{ background: '#c9efd8' }} />Seçilen boş gün</span>
      <span><i className="dot" style={{ background: '#ffe9a8' }} />Seçilen yarım gün dersli gün</span>
      <span><i className="dot" style={{ background: '#ffd6d6' }} />Seçilmemiş boş gün (yarım gün yerine seçilebilirdi)</span>
      <span><i className="dot" style={{ background: '#fdf1f1' }} />Seçilemez (hafta sonu, tatil, tam gün ders)</span>
    </div>
  );
}
