import type { Role } from '../api/types';

export interface MenuItem {
  path: string;
  label: string;
  icon: string;
  desc: string;
}

export const roleLabels: Record<Role, string> = {
  FacultyAdmin: 'Fakülte Sekreterliği',
  DepartmentHead: 'Birim Yetkilisi',
  Advisor: 'Sorumlu Personel',
  Student: 'Öğrenci',
};

// Her rolün menüsü gereksinim dokümanının 2. bölümündeki yetkilere göre kurulur.
const home: MenuItem = { path: '/', label: 'Ana Sayfa', icon: 'home', desc: 'Özet bilgiler ve hızlı erişim' };

export const menus: Record<Role, MenuItem[]> = {
  FacultyAdmin: [
    home,
    { path: '/ogrenciler', label: 'Öğrenciler', icon: 'users', desc: 'Öğrenci ekle, düzenle, toplu ata ve incele' },
    { path: '/plan-onaylari', label: 'Çalışma Planı Onayları', icon: 'check', desc: 'Aylık planları onayla veya reddet' },
    { path: '/calisanlar', label: 'Kim Ne Zaman Çalışıyor', icon: 'calendar', desc: 'Günlere göre çalışan öğrenciler ve geçmiş yıllar' },
    { path: '/izinler', label: 'İzinler', icon: 'mail', desc: 'Öğrenci izinlerini izle, gerekirse iptal et' },
    { path: '/devamsizlik', label: 'Devamsızlık', icon: 'alert', desc: 'Devamsızlık kayıtları ve sınır uyarıları' },
    { path: '/mesai-ekrani', label: 'Mesai Karekod Ekranı', icon: 'qr', desc: 'Monitörde sabah ve akşam karekodu, saat ayarları' },
    { path: '/devam-oturumlari', label: 'Karekod Oturumları', icon: 'qr', desc: 'Ek devam oturumu aç, kayıtları düzelt' },
    { path: '/gorevler', label: 'Görevler', icon: 'tasks', desc: 'Öğrencilere görev ver ve takip et' },
    { path: '/raporlar', label: 'Raporlar', icon: 'report', desc: 'Aylık çalışma, devam ve izin raporları' },
    { path: '/iliski-kesme', label: 'İlişik Kesme', icon: 'ban', desc: 'Program ilişiğini kesme kararları' },
    { path: '/tanimlar', label: 'Tanımlar ve Hesaplar', icon: 'settings', desc: 'Bölüm, birim, tatil ve personel hesapları' },
    { path: '/denetim', label: 'Denetim Kayıtları', icon: 'shield', desc: 'Sistemde yapılan işlemlerin izi' },
  ],
  DepartmentHead: [
    home,
    { path: '/ogrenciler', label: 'Bölüm Öğrencileri', icon: 'users', desc: 'Bölümünüzdeki öğrenciler ve sorumlu personel atama' },
    { path: '/calisanlar', label: 'Kim Ne Zaman Çalışıyor', icon: 'calendar', desc: 'Günlere göre çalışan öğrenciler ve geçmiş yıllar' },
    { path: '/mesai-ekrani', label: 'Mesai Karekod Ekranı', icon: 'qr', desc: 'Monitörde sabah ve akşam karekodu' },
    { path: '/devam-oturumlari', label: 'Karekod Oturumları', icon: 'qr', desc: 'Ek devam oturumu aç, karekodu göster' },
    { path: '/izinler', label: 'İzin ve Devamsızlık', icon: 'mail', desc: 'Bölüm izin ve devamsızlık durumu' },
    { path: '/gorevler', label: 'Görevler ve Notlar', icon: 'tasks', desc: 'Görevleri ve değerlendirme notlarını izle' },
    { path: '/bildirimler', label: 'Devamsızlık Bildirimleri', icon: 'bell', desc: 'Sorumlu personelden gelen bildirimler' },
  ],
  Advisor: [
    home,
    { path: '/ogrenciler', label: 'Öğrencilerim', icon: 'users', desc: 'Sorumlusu olduğunuz öğrenciler' },
    { path: '/calisanlar', label: 'Kim Ne Zaman Çalışıyor', icon: 'calendar', desc: 'Öğrencilerinizin çalışma günleri' },
    { path: '/devam', label: 'Devam Durumu', icon: 'qr', desc: 'Öğrencilerin karekodlu devam kayıtları' },
    { path: '/izinler', label: 'İzin Durumu', icon: 'mail', desc: 'Öğrencilerinizin izin durumu' },
    { path: '/gorevler', label: 'Görev ve Değerlendirme', icon: 'tasks', desc: 'Görev ver, değerlendirme notu yaz' },
    { path: '/bildirimler', label: 'Devamsızlık Bildirimi', icon: 'bell', desc: 'Birim yetkilisine bildirim gönder' },
  ],
  Student: [
    home,
    { path: '/plan', label: 'Çalışma Planım', icon: 'calendar', desc: 'Aylık çalışma günlerini seç ve gönder' },
    { path: '/izinlerim', label: 'İzinlerim', icon: 'mail', desc: 'Çalışma günlerinden izin al, hakkını gör' },
    { path: '/devam-kaydi', label: 'Karekod ile Devam', icon: 'qr', desc: 'Günlük devam kaydını karekodla yap' },
    { path: '/gorevlerim', label: 'Görevlerim', icon: 'tasks', desc: 'Verilen görevler ve değerlendirmeler' },
    { path: '/gecmis', label: 'Geçmiş Kayıtlarım', icon: 'clock', desc: 'Plan, izin, devamsızlık ve devam geçmişi' },
    { path: '/profil', label: 'Profilim', icon: 'user', desc: 'Kişisel bilgiler, IBAN ve çalışma günü hakkı' },
  ],
};
