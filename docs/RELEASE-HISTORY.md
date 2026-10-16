# Ayrıntılı sürüm geçmişi

Bu sayfa uygulamanın bütün yayıma hazırlanan 1.x sürümlerinin içeriğini, değişiklik nedenlerini ve doğrulama sınırlarını kaydeder. Sürümler aynı geliştirme çalışması içinde ilerledi; her biri ayrı bir eski indirme olarak sunulmuyor.

**Çalıştırılabilir indirme yalnızca [son sürüm](https://github.com/ardacob/air5-control/releases/latest) içindir.** Eski kaynaklar history/ altında okunabilir. Güncel sürümün kaynakları src/ altındadır.

## 1.0 — İlk Windows uygulaması

[Kaynak](../history/v1.0)

- Koyu temalı taşınabilir WPF uygulaması.
- Windows WinRT üzerinden kısa BLE paketleriyle cihaz bağlantısı.
- ANC, şeffaflık ve Normal düğmeleri.
- Mod değişikliğini GET ile tekrar doğrulama.
- Sol/sağ ve cihaz sunarsa kutu pili; firmware gösterimi.
- Yaklaşık 30 saniyelik pil/mod yenileme, elle yenileme ve bağlantı.
- Paket izin listesi; EQ, kazanç ve reset komutlarının engellenmesi.
- Kaynak, CodeDOM derleme betiği ve gömülü test/önizleme.

Kullanıcı pil ve ANC işlevlerini doğruladı. Normal'e geçince bas azalması bildirdi. O aşamada bunun adaptif EQ'nun bozulması mı yoksa modlar arası ses farkı mı olduğu ayrılmamıştı.

## 1.1 — Normal mod için geçici önlem

[Kaynak](../history/v1.1)

- Normal düğmesi kaldırıldı.
- Mod 00, yalnızca arayüzde değil paket oluşturma ve gönderim doğrulamasında da engellendi.
- ANC, şeffaflık, pil ve firmware korundu.
- Normal durumunu okumak mümkün kaldı; uygulama bu moda geçiremiyordu.
- Kaynak testleri Normal paketinin reddini denetledi.

Bu sürüm kalıcı bir ses çözümü değildi. Kullanıcı Normal'i korumak istedi. Sonraki karşılaştırmada aynı bas azalması kulaklığın dokunmatik kontrolünde de duyuldu; adaptif EQ'nun silindiği kanıtlanmadı.

## 1.2 — Normal geri geldi

[Kaynak](../history/v1.2)

- Üç mod yeniden kullanılabilir oldu.
- Her mod için gönderilen tam paket ve okunan yanıt test edildi.
- Geçersiz mod, EQ ve reset paketlerinin reddi korundu.
- EQ ile bas telafisi eklenmedi.
- Güncel kullanım notlarıyla son paket hazırlandı.

Kullanıcı uygulamanın güzel çalıştığını bildirdi. Bu sürümde tepsi veya Windows başlangıç entegrasyonu yoktu.

## 1.3 — Sistem tepsisi

[Kaynak](../history/v1.3)

- Windows Forms NotifyIcon ve tepsi menüsü.
- Tepsiden ANC/şeffaflık/Normal, pil, yenileme ve yeniden bağlantı.
- Pil yüzdelerini simge ipucunda gösterme.
- X ve küçült ile gizleme; çift tıkla geri açma.
- Çıkış ile tam kapanma ve kaynak temizliği.
- Gizliyken pil yenilemesine devam etme.
- Bağlantısız/işlemde menü öğelerinin devre dışı bırakılması.
- Tepsi ve pencere yaşam döngüsü otomatik testi.

Kullanıcı tepsi davranışını olumlu doğruladı.

## 1.4 — Windows ile otomatik başlatma

[Kaynak](../history/v1.4)

- Kullanıcı bazlı Windows Run kaydı.
- İlk normal çalıştırmada varsayılan açık başlangıç tercihi.
- Menüden tercihi kapatma ve sonraki açılışlarda koruma.
- EXE taşınırsa açık başlangıç kaydını güncelleme.
- Otomatik/elle başlatmada doğrudan tepsiye yerleşme.
- --show ile pencereyle açılma.
- Gizli başlatmadan pencereye geri dönüş düzeltmesi.
- Testlerde başlangıç kaydına dokunmama.

Kulaklık başlangıçta kapalıysa daha sonra otomatik yeniden bağlanma henüz tamamlanmamıştı.

## 1.5 — Otomatik BLE yeniden bağlantısı

[Kaynak](../history/v1.5)

- Kulaklık kapalıyken uygulamanın açık kalması.
- Bluetooth bağlantı durumu değişimini izleme.
- Üç saniyelik denetim, başarısız işlem sonrası on beş saniyelik yeniden deneme beklemesi.
- Aynı anda yalnızca tek işlem; kopunca eski pil değerlerini temizleme.
- Tekrarlayan aynı hata için log çoğalmasını azaltma.
- Simüle kopma, tekrar deneme ve eşzamanlılık testleri.

**Bilinen eksik:** kontrol bağlantısı, Windows ses çıkışının hazır olmasını beklemiyordu. Sonraki kullanıcı testinde BLE verileri geri gelirken ses çıkışı gelmeyebildi. Güncel sürüm yerine kullanılmamalıdır.

## 1.5.1 — Bağlantı tanılaması

[Kaynak](../history/v1.5.1)

- START, CONNECT_ATTEMPT, CONNECTED, DISCONNECTED, ERROR, NO_RESPONSE ve EXIT olaylarını connection.log'a kaydetme.
- Geçerli sorgu yanıtından sonra kontrol bağlantısını başarılı sayma.
- Pil ve dinleme durumunu başarı kaydına ekleme.
- 256 KiB üzerinde tek önceki log yedeği.
- Test olaylarını gerçek bağlantı logundan ayırma.

Loglar kontrol kanalının otomatik geri geldiğini gösterdi. Ancak kullanıcı Windows ses çıkışında kulaklığı göremediğini, uygulama kapalıyken aynı kutuya koy/çıkar testinin sesle bağlandığını bildirdi. Bu karşılaştırma sonraki bağlantı sırası düzeltmesini belirledi.

CONNECTED kaydı bu sürümde de sesin duyulduğunu kanıtlamaz; yalnızca BLE kontrol kanalıdır.

## 1.6 — Önce ses, ardından BLE; oyun modu

[Güncel kaynak](../src)

- Windows MMDevice ile Air5 Pro'nun etkin render endpoint'ini salt okunur sorgulama.
- Aynı endpoint en az sekiz saniye kararlı görünmeden BLE açmama.
- Ses çıkışı kaybolunca kontrol kanalını bırakma.
- Kopmada kararlılık ve yeniden deneme beklemesini sıfırlama.
- Elle Bağlan seçeneğinde de aynı ses kapısı.
- Ses çıkışı ile kontrol bağlantısını ayrı gösterme.
- Pencere ve tepsiye oyun modu ekleme: GET 030E, SET 030F 00/01.
- Oyun durumu bilinmiyorsa düğmeyi kapatma; SET sonrası GET ile doğrulama.
- AUDIO_READY, WAIT_AUDIO, GAME_STATE ve GAME_SET tanılama olayları.
- Mevcut pil, üç mod, tepsi, otomatik başlatma ve yerel log işlevlerini koruma.
- EQ/kazanç/firmware komutları eklememe.

**Kullanıcı doğrulaması, 17 Ekim 2026:** Air5 Pro S1_20250410_V2.2 üzerinde kutuya koy/çıkar sonrası ses çıkışı ve BLE bağlantısı geri geldi; video sesi duyuldu; oyun modu açma/kapatma çalıştı. Loglar önce sesin hazır olmasını, yaklaşık dokuz saniye sonra kontrol denemesini ve dört oyun SET/GET eşleşmesini gösterdi.

Bu, tek cihaz/firmware üzerinde doğrulamadır. Varsayılan ses seçimi, duyulan ses ve gerçek gecikme ayrı konulardır; uygulama ses akışı veya gecikme ölçmez.

## İndirme politikası

- Tek etkin çalıştırılabilir sürüm: v1.6 / Releases latest.
- Eski EXE, ZIP veya eski sürümlere indirme bağlantıları eklenmez.
- Eski sürümlerin kaynakları ve açıklamaları yalnızca tarihçe olarak korunur.
- Ara BLE/GAIA/EQ araştırma araçları, APK ve kişisel tanılama logları dağıtıma dahil değildir.
