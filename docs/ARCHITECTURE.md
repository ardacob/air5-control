# Mimari ve bağlantı akışı

## Bileşenler

- src/Air5.cs: başlangıç kaydı, paket sınırları, WinRT BLE, WPF/tepsi kontrolü, zamanlayıcı, tanılama ve gömülü testler.
- src/AudioOutput.cs: salt okunur Windows MMDevice aygıt sorgusu ve ses kararlılık kapısı.
- src/MainWindow.xaml: EXE'ye gömülen WPF arayüzü.
- Derle.ps1: Windows PowerShell 5.1 ve CodeDOM ile derleme.

## Ses kapısı

AudioOutput.Read, render aygıtlarını enumerasyonla okur; akış açmaz veya varsayılan aygıtı değiştirmez. Air5 Pro adına uyan bir aygıt için state=1 (DEVICE_STATE_ACTIVE) aranır. Diğer state değerleri bağlantı için yeterli değildir. Pro+ adı eşleşmeden dışlanır.

AudioGate aynı endpoint kimliğini en az sekiz saniye boyunca hazır görmelidir. Ses kaybı veya endpoint değişimi kararlılık süresini sıfırlar. Kontrol bağlantısının kopması da kararlılık süresini ve tekrar deneme beklemesini sıfırlar; eski aktif ses kaydına bakarak hemen yeniden BLE açılması önlenir.

Bütün kullanıcı bağlantı düğmeleri ve otomatik bağlantı aynı ses kontrolünden geçer. Ses okunamazsa sistem bekler; bu hata BLE'yi doğrudan açmak için gerekçe yapılmaz.

## İşlemler

Zamanlayıcı üç saniyede bir durumu değerlendirir. Tek busy koruması, menü/pencere/zamanlayıcı işlemlerinin çakışmasını engeller. Başarısız bir işlem bittikten sonra yeniden bağlantı için en az on beş saniye beklenir. Bağlıyken pil/mod okuması yaklaşık otuz saniyede bir yapılır.

GATT bildirimleri thread-safe kuyruğa alınır. Yanıtlar vendor ve komutla eşleştirilir. Windows işlemlerine süre sınırı uygulanır. Kapanışta zamanlayıcı durdurulur, olay işleyicileri kaldırılır ve kaynaklar bırakılır. Kullanıcının seçtiği tam çıkış sırasında işlem varsa bitmesi beklenir.

Pencereyi gizlemek bağlantıyı kapatmaz. İlk görünür yüklemeden sonraki pencere açılışları bağlantıyı yeniden başlatmaz.

## Durumların anlamı

- Ses çıkışı hazır: Windows'ta eşleşen etkin bir render endpoint var.
- Kontrol bağlı: BLE sorguları geçerli pil veya mod yanıtı verdi.
- Ses duyuldu: uygulamanın ölçmediği, kullanıcı tarafından doğrulanan durum.
- Oyun modu doğrulandı: SET sonrasında GET istenen durumu döndürdü; gecikme ölçümü değildir.

## Loglar

connection.log, yalnızca anlamlı geçişleri ve bağlantı/oyun işlemlerini kaydeder. Aynı ses durumunun her poll'da yazılması önlenir. Dosya 256 KiB'yi aşarsa tek .previous yedeği tutulur. air5.log ayrıntılı hataları içerir; aynı tekrarlayan hata her yeniden bağlantıda tekrar yazılmaz.

## Testler

--self-test: paketler, izin listesi, yanıt ayrıştırma, oyun modu, ses kapısı, aygıt eşleşmesi, tekrar deneme ve başlangıç komutu.
--tray-test: Bluetooth ve başlangıç kaydını değiştirmeden gizli açılış, tepsi/pencere, ses yokken manuel/otomatik bağlantının engellenmesi, oyun modu menü durumu ve kapanış.
--preview: Bluetooth kullanmadan arayüz render.
--audio-test: salt okunur gerçek ses endpoint sorgusu; sonucu audio-test.txt'ye yazar.

17 Ekim 2026'da kullanıcı v1.6 üzerinde kulaklığı kutuya koyup çıkararak otomatik ses/kontrol bağlantısını, video sesini ve oyun modunu doğruladı. Donanım sonucu tek cihaz/firmware kapsamındadır.

## Windows kaynakları

- [EnumAudioEndpoints](https://learn.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nf-mmdeviceapi-immdeviceenumerator-enumaudioendpoints)
- [Microsoft SDK COM arayüz tanımları](https://github.com/microsoft/win32metadata/blob/main/generation/WinSDK/RecompiledIdlHeaders/um/mmdeviceapi.h)
