# 3DSimple — Game Design Document (GDD)

> Yaşayan doküman. Oyun büyüdükçe güncellenir.
> Durum: erken prototip → hedef: **PoE2 tarzı online ARPG.**
> Son güncelleme bağlamı: mevcut kod tabanı incelenerek yazıldı (tek archer karakter, mobil, toggle skill sistemi).

---

## 1. Vizyon / Pitch

**Çok sınıflı, derin skill ve pasif ağaç sistemine sahip, online ve authoritative bir aksiyon RPG (ARPG).**
İlham kaynağı **Path of Exile 2**: instanced haritalar, loot avı, modifier-tabanlı build derinliği, asenkron trade ve sezonluk ligler. Platform öncelikli olarak **mobil (touch/joystick)**, ileride PC düşünülebilir.

**Tek cümlelik öz:** "Cebinde taşıdığın, build derinliği olan, online bir PoE."

---

## 2. Tasarım Sütunları (Design Pillars)

1. **Build derinliği** — Oyuncu gücü tek bir sayıdan değil, üst üste binen **modifier'lardan** (skill + pasif ağaç + item) gelir. "Sonsuz kombinasyon" hissi.
2. **Loot avı** — Öldür → düşür → güçlen döngüsü. Item'lar rastgele affix'lerle gelir.
3. **Authoritative & adil** — Kritik her şey sunucuda hesaplanır; ekonomi ve ilerleme güvenilir.
4. **Sezonluk tazelik** — Ligler her sezon ekonomiyi sıfırlar, yeni mekanik getirir, oyuncuyu geri çağırır.
5. **Mobil-dostu kontrol** — Tek elle oynanabilir: joystick hareket + dur-vur otomatik saldırı + skill butonları.

---

## 3. Mevcut Durum (tek-kullanımlık prototip referansı)

> ⚠️ **Bu bölüm "temel" değil, sadece bugün ne çalıştığının kaydı.** Mevcut kod büyük ölçüde
> değişecek/atılacak (tek archer karakter → çok sınıflı sistem). Buraya bakmanın amacı "neyi
> yeniden kullanırız / neyi atarız"ı görmek; tasarımı buradan türetmiyoruz, **vizyondan (Bölüm 4)
> türetiyoruz.** Yeniden kullanılabilir olan tek şey: modifier'a çevrilecek skill mantığı fikri
> ve genel savaş/AI akışı. Geri kalan (tek sınıf, toggle skiller) yerini bırakacak.

### 3.1 Karakter & Kontrol
- Tek karakter: **Archer (okçu)**. Sınıf seçimi henüz yok.
- Hareket: **FixedJoystick** ile (`PlayerMovementScript`), Rigidbody tabanlı, yön dönüşü Slerp.
- Can: `PlayerScript` — `int maxHealth/curHealth`. Ölünce `Destroy` (henüz respawn/checkpoint yok).

### 3.2 Savaş
- **Otomatik saldırı** (`AutoAttackScript`): Oyuncu **dururken** (hareket etmiyorken) en yakın düşmana döner ve ok atar.
  - `arrowCount` (aynı anda kaç ok), `attackCooldown`, `attackRange`.
  - Çoklu ok açısal yelpaze (her ok ~10° offset) ile atılır.
- **Ok** (`BowScript`): ileri hareket eder, düşmana çarpınca `damage` uygular, isteğe bağlı **yanma (burn DoT)** ekler, sonra yok olur. 5 sn sonra otomatik silinir.

### 3.3 Düşmanlar
- `EnemyScript`: **NavMeshAgent** ile oyuncuyu kovalar, menzile girince melee vurur (`attackDamage`, `attackCooldown`), can barı (`EnemyHbUI`), **yanma** alabilir.
- Birden fazla düşman tipi (`defaultenemies[]`).
- **Boss**: süre dolunca (`bossSpawnTime`) spawn olur.
- `EnemyManager`: aynı anda max 5 düşman tutar, eksildikçe spawn eder; boss zamanı gelince boss çağırır.
- `Moderator` (Bossroom): ayrı boss arena sahnesi — boss + oyuncu spawn eder.

