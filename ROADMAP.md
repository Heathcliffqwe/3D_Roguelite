# 3DSimple — Yol Haritası (Roadmap)

> Genel gidişat dokümanı. Tartışmak ve önceliklendirmek için yazıldı; takvim değil, **sıra ve mantık** belgesidir.
> İlgili dokümanlar: oyun tasarımı → **GDD.md**, backend mimarisi → **BACKEND_ARCHITECTURE.md**.

---

## Alınan Kararlar (sabitler)

- **Hedef:** PoE2 tarzı, tam online, **authoritative** ARPG. Trade var; ligler sonraki fazlarda. Anti-cheat şimdilik ertelendi.
- **Backend'i kendin yazacaksın** (Nakama/BaaS değil). Sen zaten n-layer web API geliştiriyorsun → meta backend senin konfor alanın.
- **Mimari stil: Modular Monolith** (microservices değil) + pragmatik clean prensipleri (özellikle simülasyon çekirdeğini izole et).
- **3 katman gerçeği:** Realtime/Instance server (A) + Meta backend (B) + DB (C). Bunlar karışmaz.

---

## Temel Strateji: "Önce çekirdek, sonra online"

Sunucu kurmak ilk adım DEĞİL. Çünkü authoritative sunucuya taşıyacağın **oyun mantığı henüz Unity component'lerine yapışık** ve **stat motoru yok**. O yüzden sıra:

```
Faz 0  Oyun mantığını Unity'den ayır + Stat motoru kur   (hâlâ offline, tek oyunculu)
Faz 1  Meta backend = kendi .NET Web API'n + DB           (online'ın TANIDIK yarısı)
Faz 2  Authoritative realtime instance server             (online'ın YENİ yarısı)
Faz 3  Trade + ekonomi + güvenlik sertleştirme
Faz 4  Ligler / sezonlar
```

Her faz **kendi başına oynanabilir/test edilebilir** bir şey bırakır; büyük patlama (big bang) yok.

---

## Faz 0 — Çekirdek Refactor + Stat Motoru
**Amaç:** Backend'e taşınabilir, derinleşebilir bir oyun çekirdeği. Hâlâ offline, tek oyunculu.
**Neden önce:** Bu olmadan ne sınıf, ne skill tree, ne authoritative sunucu mümkün.

İşler:
1. **Simulation / Presentation ayrımı**
   - Oyun kuralları (can, hasar, skill etkisi, AI kararı) → **saf C# sınıfları** (MonoBehaviour değil, UnityEngine'siz).
   - MonoBehaviour'lar ince "view/controller" olur: input alır, çekirdeği çağırır, sonucu render eder.
   - Hedef: `PlayerScript`, `EnemyScript`, `SkillManager` mantığı çekirdeğe taşınsın.
2. **Stat / Modifier motoru** (`FinalStat = (Base + ΣAdded) × (1 + ΣIncreased) × Π More`)
   - Tüm güç hesabı buradan geçer.
   - Mevcut skiller (MultiShot/Burn/AttackSpeed/Rage) **modifier üreten** yapıya çevrilir; `arrowCount +=` gibi doğrudan mutasyon kalkar.
   - `SkillManager.CreateStrategy` if/else zinciri → data-driven kayıt/registry.
3. **Modüler sınır taslağı** (kod içi): `Combat`, `Skills`, `Stats`, `Character`, `Loot` namespace/asmdef'leri net ayrılır.
4. **Sınıf & ağaç sistemini data-driven kur** (çok-sınıflı vizyon gereği):
   - Sınıf = veri (skill listesi + skill ağacı grafiği + stat profili), kod değil. Yeni sınıf (warrior/mage/necromancer...) eklemek **veri eklemek** olmalı.
   - **Sınıf-başı skill ağacı** önce; **paylaşımlı PoE-tarzı pasif ağaç** sonraya bırakılır ama aynı modifier motoruna basacağı için sonradan veri olarak eklenir (bkz. GDD 4.3).
   - Mevcut tek-archer kodu burada büyük ölçüde yerini bırakır; modifier mantığı fikri korunur.

**Çıktı:** Çok sınıflı, modifier tabanlı, veri-driven çekirdek. Sınıf/skill tree/item artık **kod değil veri** olarak eklenir.
**Bağımlılık:** Yok. Hemen başlanabilir.

---

## Faz 1 — Meta Backend (kendi .NET Web API'n) + Persistence
**Amaç:** Oyun "online" olur: hesap aç, karakterini buluta kaydet/yükle. **Bu faz birebir senin bildiğin web API.**

İşler:
1. **Modular monolith .NET backend** iskeleti. Modüller (ayrı project/namespace, net arayüz):
   - `Account` (kayıt/login, JWT)
   - `Character` (oluştur/listele/sil, sınıf, seviye, stat)
   - `Inventory` (item/envanter/stash)
   - `PassiveTree` (alınan düğümler)
   - (sonraki fazlar için yer: `Trade`, `League`)
2. **DB:** PostgreSQL (veya alıştığın SQL Server). İlişkisel şema: Account, Character, Item, PassiveAllocation...
3. **API:** REST endpoint'leri (`POST /auth/login`, `GET /characters`, `PUT /characters/{id}/inventory`...).
4. **Unity client entegrasyonu:** `UnityWebRequest` ile login + karakter kaydet/yükle.

