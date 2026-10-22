import type { Role } from '../api/types';

export interface MenuItem {
  path: string;
  label: string;
  icon: string;
  desc: string;
}

export const roleLabels: Record<Role, string> = {
  FacultyAdmin: 'Fakülte Sekreterliği',
  DepartmentHead: 'Bölüm Başkanı',
  Advisor: 'Danışman Öğretim Elemanı',
  Student: 'Öğrenci',
};

// Her rolün menüsü gereksinim dokümanının 2. bölümündeki yetkilere göre kurulur.
const home: MenuItem = { path: '/', label: 'Ana Sayfa', icon: 'home', desc: 'Özet bilgiler ve hızlı erişim' };

export const menus: Record<Role, MenuItem[]> = {
  FacultyAdmin: [
    home,
    { path: '/ogrenciler', label: 'Öğrenciler', icon: 'users', desc: 'Kayıtlı öğrencileri ara, ata ve incele' },
    { path: '/plan-onaylari', label: 'Çalışma Planı Onayları', icon: 'check', desc: 'Aylık planları onayla veya reddet' },
    { path: '/izinler', label: 'İzin Talepleri', icon: 'mail', desc: 'Bekleyen izinleri değerlendir' },
    { path: '/devamsizlik', label: 'Devamsızlık', icon: 'alert', desc: 'Devamsızlık kayıtları ve sınır uyarıları' },
    { path: '/devam-oturumlari', label: 'Karekod Devam', icon: 'qr', desc: 'Devam oturumu aç, karekodu göster' },
    { path: '/gorevler', label: 'Görevler', icon: 'tasks', desc: 'Öğrencilere görev ver ve takip et' },
    { path: '/raporlar', label: 'Raporlar', icon: 'report', desc: 'Aylık çalışma, devam ve izin raporları' },
    { path: '/iliski-kesme', label: 'İlişik Kesme', icon: 'ban', desc: 'Program ilişiğini kesme kararları' },
    { path: '/tanimlar', label: 'Tanımlar ve Hesaplar', icon: 'settings', desc: 'Bölüm, birim, tatil ve personel hesapları' },
    { path: '/denetim', label: 'Denetim Kayıtları', icon: 'shield', desc: 'Sistemde yapılan işlemlerin izi' },
  ],
  DepartmentHead: [
    home,
    { path: '/ogrenciler', label: 'Bölüm Öğrencileri', icon: 'users', desc: 'Bölümünüzdeki öğrenciler ve danışman atama' },
    { path: '/devam-oturumlari', label: 'Karekod Devam', icon: 'qr', desc: 'Devam oturumu aç, karekodu göster' },
    { path: '/izinler', label: 'İzin ve Devamsızlık', icon: 'mail', desc: 'Bölüm izin ve devamsızlık durumu' },
    { path: '/gorevler', label: 'Görevler ve Notlar', icon: 'tasks', desc: 'Görevleri ve değerlendirme notlarını izle' },
    { path: '/bildirimler', label: 'Devamsızlık Bildirimleri', icon: 'bell', desc: 'Danışmanlardan gelen bildirimler' },
    { path: '/raporlar', label: 'Raporlar', icon: 'report', desc: 'Aylık çalışma, devam ve izin raporları' },
  ],
  Advisor: [
    home,
    { path: '/ogrenciler', label: 'Öğrencilerim', icon: 'users', desc: 'Danışmanı olduğunuz öğrenciler' },
    { path: '/devam', label: 'Devam Durumu', icon: 'qr', desc: 'Öğrencilerin karekodlu devam kayıtları' },
    { path: '/izinler', label: 'İzin Durumu', icon: 'mail', desc: 'Öğrencilerinizin izin durumu' },
    { path: '/gorevler', label: 'Görev ve Değerlendirme', icon: 'tasks', desc: 'Görev ver, değerlendirme notu yaz' },
    { path: '/bildirimler', label: 'Devamsızlık Bildirimi', icon: 'bell', desc: 'Bölüm başkanlığına bildirim gönder' },
    { path: '/raporlar', label: 'Raporlar', icon: 'report', desc: 'Aylık çalışma, devam ve izin raporları' },
  ],
  Student: [
    home,
    { path: '/plan', label: 'Çalışma Planım', icon: 'calendar', desc: 'Aylık çalışma günlerini seç ve gönder' },
    { path: '/izinlerim', label: 'İzin Taleplerim', icon: 'mail', desc: 'İzin iste, durumunu ve hakkını gör' },
    { path: '/devam-kaydi', label: 'Karekod ile Devam', icon: 'qr', desc: 'Günlük devam kaydını karekodla yap' },
    { path: '/gorevlerim', label: 'Görevlerim', icon: 'tasks', desc: 'Verilen görevler ve değerlendirmeler' },
    { path: '/gecmis', label: 'Geçmiş Kayıtlarım', icon: 'clock', desc: 'Plan, izin, devamsızlık ve devam geçmişi' },
    { path: '/profil', label: 'Profilim', icon: 'user', desc: 'Kişisel bilgiler, IBAN ve parola' },
  ],
};
