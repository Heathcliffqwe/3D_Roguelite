# 3DSimple — Backend Mimari Notları

> Hedef: PoE2 tarzı, **tam online, authoritative ARPG** backend'i.
> Çok sınıf + skill + skill tree + trade (ileride) + ligler (ileride). Anti-cheat şimdilik ertelendi.
> Bu doküman bir **tasarım/karar** dokümanıdır; kodda değişiklik içermez.

---

## 0. Mevcut Durum (kod incelemesinden)

- **Unity 6000.2.2f1 + URP**, Input System + **Joystick Pack** → mobil/touch hedefli (muhtemelen + PC).
- **Tek karakter** var; sınıf sistemi yok. `PlayerScript` sadece `int maxHealth/curHealth` tutuyor, ölünce `Destroy`.
- **Skill sistemi temiz başlamış:** `SkillConfig` (ScriptableObject) + **Strategy pattern** (`ISkillStrategy` → MultiShot, BurnDamage, AttackSpeed, RageMode). `SkillManager` toggle'lıyor, event yayınlıyor.
- **Mimari engeller:**
  - `SkillManager.CreateStrategy` (`SkillManager.cs:12-27`) dev bir `if/else` zinciri — her yeni skill burayı şişirir.
  - Skiller **boolean aç/kapa** mantığında; PoE'deki gibi **stat'a katkı veren modifier sistemi yok**.
  - Stratejiler client component'lerini (`autoAttack`, `bow`) **doğrudan mutasyona** uğratıyor → simulation ve presentation iç içe.
  - Skill tree, sınıf, item/affix, persistence (kayıt), network — hiçbiri yok. Tamamen client-side, tek oturumluk.

---

## 1. Çerçeve: ARPG backend'i FPS netcode'u DEĞİLDİR

PoE / Diablo = **tick-tabanlı, instanced (bölme bazlı), oturum-otoriteli** mimari. Counter-Strike gibi 60Hz sürekli pozisyon senkronu değil. Bu yüzden "Unity multiplayer" araması senin probleminin sadece küçük bir parçasını çözer.

PoE'nin gerçek yapısı:
- **Instanced zone'lar:** Her harita, sen/partin için sunucuda anlık üretilen, birkaç dakika yaşayan ayrı simülasyon instance'ı. Dünya tek parça değil.
- **Authoritative simülasyon:** Düşman, loot, hasar sunucuda hesaplanır; client gösterir + input gönderir.
- **Kalıcı metagame:** Hesap, karakter, envanter, pasif ağaç, stash → merkezi DB.
- **Asenkron trade:** Gerçek zamanlı değil; currency exchange (otomatik emir eşleştirme) + item için oyuncu-oyuncu mesajlaşma.
- **Lig = yeni "shard" + ekonomi reset:** Karakter bir lige aittir; lig bitince Standard'a taşınır.

**Sonuç:** Tek "backend" değil, **3 katman** lazım. Karıştırmak en büyük mimari hatadır.

---

## 2. Doğru cevap: 3 katmanlı mimari

```
┌─────────────────────────────────────────────────────────┐
│  UNITY CLIENT (mevcut projen)                            │
│  - Sadece görsel/input. Otorite YOK.                     │
│  - Simülasyonu ÇALIŞTIRMAZ, sonucu render eder.          │
└───────────────┬──────────────────────┬──────────────────┘
                │ realtime (UDP/WS)     │ HTTPS (REST/gRPC)
                ▼                       ▼
┌──────────────────────────┐  ┌─────────────────────────────┐
│ KATMAN A: GAME / INSTANCE │  │ KATMAN B: META / PLATFORM   │
│ SERVER (authoritative)    │  │ BACKEND (kalıcı dünya)      │
│ - Zone instance simülasyonu│ │ - Hesap / auth              │
│ - Hasar, AI, loot, tick   │  │ - Karakter, envanter, stash │
│ - Geçici (stateless-ish)  │  │ - Pasif ağaç durumu         │
│ - Yatay ölçeklenir        │  │ - Trade, ekonomi, lig       │
└───────────┬───────────────┘  └──────────────┬──────────────┘
            │                                  │
            └──────────────►  KATMAN C: DB  ◄──┘
               (Postgres + Redis)
```

