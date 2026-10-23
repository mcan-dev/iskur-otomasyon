# API başvurusu

Bu dosya controller'lardan çıkarılmıştır. Ayrıntılı istek/yanıt şemaları için Development ortamında Swagger ana sayfasını açın.

Tüm istekler (giriş ve kayıt hariç) `Authorization: Bearer <token>` başlığı ister. Hatalar tek biçimde döner: `400` doğrulama (`errors` alanıyla), `401` oturum yok, `403` yetki yok, `404` kayıt yok ya da kapsam dışı, `409` çakışma.
Roller: **Fakülte** = Dekanlık/Fakülte Sekreterliği, **Bölüm Başkanı**, **Danışman**, **Öğrenci**.

## Kimlik doğrulama

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| POST | `/api/auth/login` | Herkese açık | E-posta ve parola ile giriş yapar, JWT erişim jetonu döner. İsteğe bağlı `role` alanı (giriş sayfasının rolü) doluysa hesap rolüyle eşleşmelidir; eşleşmezse 403 döner ve jeton verilmez. |
| POST | `/api/auth/register-student` | Herkese açık | Öğrenci ilk kayıt formu (belge madde 3). Hesap, profil ve ilk program katılım dönemi birlikte oluşur.  |
| GET | `/api/auth/me` | Giriş yapmış herkes | Giriş yapmış kullanıcının kendi hesap bilgileri. |
| POST | `/api/auth/register-staff` | Herkese açık | Danışman, bölüm başkanı veya fakülte sekreterliği başvurusu. Hesap pasif oluşur; fakülte yönetimi `PUT /api/users/{id}/active` ile aktif edince giriş açılır. |
| POST | `/api/auth/forgot-password` | Herkese açık | Parola sıfırlama bağlantısını e-postayla gönderir. E-posta kayıtlı olsun olmasın yanıt aynıdır (204). |
| POST | `/api/auth/reset-password` | Herkese açık | E-postadaki bağlantıdaki jetonla yeni parola belirler. Bağlantı tek kullanımlık ve 30 dakika geçerlidir. |
| POST | `/api/auth/change-password` | Giriş yapmış herkes | Kendi parolasını değiştirir. |

## Öğrenciler

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/students/me` | Öğrenci | Öğrencinin kendi profili (TC ve IBAN ham görünür). |
| PUT | `/api/students/me` | Öğrenci | Öğrenci kendi telefon ve IBAN bilgisini günceller. |
| GET | `/api/students` | Fakülte, Bölüm Başkanı, Danışman | Öğrenci arama/filtreleme (belge madde 10). Sonuç rolün kapsamıyla otomatik sınırlanır. Listede T.C. kimlik no maskelidir.  |
| GET | `/api/students/{id}` | Giriş yapmış herkes | Öğrenci detayı, tüm geçmişiyle. Ham TC/IBAN yalnızca fakülte yönetimine ve öğrencinin kendisine gösterilir; yönetimin ham veriye erişimi denetim kaydına yazılır.  |
| POST | `/api/students/{id}/end-participation` | Fakülte | Öğrencinin program katılımını sonlandırır (kayıt silinmez, pasif olur). |
| POST | `/api/students/{id}/start-participation` | Fakülte | Pasif öğrenciyi yeni bir katılım dönemiyle programa yeniden dahil eder. |
| PUT | `/api/students/{id}/unit` | Fakülte | Öğrenciyi bir çalışma birimine atar; önceki atama geçmişe taşınır. |
| PUT | `/api/students/{id}/advisor` | Fakülte, Bölüm Başkanı | Öğrenciye danışman atar; önceki danışman geçmişe taşınır. Bölüm başkanı yalnızca kendi bölümü için. |

## Personel hesapları

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/users` | Fakülte, Bölüm Başkanı | Kullanıcı listesi. Bölüm başkanı yalnızca kendi bölümündekileri görür (danışman seçimi için). |
| POST | `/api/users` | Fakülte | Personel hesabı oluşturur (Fakülte yönetimi, Bölüm Başkanı veya Danışman). |
| PUT | `/api/users/{id}/active` | Fakülte | Personel hesabını aktif/pasif yapar (hesap silinmez). |

## Bölümler

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/departments` | Herkese açık | Bölüm listesi. Giriş gerektirmez: öğrenci kayıt formundaki bölüm seçimi için gereklidir. Pasif bölümler (includeInactive=true) yalnızca fakülte yönetimine görünür.  |
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

## Ders günleri

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/students/me/course-days` | Öğrenci |  |
| PUT | `/api/students/me/course-days` | Öğrenci |  |

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

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| GET | `/api/leaves/me/balance` | Öğrenci |  |
| GET | `/api/leaves/me` | Öğrenci |  |
| POST | `/api/leaves/me` | Öğrenci | İzin talebi oluşturur. Yanıttaki bakiye, bu talep dahil güncel izin durumunu ve uyarıları içerir. |
| POST | `/api/leaves/me/{id}/cancel` | Öğrenci |  |
| POST | `/api/leaves/me/{id}/document` | Öğrenci | Talebe belge ekler (PDF, JPG veya PNG, en fazla 5 MB). |
| GET | `/api/leaves/{id}/document` | Giriş yapmış herkes |  |
| GET | `/api/leaves` | Fakülte, Bölüm Başkanı, Danışman |  |
| GET | `/api/leaves/{id}` | Giriş yapmış herkes |  |
| POST | `/api/leaves/{id}/review` | Fakülte | Talebi onaylar veya gerekçeyle reddeder. |
| GET | `/api/leaves/students/{studentId}/balance` | Fakülte, Bölüm Başkanı, Danışman |  |
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
| POST | `/api/attendance/sessions` | Fakülte, Bölüm Başkanı |  |
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
| GET | `/api/reports/{kind}` | Fakülte, Bölüm Başkanı, Danışman |  |
| GET | `/api/reports/{kind}/export` | Fakülte, Bölüm Başkanı, Danışman |  |

## İlişik kesme kararları

| Yöntem | Adres | Kim | Açıklama |
|---|---|---|---|
| POST | `/api/terminations` | Fakülte | Kararı kaydeder ve öğrencinin program katılımını sonlandırır. |
| GET | `/api/terminations` | Fakülte, Bölüm Başkanı |  |