### 3.4 Skill Sistemi
- `SkillConfig` (ScriptableObject) + **Strategy pattern** (`ISkillStrategy`).
- Mevcut skiller (hepsi **aç/kapa toggle**):
  - **MultiShot** — `arrowCount`'a ekstra ok ekler.
  - **BurnDamage** — oka yanma ekler (süre + tick hasarı).
  - **AttackSpeed** — saldırı hızını çarpar.
  - **RageMode** — diğer aktif skillerin etkisini `rageMultiplier` ile çarpar (meta-skill).
- `SkillButonUi`: buton durumları (ready / active / disabled-cooldown).

### 3.5 Henüz OLMAYAN (ama vizyonda olan)
Sınıf sistemi · skill tree (pasif ağaç) · item/envanter/affix · stat/modifier motoru · seviye/XP · persistence (kayıt) · hesap/login · online/multiplayer · trade · lig · respawn/checkpoint.

---

## 4. Hedef Tasarım (nereye gidiyor)

### 4.1 Sınıflar (Classes) — oyunun merkezi
Oyun **çok sınıflı** kurgulanıyor. Planlanan sınıflar (genişleyebilir):
- **Kılıç (Warrior / melee)** — yakın dövüş, dayanıklılık.
- **Mızrak (Spear / lancer)** — menzilli melee, isabet/penetrasyon.
- **Büyücü (Mage)** — elemental büyü, alan hasarı.
- **Necromancer** — minion/summon odaklı (kendi yaratıklarını yönetir).
- (Archer/Ranger mevcut prototipten evrilebilir.)

Her sınıfın **kendine ait** olanları:
1. **Kendi skill seti** — sınıfa özgü aktif/destek yetenekler (büyücünün fireball'u necromancer'da yok).
2. **Kendi skill ağacı** — bkz. 4.3. Her sınıfın bağımsız ağacı var.
3. Başlangıç stat profili (str/dex/int benzeri) ve oynanış kimliği (necromancer minion yönetir, warrior yüze girer...).

> Tasarım sonucu: sınıf sadece "skin" değil, **ayrı veri setleri** (skill listesi + ağaç + stat profili). Sistem bunları **data-driven** taşımalı ki yeni sınıf eklemek kod değil veri işi olsun.

### 4.2 Skill Sistemi (hedef) — toggle'dan modifier'a
Mevcut "aç/kapa" yeterli değil. Hedef model:
- Her skill bir **aktif yetenek** (kullanılan: MultiShot atışı) ya da **destek/pasif** (stat veren) olabilir.
- Tüm etkiler **stat motoruna modifier** olarak gider (bkz. 4.4). Skill artık `arrowCount += x` diye component'i doğrudan değiştirmez; "arrowCount stat'ına +x added modifier" basar.

### 4.3 Skill Ağaçları — iki katmanlı plan
Bu oyunda **iki ayrı ağaç kavramı** var; karıştırılmamalı:

**(a) Sınıf-başı Skill Ağacı (önce bu — temel)**
- **Her sınıfın KENDİ bağımsız skill ağacı** var (warrior ağacı ≠ mage ağacı).
- Düğümler: sınıfa özgü skill kilidi açma + o sınıfın stat'larına **modifier** (`+%10 melee damage`, `+2 minion`...).
- Aralarda **notable/keystone** güçlü düğümler.
- Karakter seviyesi başına puan → kendi sınıf ağacında harcanır.
- Veri-driven: her ağaç bir **graph** (düğüm id, komşular, verdiği modifier/skill).

**(b) Paylaşımlı PoE-tarzı Pasif Ağaç (sonra — opsiyonel, "işler o raddeye gelirse")**
- Tüm sınıfların paylaştığı **tek büyük harita**; sınıflar farklı noktadan girer.
- Çoğunlukla genel modifier (`+%8 attack speed`, `+20 life`...) + uzaktaki notable/keystone'lar.
- Sınıf-başı ağacın **üstüne** gelen ekstra derinlik katmanı; sınıf kimliğini bozmadan melezleşme sağlar.
- **Faz/öncelik:** (a) çekirdeğin parçası; (b) ileri faz, ama stat motoru (a) ile aynı modifier sistemini kullandığı için (b) sonradan **kod değil veri** olarak eklenir.

