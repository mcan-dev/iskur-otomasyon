# API başvurusu

Bu dosya controller'lardan çıkarılmıştır. Ayrıntılı istek/yanıt şemaları için Development ortamında Swagger ana sayfasını açın.

Tüm istekler (giriş ve şifre sıfırlama hariç) `Authorization: Bearer <token>` başlığı ister. Hatalar tek biçimde döner: `400` doğrulama (`errors` alanıyla), `401` oturum yok, `403` yetki yok, `404` kayıt yok ya da kapsam dışı, `409` çakışma.
Roller: **Fakülte** = Dekanlık/Fakülte Sekreterliği, **Bölüm Başkanı** = arayüzde *Birim yetkilisi*, **Danışman** = arayüzde *Sorumlu personel*, **Öğrenci**. Rol adları arayüzde değişti; yetkiler ve API'deki rol değerleri (`DepartmentHead`, `Advisor`) aynıdır. Sistemde kayıt olma yoktur.

## Kimlik doğrulama

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| POST | `/api/auth/login` | Herkese açık | `{ username, password, role }`. Öğrenci (`role: Student`): öğrenci e-postası + OBİS şifresi. Personel: sicil numarası + şifre. Hesap rolü `role` ile eşleşmelidir; eşleşmezse 403 döner ve jeton verilmez. |
| GET | `/api/auth/me` | Giriş yapmış herkes | Giriş yapmış kullanıcının kendi hesap bilgileri (personelde `registryNumber`). |
| POST | `/api/auth/forgot-password` | Herkese açık | Personel için sıfırlama bağlantısını e-postayla gönderir; alan e-posta ya da sicil numarası olabilir. Öğrenciye e-posta gitmez (OBİS şifresi OBİS'ten yenilenir). Yanıt her durumda 204'tür. |
| POST | `/api/auth/reset-password` | Herkese açık | E-postadaki bağlantıdaki jetonla yeni şifre belirler. Bağlantı tek kullanımlık ve 30 dakika geçerlidir. |
| POST | `/api/auth/change-password` | Personel | Kendi şifresini değiştirir. Öğrenci için 403 (OBİS şifresi). |

## Öğrenciler

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/students/me` | Öğrenci | Öğrencinin kendi profili (TC ve IBAN ham görünür). |
| PUT | `/api/students/me` | Öğrenci | Telefon ve IBAN. IBAN öğrencinin kendisine ait olmalı: `ibanHolderName` kayıtlı adla aynı ve `confirmOwnIban: true`; şubesiz dijital bankalar (Enpara Bank 00157, Colendi 00158, Fups 00159, Ziraat Dinamik 00160, Hayat Finans 00212, T.O.M. 00213, Dünya Katılım 00214) ve elektronik para kuruluşları (008xx/009xx, ör. Papara) reddedilir. |
| GET | `/api/students` | Fakülte, Bölüm Başkanı, Danışman | Öğrenci arama/filtreleme (belge madde 10). Sonuç rolün kapsamıyla otomatik sınırlanır. Listede T.C. kimlik no maskelidir.  |
| GET | `/api/students/{id}` | Giriş yapmış herkes | Öğrenci detayı, tüm geçmişiyle. Ham TC/IBAN yalnızca fakülte yönetimine ve öğrencinin kendisine gösterilir; yönetimin ham veriye erişimi denetim kaydına yazılır.  |
| POST | `/api/students` | Fakülte | Bilgi işlemden gelen bilgilerle öğrenci hesabı açar (`email` öğrenci e-postası, `obisPassword`). |
| PUT | `/api/students/{id}` | Fakülte | Öğrenci bilgilerini düzenler (ad, e-posta, TC, numara, bölüm, kayıt yılı, isteğe bağlı IBAN ve yeni OBİS şifresi). Bölüm değişirse sorumlu personel ataması kapanır. |
| POST | `/api/students/bulk-assign` | Fakülte, Bölüm Başkanı | `{ studentIds, workUnitId?, advisorUserId? / advisorRegistryNumber? }`: seçilen öğrencileri tek işlemde birime ve/veya sorumlu personele atar. Birim atamasını yalnızca fakülte yapar. Atanamayanlar nedenleriyle döner. |
| POST | `/api/students/{id}/end-participation` | Fakülte | Öğrencinin program katılımını sonlandırır (kayıt silinmez, pasif olur). |
| POST | `/api/students/{id}/start-participation` | Fakülte | Pasif öğrenciyi yeni bir katılım dönemiyle programa yeniden dahil eder. |
| PUT | `/api/students/{id}/leave-allowance` | Fakülte | Öğrenciye özel yıllık izin hakkını (gün) belirler; boş gönderilirse genel hak geçerli olur. |
| PUT | `/api/students/{id}/unit` | Fakülte | Öğrenciyi bir çalışma birimine atar; önceki atama geçmişe taşınır. |
| PUT | `/api/students/{id}/advisor` | Fakülte, Bölüm Başkanı | Sorumlu personel atar: `advisorRegistryNumber` (sicil no) ya da `advisorUserId`. Önceki atama geçmişe taşınır. Bölüm başkanı yalnızca kendi bölümü için. |

## Personel hesapları

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/users` | Fakülte, Bölüm Başkanı | Personel listesi. `role`, `departmentId`, `isActive`, `registryNumber` ile süzülür. Bölüm başkanı yalnızca kendi bölümündekileri görür. |
| POST | `/api/users` | Fakülte | Personel hesabı oluşturur; `registryNumber` (sicil no, benzersiz) zorunludur. |
| PUT | `/api/users/{id}` | Fakülte | Personel bilgilerini düzenler (ad, sicil no, e-posta, rol, bölüm, isteğe bağlı yeni şifre). |
| PUT | `/api/users/{id}/active` | Fakülte | Personel hesabını aktif/pasif yapar (hesap silinmez). |

## Bölümler

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/departments` | Herkese açık | Bölüm listesi. Pasif bölümler (includeInactive=true) yalnızca fakülte yönetimine görünür.  |
| POST | `/api/departments` | Fakülte |  |
| PUT | `/api/departments/{id}` | Fakülte |  |

## Çalışma birimleri

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/work-units` | Fakülte |  |
| POST | `/api/work-units` | Fakülte |  |
| PUT | `/api/work-units/{id}` | Fakülte |  |

## Denetim kayıtları

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/audit-logs` | Fakülte |  |

## Aylık çalışma planı

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/work-plans/me/calendar` | Öğrenci | Öğrencinin kendi takvimi: her gün için renk, etiket ve seçim durumu. |
| GET | `/api/work-plans/me` | Öğrenci | Öğrencinin belirli bir aya ait planı. |
| GET | `/api/work-plans/me/history` | Öğrenci | Öğrencinin geçmiş dahil tüm planları. |
| PUT | `/api/work-plans/me` | Öğrenci | Planı kaydeder; aynı ay için tekrar çağrılırsa planı günceller. |
| GET | `/api/work-plans` | Fakülte, Bölüm Başkanı, Danışman | Personel için ay bazında plan listesi. Sonuç rolün kapsamıyla sınırlanır. |
| GET | `/api/work-plans/{id}` | Giriş yapmış herkes |  |
| GET | `/api/work-plans/students/{studentId}/calendar` | Fakülte, Bölüm Başkanı, Danışman | Belirli bir öğrencinin takvimi (personel). |
| PUT | `/api/work-plans/students/{studentId}` | Fakülte | Fakülte yönetimi öğrenci adına plan girer veya düzeltir. |
| POST | `/api/work-plans/{id}/review` | Fakülte | Planı onaylar veya gerekçeyle reddeder. |
| GET | `/api/work-plans/me/lifetime` | Öğrenci | Üniversite hayatı boyunca çalışılabilecek 140 iş gününden çalışılan, planlarda bekleyen ve kalan gün. Plan kaydı bu sınırı aşarsa 400 döner. |
| GET | `/api/work-plans/students/{studentId}/lifetime` | Fakülte, Bölüm Başkanı, Danışman | Aynı bilgi, öğrencinin sorumluları için. |

Tam gün dersli gün çalışma günü olarak seçilemez. Takvim renkleri: yarım gün dersli seçilen gün sarı, boş seçilen gün yeşil; aynı haftada yarım gün dersli gün seçilip boş gün seçilmemişse o boş gün kırmızı.

## Ders günleri

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/students/me/course-days` | Öğrenci |  |
| PUT | `/api/students/me/course-days` | Öğrenci | Her gün için `FullDay`, `Morning` veya `Afternoon` seçilir. IBAN kayıtlı değilse `Iban` alan hatası döner. |

## Takvim (tatil ve sınav dönemi)

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/calendar/holidays` | Giriş yapmış herkes |  |
| POST | `/api/calendar/holidays` | Fakülte |  |
| DELETE | `/api/calendar/holidays/{id}` | Fakülte |  |
| GET | `/api/calendar/exam-periods` | Giriş yapmış herkes |  |
| POST | `/api/calendar/exam-periods` | Fakülte |  |
| DELETE | `/api/calendar/exam-periods/{id}` | Fakülte |  |

## İzin

İzin onay beklemez: öğrenci onaylı planındaki çalışma günlerinden seçer, kayıt hemen geçerli olur ve sekreterlik bilgilendirilir. Her gün ayrı bir kayıttır. Tür ve gerekçe yoktur, açıklama isteğe bağlıdır. Ayın bütün çalışma günleri için izin alınamaz.

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/leaves/me/balance` | Öğrenci | Yıllık izin hakkı, kullanılan (izin + devamsızlık) ve kalan. |
| GET | `/api/leaves/me` | Öğrenci | Öğrencinin izinleri. |
| GET | `/api/leaves/me/calendar` | Öğrenci | `year`, `month`: ayın günleri; onaylı plandaki ve geçmemiş çalışma günleri `canSelect: true`. |
| POST | `/api/leaves/me` | Öğrenci | `{ days: [tarih...], description? }`. Aynı ayın çalışma günleri olmalı; geçmiş gün, plan dışı gün, ayın tüm çalışma günleri ve kalan hakkı aşan seçim reddedilir. |
| POST | `/api/leaves/me/{id}/cancel` | Öğrenci | Gelecekteki kendi iznini iptal eder. |
| GET | `/api/leaves` | Fakülte, Bölüm Başkanı, Danışman | `year`, `month`, `status`, `studentId`, `departmentId`. Öğrencinin sorumluları görür (kapsam rolden gelir). |
| GET | `/api/leaves/{id}` | Giriş yapmış herkes | Kapsam dışıysa 404. |
| POST | `/api/leaves/{id}/cancel` | Fakülte | İzni iptal eder (`note` isteğe bağlı). Onaylama adımı yoktur. |
| GET | `/api/leaves/students/{studentId}/balance` | Fakülte, Bölüm Başkanı, Danışman | |
| GET | `/api/leaves/alerts` | Fakülte, Bölüm Başkanı, Danışman | İzin veya devamsızlık sınırına yaklaşan ya da sınırı aşan öğrenciler. |

## Devamsızlık

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/absences/me` | Öğrenci |  |
| GET | `/api/absences` | Fakülte, Bölüm Başkanı, Danışman |  |
| POST | `/api/absences` | Fakülte |  |
| POST | `/api/absences/{id}/revoke` | Fakülte | Hatalı kaydı gerekçesiyle geri alır; kayıt silinmez. |

## İzin ayarları

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/settings/leave` | Fakülte, Bölüm Başkanı, Danışman |  |
| PUT | `/api/settings/leave` | Fakülte |  |

## Karekodla devam

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/attendance/schedule` | Giriş yapmış herkes | Sabah (giriş) ve akşam (çıkış) karekod saatleri. |
| PUT | `/api/attendance/schedule` | Fakülte | Karekod saatlerini belirler. |
| GET | `/api/attendance/display` | Fakülte, Bölüm Başkanı | Monitör ekranı: saat aralığı içindeyse fakülte geneli oturumu kendiliğinden açar ve 30 saniyede bir değişen karekodu döner; dışındaysa sıradaki saati yazar. Hafta sonu ve tatilde kapalıdır. |
| POST | `/api/attendance/sessions` | Fakülte, Bölüm Başkanı | Ek (elle) oturum. |
| GET | `/api/attendance/sessions` | Fakülte, Bölüm Başkanı |  |
| POST | `/api/attendance/sessions/{id}/close` | Fakülte, Bölüm Başkanı |  |
| GET | `/api/attendance/sessions/{id}/qr` | Fakülte, Bölüm Başkanı | Ekranda gösterilecek güncel karekod. `refreshAtUtc` geçince yeniden istenmelidir. |
| POST | `/api/attendance/check-in` | Öğrenci |  |
| GET | `/api/attendance/me` | Öğrenci |  |
| GET | `/api/attendance/me/comparison` | Öğrenci |  |
| GET | `/api/attendance` | Fakülte, Bölüm Başkanı, Danışman | Günlük (date), aylık (year+month) veya yıllık (year) devam kayıtları. |
| GET | `/api/attendance/students/{studentId}/comparison` | Fakülte, Bölüm Başkanı, Danışman | Planlanan çalışma günleri ile gerçekleşen devamın karşılaştırması. |
| POST | `/api/attendance/manual` | Fakülte | Okutamayan öğrenci için gerekçeli elle kayıt. |
| POST | `/api/attendance/{id}/void` | Fakülte | Hatalı kaydı gerekçeyle iptal eder (kayıt silinmez). |

## Kim ne zaman çalışıyor

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/work-roster` | Fakülte, Bölüm Başkanı, Danışman | `year`, `month`, `departmentId`: her gün onaylı plana göre çalışan öğrenciler ve durumları (Planlı, Çalıştı, İzinli, Kayıt yok). |
| GET | `/api/work-roster/year` | Fakülte, Bölüm Başkanı | `year`: o yıl çalışan öğrenciler (geçmiş yıllar dahil), planlanan/çalışılan/izinli gün. |

## Görevler ve değerlendirme

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| POST | `/api/tasks` | Fakülte, Bölüm Başkanı, Danışman |  |
| GET | `/api/tasks` | Fakülte, Bölüm Başkanı, Danışman |  |
| GET | `/api/tasks/me` | Öğrenci |  |
| GET | `/api/tasks/{id}` | Giriş yapmış herkes | Görev ve tüm değerlendirme geçmişi. |
| POST | `/api/tasks/{id}/evaluations` | Danışman | Danışman not ekler ve/veya görev durumunu günceller. Önceki kayıtlar korunur. |

## Devamsızlık bildirimleri

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| POST | `/api/absence-notices` | Danışman |  |
| GET | `/api/absence-notices` | Fakülte, Bölüm Başkanı, Danışman |  |
| POST | `/api/absence-notices/{id}/read` | Bölüm Başkanı |  |

## Raporlar

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/reports/{kind}` | Fakülte |  |
| GET | `/api/reports/{kind}/export` | Fakülte |  |

## İlişik kesme kararları

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| POST | `/api/terminations` | Fakülte | Kararı kaydeder ve öğrencinin program katılımını sonlandırır. |
| GET | `/api/terminations` | Fakülte, Bölüm Başkanı |  |
