import { useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { Card } from '../components/ui';
import { menus } from '../layout/menu';

// Henüz yazılmamış ekranlar için. Her ekran kendi aşamasında bu sayfanın yerini alır.
export default function Placeholder() {
  const { pathname } = useLocation();
  const { user } = useAuth();
  const item = user ? menus[user.role].find((m) => m.path === pathname) : undefined;
  return (
    <Card title={item?.label ?? 'Sayfa'} subtitle="Bu ekran arayüzün sonraki aşamalarında eklenecek.">
      <p className="muted">Arka yüzdeki işlemleri bu ekrandan kullanabileceksiniz.</p>
    </Card>
  );
}