**Önemli:** Bu fazda oyun **hâlâ client-otoriter** (cheat mümkün) — sorun değil, önce veri akışını kur. Authoritative Faz 2'de gelir.
**Çıktı:** Telefonunu değiştir, karakterin bulutta durur. Gerçek anlamda ilk "online" adım.
**Bağımlılık:** Faz 0'ın veri modeli (stat/karakter yapısı netleşmiş olmalı).

---

## Faz 2 — Authoritative Realtime Instance Server
**Amaç:** Asıl "oyun sunucusu". Otorite client'tan alınır; hasar/AI/loot sunucuda hesaplanır. **Bu fazın paradigması yeni** (request/response değil, kalıcı bağlantı + tick loop).

İşler:
1. **Instance server**: bir zone'u sunucuda simüle eden process.
   - Seçenek A: **Unity headless server build (`-batchmode`) + FishNet** → Faz 0 çekirdeğin sunucuda birebir koşar (önerilen, çünkü mantığın C#).
   - Seçenek B: zone mantığı saf/deterministikse → kendi .NET realtime server'ında koştur (Unity'siz). Faz 0 çekirdeği UnityEngine'siz olduğu için bu da mümkün.
   - **Karar noktası:** Faz 2 başında ver; fizik/NavMesh bağımlılığına göre.
2. **Bağlantı:** kalıcı WebSocket/UDP; client input gönderir, sunucu state push eder.
3. **Matchmaking/instance ayırma:** "zone'a gir" → meta backend bir instance ayırır → client ona bağlanır.
4. Client artık "aptal terminal": gösterir + input yollar.

**Çıktı:** Otoriter, cheat'e dayanıklı temel. Co-op'un da kapısı (aynı instance'a 2+ oyuncu).
**Bağımlılık:** Faz 0 (izole çekirdek) + Faz 1 (hesap/instance ayırma).

---

## Faz 3 — Trade + Ekonomi + Güvenlik
**Amaç:** Oyuncular arası ekonomi. Asenkron (PoE modeli), realtime değil.

İşler:
1. **`Trade` modülü** (meta backend içinde): item listeleme, arama, oyuncu-oyuncu takas onayı.
2. **Currency exchange**: otomatik emir eşleştirme (order book) — **Redis** uygun.
3. **Güvenlik sertleştirme**: artık authoritative olduğun için item dupe/ekonomi exploit'lerine karşı sunucu doğrulamaları; anti-cheat'in ertelenen kısmı buraya.

**Çıktı:** Yaşayan ekonomi.
**Bağımlılık:** Faz 2 (authoritative item sahipliği güvenli olmalı, yoksa trade dupe cenneti olur).

---

## Faz 4 — Ligler / Sezonlar
**Amaç:** Sezonluk tazelik, ekonomi reset, yeni mekanik.

İşler:
1. **`League` modülü**: karakter bir `league_id`ye bağlı; ligler izole (Standard kalıcı, sezonluk geçici).
2. **Reset/migrate job**: lig bitince karakter/item'ları Standard'a taşıyan batch.
3. Lig-özel içerik bayrakları (yeni mekanik sadece o ligde aktif).

**Çıktı:** Tekrar oynanabilirlik motoru.
**Bağımlılık:** Faz 1-3 stabil (hesap, item, trade, ekonomi).

---

## Görsel Özet

```
Faz 0 ─ Çekirdek + Stat motoru ............. OFFLINE,  bağımlılık yok        ← HEMEN
Faz 1 ─ .NET Web API + DB (persistence) .... ONLINE-meta, tanıdık dünya
Faz 2 ─ Authoritative instance server ...... ONLINE-realtime, yeni dünya
Faz 3 ─ Trade + ekonomi + güvenlik ......... Faz 2 üstüne
Faz 4 ─ Ligler / sezonlar .................. Faz 1-3 üstüne
```

---

## Tartışılacak Açık Sorular (sonraki sohbet için)

1. **Faz 2 sunucu seçimi:** Unity headless + FishNet mi, yoksa zone mantığını tam saf C# yapıp kendi .NET realtime server'ında mı koşturalım? (Mevcut kod NavMesh/physics kullanıyor → bu kararı etkiler.)
2. **Co-op kapsamı:** Faz 2'de baştan çok-oyunculu instance mı, yoksa önce tek-oyunculu-authoritative mi?
3. **Mobil + realtime:** mobil ağda UDP/WebSocket tercihi ve tick rate hedefi.
4. **Faz 0 derinliği:** stat motorunu en baştan ne kadar PoE-derinliğinde (More/Increased/tag'li modifier) kuralım, yoksa minimal başlayıp mı büyütelim?
4b. **Sınıf kapsamı:** Faz 0'da kaç sınıfla başlayalım (önce 1-2 sınıfla sistemi kanıtla, sonra çoğalt mı)? Sınıf-başı ağaç ile global PoE ağacını ne zaman ayıralım?
5. **DB:** PostgreSQL mi, alıştığın SQL Server mı? (Trade order book için Redis ne zaman girsin.)

> Not: Bu yol haritası "her fazı bitirmeden diğerine geçme" demek değil. Faz 0 sağlam bitmeli (temel), ama Faz 1 ve sonrası iteratif/paralel ilerleyebilir.
