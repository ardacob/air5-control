# Sorun giderme

## Uygulama açıldı ama pencere yok

Normal davranış: sistem tepsisinde açılır. Gizli simgeleri genişlet, Air5 Control simgesine çift tıkla. Tam kapatma için tepsi menüsündeki Çıkış'ı kullan.

## Ses bağlantısı bekleniyor

Windows Air5 Pro'yu etkin ses çıkışı olarak henüz göstermiyor. Uygulama bu durumda BLE bağlantısı açmaz. Kulaklığı Windows'ta eşleştir, açık ve bilgisayara ses için bağlı olduğundan emin ol. Başka telefona bağlıysa test sırasında telefonun Bluetooth'unu geçici kapatıp karşılaştırabilirsin.

Ses çıkışı geldikten sonra kararlılık ve yeniden deneme süreleri nedeniyle pil göstergesi biraz daha geç gelebilir. Art arda Baglan'a basmak bu beklemeyi atlatmaz.

## Ses çıkışı var ama video sessiz

Windows Ses ayarlarında veya ses karıştırıcısında doğru çıkışın seçili olduğunu ve sesin kapalı olmadığını kontrol et. Uygulama varsayılan ses çıkışını seçmez. Eski Air5 Control sürümlerinin kapalı olduğundan emin ol. Özellikle v1.5/v1.5.1, ses hazır olmadan BLE bağlantısı açabiliyordu.

## Pil var ama ses bağlantısı yok

v1.6, ses çıkışı kaybolunca kontrol kanalını bırakır. Uygulama sürümünü kontrol et. connection.log içindeki WAIT_AUDIO / AUDIO_READY / CONNECTED sırasını incele. CONNECTED sadece kontrol kanalını ifade eder. Windows durumu ile duyulan ses hâlâ çelişiyorsa yeniden üretme adımlarını bir issue'da paylaş.

## Oyun modu etkinleşmiyor

Geçerli 0/1 durum yanıtı alınmadan düğme etkinleşmez. connection.log içindeki GAME_STATE kaydına bak. Hata kodu veya yanıt yokluğu başarı değildir. Başka firmware'de komut uyumluluğu garanti edilmez.

## Kutu pili görünmüyor

Cihaz bu bağlantıda değer sunmamış olabilir. Tire bilinmeyen demektir. Kutu pili bağımsız olarak kutudan canlı okunmaz.

## Normal'de bas azalıyor

Bu fark test edilen cihazda dokunmatik mod geçişinde de duyuldu. Bu uygulama EQ'yu yeniden yazmaz ve modlar arası ses farkını telafi etmez.

## Windows ile açılmıyor

Windows ile başlat menü öğesinin işaretli olduğundan emin ol. EXE taşındıysa yeni konumdan bir kez çalıştır. Windows Başlangıç uygulamaları bölümünde ayrıca devre dışı bırakılmadığını kontrol et. Yönetici olarak çalıştırmak gerekmez.

## Kaynak derleme hatası

Windows PowerShell 5.1 kullan; pwsh yerine powershell.exe. .NET Framework 4.8/WPF ve Windows System32/WinMetadata referansları gereklidir. src içindeki iki C# dosyası, XAML, simge ve manifest yerinde olmalıdır.

## Log paylaşımı

EXE'nin yanındaki connection.log ve gerekirse air5.log kullanılır. Paylaşmadan önce dosya yolları ve kişisel bilgileri incele. Tam sistem dökümü, hesap bilgileri veya APK ekleme.