> Mimari avantaj: ister sınıf ağacı, ister global pasif ağaç, ister item, ister skill — **hepsi aynı stat/modifier motoruna** (4.4) modifier basar. Motoru bir kez doğru kurarsan, kaç ağaç olduğu önemli olmaz.

### 4.4 Stat / Modifier Motoru (oyunun kalbi)
Tüm güç hesabı tek formülden geçer:
```
FinalStat = (Base + Σ Added) × (1 + Σ Increased) × Π (More/Less)
```
- Kaynaklar: sınıf bazı + pasif ağaç + item affix'leri + aktif skiller + geçici buff'lar (RageMode gibi).
- Bu motor **UnityEngine'den bağımsız saf C#** olmalı (ileride sunucuda birebir koşacak).
- `RageMode`'un şu anki "config değerini kalıcı çarpıp bölme" yaklaşımı (bkz. `RageModeStrategy`) **buggy ve geçici**; modifier motorunda "geçici More çarpanı" olarak temiz çözülür.

### 4.5 Item & Loot
- Item tipleri (silah/zırh/aksesuar) + **rastgele affix** (prefix/suffix) → her drop farklı.
- Rarity katmanları (normal/magic/rare/unique benzeri).
- Envanter + stash (depo).

### 4.6 Progression
- XP & seviye → pasif puanı + can/stat artışı.
- Zone/akt ilerlemesi; ileride endgame (harita sistemi).

### 4.7 Online (ARPG hedefi)
- **Instanced zone'lar**: her harita oyuncuya/partiye özel, sunucuda üretilen geçici instance.
- **Authoritative simülasyon**: hasar/loot/AI sunucuda.
- **Asenkron trade**: currency exchange (otomatik emir eşleştirme) + item için listeleme/mesajlaşma. Realtime değil.
- **Ligler**: sezonluk, ekonomi reset, lig bitince karakter "Standard"a taşınır.

> Online mimarinin teknik detayları ayrı dokümanda: **BACKEND_ARCHITECTURE.md**.
> Genel sıra ve öncelikler: **ROADMAP.md**.

---

## 5. Çekirdek Oyun Döngüsü

**Anlık (saniyeler):** hareket et → dur → otomatik vur / skill kullan → düşman öldür → loot.
**Oturum (dakikalar):** zone temizle → boss → ödül → level/puan → build güçlendir.
**Meta (günler/sezon):** karakter geliştir → trade ile optimize et → endgame → yeni lig.

---

## 6. Platform & Teknik Çerçeve

- **Motor:** Unity 6000.2.2f1, URP.
- **Girdi:** Unity Input System + Joystick Pack (mobil).
- **Hedef:** mobil öncelikli (touch), PC opsiyonel.
- **Backend (planlanan):** kendi yazılan **modular monolith** sunucu (web API dünyası, .NET) + ileride authoritative realtime katmanı. Detay: BACKEND_ARCHITECTURE.md.

---

## 7. Tasarımın Backend'e Etkisi (kritik notlar)

- **Stat motoru olmadan** sınıf/skill tree/item derinliği yazılamaz; her şey ona modifier basar → **ilk inşa edilecek sistem budur.**
- Skiller component'i doğrudan mutasyona uğratmamalı (mevcut `MultiShotStrategy.arrowCount +=` gibi) → authoritative sunucuya taşınamaz. Modifier'a çevrilmeli.
- Simülasyon mantığı **MonoBehaviour'dan ayrılmalı** (saf C# çekirdek + ince Unity "view"). Bu çekirdek ileride sunucuda koşar.
- Loot/trade/lig → **server-authoritative** olmak zorunda (ekonomi güvenliği).

---

## 8. Sözlük

- **Modifier:** bir stat'a katkı (Added / Increased / More). Build derinliğinin atomu.
- **Instance:** bir zone'un oyuncuya özel, geçici, sunucuda üretilen kopyası.
- **Authoritative server:** oyun kurallarını client'a güvenmeden sunucuda işleten yapı.
- **Lig (League):** sezonluk, izole ekonomili oyun dünyası.
- **Affix:** item üzerindeki rastgele modifier (prefix/suffix).
