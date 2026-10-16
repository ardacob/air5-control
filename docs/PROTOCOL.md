# Protokol notları

Bu belge Air5 Pro üzerinde doğrulanan kısa BLE çerçevesini ve v1.6'nın izin verdiği komutları açıklar. Başka SOUNDPEATS modelleri için uyumluluk iddiası değildir.

## BLE kanalı

| Öğe | UUID |
| --- | --- |
| Servis | 00001100-d102-11e1-9b23-00025b00a5a5 |
| Yazma | 00001101-d102-11e1-9b23-00025b00a5a5 |
| Bildirim | 00001102-d102-11e1-9b23-00025b00a5a5 |

WriteWithResponse ve bildirim aboneliği kullanılır. BLE paketlerinin başına FF veya SPP uzunluk başlığı eklenmez.

İstek: vendor (00 0A), komut (03 xx), varsa yük.
Sorgu yanıtı: vendor (00 0A), yanıt komutu (83 xx), durum, yük.
Başarı için durum baytı 00 olmalıdır. GATT yazmasının başarılı olması ayarın uygulandığını kanıtlamaz.

## İzin verilen komutlar

| İşlev | GET | SET |
| --- | --- | --- |
| Sol pil | 00 0A 03 06 | Yok |
| Sağ pil | 00 0A 03 07 | Yok |
| Firmware | 00 0A 03 09 | Yok |
| Oyun modu | 00 0A 03 0E | 00 0A 03 0F ss |
| Dinleme modu | 00 0A 03 10 | 00 0A 03 11 mm |
| Kutu pili | 00 0A 03 23 | Yok |

ss: 00 kapalı, 01 açık.
mm: 00 Normal, 01 ANC, 02 şeffaflık.

Örnekler:

~~~text
00 0A 83 06 00 5A  => sol pil %90
00 0A 83 0E 00 01  => oyun modu açık
00 0A 83 10 00 02  => şeffaflık
~~~

Pil ve skaler mod yanıtları tam altı bayt olmalı, doğru vendor/komut taşımalı ve durum kodu 00 olmalıdır. Pil yalnızca 0–100 aralığında kabul edilir; FF gibi bilinmeyen değerler yüzdeye çevrilmez. Dinleme modu 0–2, oyun modu 0–1 aralığında kabul edilir.

Firmware yanıtının başarı durumundan sonraki ASCII yükü gösterilir.

## Yazma ve doğrulama

SET bir kez gönderilir. Yaklaşık 450 ms sonra ilgili GET ile gerçek durum okunur. Geri okunan durum hedefle eşleşmiyorsa başarı gösterilmez. Bir GET yanıtı alınamazsa en fazla iki sorgu denenir.

Earbuds.Validate, izinli paket şekli ve değer aralıkları dışındaki yazmaları engeller. EQ, kazanç, reset, codec veya firmware komutları bu uygulamanın izin listesinde yoktur.

## Kaynaklar ve doğrulama kapsamı

- Kullanıcının Air5 Pro'su: S1_20250410_V2.2.
- PeatsAudio'nun incelenen Android sürümündeki FunctionEnum / FunctionStatusEnum: oyun GET 030E, SET 030F, kapalı 00, açık 01; dinleme GET 0310, SET 0311.
- Air5 Pro'da pil ve dinleme modları kullanıcı tarafından doğrulandı; v1.6'da oyun SET/GET eşleşmeleri ve duyulan oyun modu etkisi de doğrulandı.
- Genel karşılaştırma: [OpenPeats protokol notları](https://github.com/Wasabules/OpenPeats/blob/main/PROTOCOL.md). H3 veya farklı modellerdeki komut/yanıtlar Air5 Pro için doğrudan başarı kanıtı sayılmamalıdır.

APK, decompile edilmiş üçüncü taraf uygulama kodu veya firmware bu depoda dağıtılmaz.

## EQ neden yok?

Önceki araştırmalarda farklı GAIA paketleriyle EQ kontrolü güvenilir biçimde doğrulanamadı. Adaptif EQ profilinin okunup geri yüklenmesi de sağlanmadı. Bu nedenle son uygulama EQ üzerinde işlem yapmaz; modlar arası ses farkını EQ ile telafi etmeye çalışmaz.