- **Katman A (Game/Instance Server):** "Tick'leyen" otoriter simülasyon. Oyun mantığın burada koşar. Geçici, ölçeklenebilir, çoğunlukla stateless (durumu C'ye yazar).
- **Katman B (Meta Backend):** Kalıcı her şey. Login, karakter, envanter, pasif ağaç, trade, lig. HTTP/REST, gerçek zamanlı değil.
- **Katman C (Veri):** **PostgreSQL** (karakter/item/trade — ilişkisel, kalıcı) + **Redis** (oturum, eşleştirme, currency exchange order book, cache).

Bu ayrım PoE, Diablo, Lost Ark dahil tüm büyük ARPG'lerin ortak iskeletidir. Hangi tech seçilirse seçilsin değişmez.

---

## 3. Teknoloji karşılaştırması

Bağlam: Unity 6 client, mobil + muhtemelen PC, tek geliştirici/küçük ekip, authoritative, trade gerek, ileride lig, anti-cheat şimdilik yok.

| Yaklaşım | Katman A'yı nasıl çözer | Katman B/C | Otorite | İş yükü | Maliyet | Uygunluk |
|---|---|---|---|---|---|---|
| **1. Unity Headless Dedicated Server** (NGO / **FishNet** / Mirror) + ayrı meta backend | Unity'yi `-batchmode` server build alıp zone instance koşturursun | Ayrı yazılır (Nakama/custom) | ✅ Tam | Orta | Orta (instance başına RAM/CPU) | **Güçlü aday.** Oyun mantığın C#; sunucuda aynı kod koşar. FishNet authoritative hazır. |
| **2. Nakama** (Heroic Labs) | Authoritative match handler (Go/TS/Lua) — ama Unity fizik/AI koşturamaz | ✅ Hepsi dahil: hesap, storage, leaderboard, matchmaker, Postgres built-in | ✅ (mantığı Go/TS yazarsan) | Orta | Düşük-orta (self-host bedava) | **Katman B için mükemmel.** Fizik-bazlı zone (A) için zayıf. |
| **3. Custom backend** (Go/.NET/Rust) — GGG'nin yolu | Kendi tick loop'un | Kendin | ✅ Tam | **Çok yüksek** | Düşük | Şu an **overkill.** Tek başına başlama. |
| **4. SpacetimeDB** | DB = sunucu; mantık DB içinde (Rust/C#) | DB'nin kendisi | ✅ | Yüksek (yeni paradigma) | Düşük | İlginç ama **olgunlaşmamış**; fizik/AI yine Unity dışı. Erken risk. |
| **5. Saf BaaS** (PlayFab / Unity Gaming Services) | Dedicated server hosting + matchmaking | ✅ Ekonomi/envanter/auth hazır | Server build authoritative | Düşük-orta | **Lock-in + ölçekte pahalı** | Hızlı başlangıç ama trade/lig özel mantığında sınırlı, kaçması zor. |

**Önemli:** Mirror/FishNet/NGO = Katman A'nın taşıma/senkron kısmı (hesap/trade vermez). Nakama/PlayFab = Katman B. Bunlar **rakip değil, tamamlayıcı.** Doğru soru "FishNet mi Nakama mı?" değil; "**hangisi A'ya, hangisi B'ye?**"

---

## 4. Önerilen tech kombinasyonu

> **Katman A = Unity Headless Server + FishNet** | **Katman B/C = Nakama (self-host) + PostgreSQL/Redis**

**Gerekçe:**
1. **Oyun mantığın zaten Unity/C#.** EnemyScript, AutoAttack, skill strategy'leri, NavMesh AI... Bunları Nakama Go runtime'ında sıfırdan yazmak = oyunu iki kez yapmak. Unity headless server ile **aynı C# kodu** otoriter koşar. FishNet authoritative + delta compression verir.
2. **Nakama, Katman B'nin %80'ini hazır verir:** auth, sosyal, storage (karakter/stash JSON), matchmaker, gömülü Postgres. Trade'in **asenkron** kısmı (currency exchange order book, item listeleme) Nakama storage + RPC üstüne temiz oturur — PoE trade'i zaten realtime değil.
3. **Maliyet/risk:** İkisi de self-host, açık kaynak, lock-in yok. PlayFab gibi ölçekte sıkmaz.
4. **Anti-cheat ertelemesiyle uyumlu:** authoritative mimariyi şimdiden kurarsın (cheat'in temeli); imza/şifreleme/doğrulama detayını faz 3'e bırakırsın.

**Ne zaman sap:** Zone'lar fizik/collision/NavMesh ağırlıklı DEĞİL de saf grid/deterministik mantıksa, Katman A'yı da Nakama match handler'da yazıp Unity'yi tamamen "aptal client" yapabilirsin (tek dil, daha ucuz). Ama mevcut kodun (NavMesh, physics, collision ok atışı) **Unity-server tarafını işaret ediyor.**

---

## 5. Mimari STİL: Clean vs Monolith vs Modular Monolith

**Bunlar aynı eksende değil:**
- **Monolith ↔ Microservices** = *deployment* ekseni (kaç ayrı parça deploy edilir).
- **Clean Architecture** = *kod organizasyonu* ekseni (bağımlılıklar içe akar; iş mantığı framework/DB'den bağımsız).
- **Modular Monolith** = tek deploy edilen birim, ama içi net modüllere bölünmüş.

"Modular monolith" + "clean prensipleri" **çelişmez, birlikte kullanılır.**

### Karar: **Modular Monolith** (+ pragmatik clean, microservices'ten kaç)

1. **Tek geliştirici/küçük ekip + erken faz** → Microservices erken ölüm. Servis keşfi, network hataları, dağıtık transaction = asıl işin değil, altyapı vergisi. PoE bile pratikte monolithik backend olarak büyüdü.
2. **Modüler tut ki sonra ayırabilesin:** `Account`, `Character`, `Inventory`, `Trade`, `League`, `Simulation` ayrı modüller/namespace'ler, net arayüzlerle. Trade gerçekten darboğaz olunca **sadece o modülü** servise çıkarırsın. Baştan microservice yazarsan yanlış yerden bölersin.
3. **Clean'in işine yarayan kısmı Faz 0'da zaten lazım:** "simülasyon mantığı Unity/DB/framework'ten bağımsız saf C# olsun" = clean'in çekirdeği (dependency inversion). Bunu al; ama clean'in aşırı katman/abstraction dogmasına (her şeye interface, 5 katman) **girme** — küçük ekipte üretkenliği öldürür.

**Net formül:** *Modular monolith deployment + içeride pragmatik clean (özellikle simülasyon çekirdeğini izole et) + microservices'i sadece kanıtlanmış darboğazda, modül modül.*

> Not: Katman A (Unity game server) ve Katman B (Nakama meta) zaten **iki ayrı process** — ama bu microservices değil, fiziksel zorunluluk (biri Unity runtime, biri backend). Her birinin **kendi içi** modular monolith olmalı.

---

## 6. ÖNCE bunu yap (sunucudan önceki asıl mesele)

En kritik engel: **skiller client component'lerini doğrudan mutasyona uğratıyor** (`SkillManager.cs:12-27`). Simülasyon ve presentation iç içe → authoritative sunucuya olduğu gibi taşınamaz (sunucuda `SkillButonUi`, `Sprite`, `Image` yok).

PoE2 "mantığı" teknik olarak iki eksik parça:

### (a) Stat / Modifier motoru
Şu an skiller boolean aç/kapa. PoE'de her şey stat'a **katkı** verir: `+%20 attack speed (increased)`, `+15 fire damage (added)`, `more/less` çarpanları.

```
FinalStat = (Base + Σ Added) × (1 + Σ Increased) × Π (More)
```

Düşman, item, pasif ağaç, skill — hepsi bu motora "modifier" basar; motor tek yerden hesaplar. `CreateStrategy` if/else zinciri yerine **data-driven modifier listesi**.

### (b) Presentation ↔ Simulation ayrımı
`PlayerScript`, `SkillManager`, `EnemyScript`'i **saf C# mantık sınıfları** (MonoBehaviour değil, UnityEngine'e bağımsız) + ince "view" katmanına böl. Bu saf mantık katmanı ileride birebir sunucuda koşar.

**Bu refactor olmadan hangi backend seçilirse seçilsin entegre edilemez.** İyi haber: mevcut Strategy + ScriptableObject yapın iyi başlangıç; config'leri "davranış" yerine "data/modifier" taşıyacak şekilde evirip Unity bağımlılığını mantıktan sökmek gerek.

---

## 7. Faz faz yol haritası

| Faz | Hedef | İş |
|---|---|---|
| **0 — Şimdi** | Mimari temel | Simulation/Presentation ayrımı + Stat/Modifier motoru + sınıf & skill tree'yi data-driven kur (ScriptableObject → ileride JSON/DB). Hâlâ tek oyunculu. |
| **1 — Persistence** | Bulut kayıt | Nakama kur (Docker). Auth + karakter/envanter/pasif ağaç JSON olarak Nakama storage'da. Client hâlâ otoriter ama veri bulutta. |
| **2 — Authoritative instance** | Otorite sunucuya geçer | Unity headless server build + FishNet. Zone girişinde Nakama matchmaker instance ayırır; simülasyon sunucuda koşar, client input gönderir. Faz 0'daki saf mantık katmanı burada çalışır. |
| **3 — Trade + güvenlik** | Ekonomi | Asenkron trade: Nakama RPC + storage ile item listeleme; currency exchange için Redis order book. Anti-cheat sertleştirme (artık authoritative). |
| **4 — Ligler** | Sezonlar | Lig = ayrı veri partisyonu (Postgres şema/tag) + reset/migrate job'ı. Karakter `league_id`ye bağlanır; lig bitince Standard'a taşıyan batch. |

---

## 8. Özet

- Tek "backend" değil, **3 katman**: realtime instance (A) + kalıcı meta (B) + DB (C).
- **Tech önerisi:** A = Unity headless + **FishNet**, B/C = **Nakama + Postgres/Redis**. Oyun mantığın C#, iki kez yazma; Nakama meta'nın çoğunu hazır verir; open-source/self-host, lock-in yok.
- **Mimari stil:** **Modular monolith** + pragmatik clean (simülasyon çekirdeğini izole et). Microservices'ten erken fazda kaç; modül sınırlarını net tut ki sonra darboğazı ayırabil.
- **Sunucudan önce** kodda iki şey şart: **simulation/presentation ayrımı** ve **stat/modifier motoru** (= asıl "PoE2 mantığı").
- **İlk somut adım:** Faz 0 — sunucu kurmadan, tek oyunculu haldeyken mantığı Unity'den ayır, data-driven stat motorunu kur. Backend bunun üstüne oturur.

---

## Kaynaklar
- Unity Netcode (NGO): https://unity.com/features/netcode
- FishNet (OVG): https://oceanviewgames.co.uk/technologies/fishnet
- Multiplayer Networking Resources: https://multiplayernetworking.com/
- Nakama vs Photon (Heroic Labs): https://heroiclabs.com/comparison/photon/
- Nakama Unity Client: https://heroiclabs.com/docs/nakama/client-libraries/unity/
- Real-Time Game Backends 2026: https://namazustudios.com/best-real-time-game-backends/
- PoE Area/Instances (PoE Wiki): https://www.poewiki.net/wiki/Area
- PoE Leagues (PoE Wiki): https://www.poewiki.net/wiki/League
- PoE Trading Guide (Odealo): https://odealo.com/articles/path-of-exile-in-depth-trading-guide
