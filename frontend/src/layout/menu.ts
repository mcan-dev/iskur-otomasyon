import type { Role } from '../api/types';

export interface MenuItem {
  path: string;
  label: string;
  icon: string;
}

export const roleLabels: Record<Role, string> = {
  FacultyAdmin: 'Fakülte Sekreterliği',
  DepartmentHead: 'Bölüm Başkanı',
  Advisor: 'Danışman Öğretim Elemanı',
  Student: 'Öğrenci',
};

// Her rolün menüsü gereksinim dokümanının 2. bölümündeki yetkilere göre kurulur.
const home: MenuItem = { path: '/', label: 'Ana Sayfa', icon: '▦' };

export const menus: Record<Role, MenuItem[]> = {
  FacultyAdmin: [
    home,
    { path: '/ogrenciler', label: 'Öğrenciler', icon: '☺' },
    { path: '/plan-onaylari', label: 'Çalışma Planı Onayları', icon: '✓' },
    { path: '/izinler', label: 'İzin Talepleri', icon: '✉' },
    { path: '/devamsizlik', label: 'Devamsızlık', icon: '⚠' },
    { path: '/devam-oturumlari', label: 'Karekod Devam', icon: '▣' },
    { path: '/gorevler', label: 'Görevler', icon: '☰' },
    { path: '/raporlar', label: 'Raporlar', icon: '▤' },
    { path: '/iliski-kesme', label: 'İlişik Kesme', icon: '✕' },
    { path: '/tanimlar', label: 'Tanımlar ve Hesaplar', icon: '⚙' },
    { path: '/denetim', label: 'Denetim Kayıtları', icon: '◷' },
  ],
  DepartmentHead: [
    home,
    { path: '/ogrenciler', label: 'Bölüm Öğrencileri', icon: '☺' },
    { path: '/devam-oturumlari', label: 'Karekod Devam', icon: '▣' },
    { path: '/izinler', label: 'İzin ve Devamsızlık', icon: '✉' },
    { path: '/gorevler', label: 'Görevler ve Notlar', icon: '☰' },
    { path: '/bildirimler', label: 'Devamsızlık Bildirimleri', icon: '⚠' },
    { path: '/raporlar', label: 'Raporlar', icon: '▤' },
  ],
  Advisor: [
    home,
    { path: '/ogrenciler', label: 'Öğrencilerim', icon: '☺' },
    { path: '/devam', label: 'Devam Durumu', icon: '▣' },
    { path: '/izinler', label: 'İzin Durumu', icon: '✉' },
    { path: '/gorevler', label: 'Görev ve Değerlendirme', icon: '☰' },
    { path: '/bildirimler', label: 'Devamsızlık Bildirimi', icon: '⚠' },
    { path: '/raporlar', label: 'Raporlar', icon: '▤' },
  ],
  Student: [
    home,
    { path: '/plan', label: 'Çalışma Planım', icon: '▥' },
    { path: '/izinlerim', label: 'İzin Taleplerim', icon: '✉' },
    { path: '/devam-kaydi', label: 'Karekod ile Devam', icon: '▣' },
    { path: '/gorevlerim', label: 'Görevlerim', icon: '☰' },
    { path: '/gecmis', label: 'Geçmiş Kayıtlarım', icon: '◷' },
    { path: '/profil', label: 'Profilim', icon: '☺' },
  ],
};
