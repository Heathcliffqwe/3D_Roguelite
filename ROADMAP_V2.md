# 3DSimple — Detaylı Yol Haritası v2

> Önceki ROADMAP.md'nin somut, adım adım versiyonu. Her alt-faz tek başına çalışır/test edilebilir bir şey bırakır. "Yarın ne yapacağım" sorusuna cevap verir.
> 
> İlgili dokümanlar: GDD.md · BACKEND_ARCHITECTURE.md · ARCHITECTURE_LAYERS.md · MODULE_BOUNDARIES_FAZ0.md

---

## Faz 0 — Çekirdek Refactor + Stat Motoru (OFFLINE, tek oyunculu)

> **Neden önce:** Bu olmadan ne sınıf, ne skill tree, ne authoritative sunucu mümkün. Mevcut kod Unity component'lerine yapışık — sunucuya taşınamaz.

### Faz 0a — Stat/Modifier Motoru (1-2 hafta)

**Amaç:** Oyunun kalbini kur. Tüm güç hesabı tek yerden geçsin.

1. `Assets/_Game/Core/Stats/` klasörünü oluştur, Core asmdef'i yarat (UnityEngine referansı YOK)
2. `ModType` enum'unu yaz: `Added`, `Increased`, `More`
3. `Modifier` struct/record'unu yaz: hangi stat'a, ne tip katkı, ne değer, kim ekledi (Source)
4. `StatSet` sınıfını yaz: modifier listesi tutar, `Add()` / `RemoveBySource()` / `Get(stat, baseValue)` metodları — GDD'deki formül: `(Base + ΣAdded) × (1 + ΣIncreased) × Π More`
5. **Test:** Konsol veya basit bir Unity test sahnesinde StatSet'e birkaç modifier ekle-çıkar, değerlerin doğru geldiğini ve aç-kapa-aç yapınca bozulmadığını doğrula
6. Mevcut `MultiShotStrategy`'yi modifier üretir hale getir: `arrowCount += extra` yerine `statSet.Add(new Modifier("ArrowCount", Added, 2, this))`
7. Mevcut `RageModeStrategy`'yi modifier üretir hale getir: kalıcı çarp/böl yerine `statSet.Add(new Modifier("Damage", More, 0.5f, this))` — buggy davranış yapısal olarak ölür
8. `BurnDamageStrategy` ve `AttackSpeedStrategy`'yi de aynı şekilde modifier'a çevir
9. `SkillManager.CreateStrategy` if/else zincirini data-driven registry'ye çevir (yeni skill = kod değil kayıt)
10. **Milestone:** Tüm mevcut 4 skill modifier üretiyor, hiçbir strategy doğrudan component mutasyonu yapmıyor, oyun eskisi gibi çalışıyor

**Bitti mi kontrolü:** RageMode'u aç-kapa-aç yap → değerler bozulmuyor. MultiShot + Rage birlikte açılınca ok sayısı doğru. Konsol hatası yok.

---

### Faz 0b — Simulation / Presentation Ayrımı (2-3 hafta)

**Amaç:** Oyun mantığını Unity'den kopar. "Core" klasöründeki hiçbir dosya `using UnityEngine` içermesin.

1. `Assets/_Game/Core/Combat/` klasörünü oluştur
2. `PlayerScript` mantığını ikiye böl:
   - **Core:** `CharacterState` sınıfı (saf C#) — maxHealth, curHealth, TakeDamage(), isDead. MonoBehaviour DEĞİL.
   - **View:** `PlayerView` (MonoBehaviour) — CharacterState'i tutar, ölünce Destroy çağırır, health bar günceller
3. `EnemyScript` mantığını ikiye böl:
   - **Core:** `EnemyState` — HP, TakeDamage(), ölüm kontrolü. NavMeshAgent KULLANMAZ (sadece karar: "hedef nerede, menzilde mi")
   - **View:** `EnemyView` — NavMeshAgent ile hareketi gerçekleştirir, animasyon, health bar
4. `AutoAttackScript` mantığını ikiye böl:
   - **Core:** `AttackSim` (saf C#) — StatSet'ten arrowCount/attackSpeed okur, cooldown hesaplar, "şu açılarla N ok at" sonucu döndürür
   - **View:** `AutoAttackView` — AttackSim.Tick() çağırır, sonuca göre ok prefab'ı Instantiate eder
5. `BowScript` (ok davranışı) mantığını ikiye böl:
   - **Core:** `ProjectileSim` — hareket yönü, hız, çarpışma kontrolü (Raycast DEĞİL, mantıksal mesafe)
   - **View:** `ArrowView` — transform hareket, VFX, Destroy
6. Core asmdef'inin UnityEngine'e referans vermediğini derleyiciyle doğrula — derleme hatası varsa bağımlılık sızmış demektir
7. **Milestone:** Oyun eskisi gibi çalışıyor ama tüm karar mantığı `_Game/Core/` altında, `using UnityEngine` satırı yok

**Bitti mi kontrolü:** Core asmdef'ini ayrı bir .NET konsol projesine referans verip derleyebilirsin (Unity olmadan). Oyun hâlâ aynı şekilde oynanıyor.

---

### Faz 0c — Sınıf Sistemi + Skill Tree (Data-Driven) (3-4 hafta)

**Amaç:** Yeni sınıf/skill eklemek kod değil veri işi olsun.

1. `Assets/_Game/Core/Character/` klasörünü oluştur
2. `ClassDef` veri yapısını tanımla: sınıf adı, başlangıç stat profili (baseHealth, baseSpeed...), kullanabildiği skill listesi, skill ağacı referansı
3. Mevcut Archer'ı bir `ClassDef` verisi olarak tanımla (ScriptableObject veya JSON)
4. İkinci bir sınıf ekle (en basit: Warrior/melee) — yeni kod YAZMA, sadece yeni veri: farklı stat profili, farklı skill listesi
5. Karakter oluşturma akışına sınıf seçimi ekle (basit UI — 2 buton yeter)
6. `Assets/_Game/Core/Skills/` altında skill tree yapısını kur:
   - `TreeNode`: id, komşu node id'leri, verdiği modifier listesi veya açtığı skill
   - `SkillTree`: node'lar listesi (graph), her sınıf kendi ağacına sahip
7. Seviye başına 1 puan → sınıf ağacında harcama. Basit UI: düğümleri göster, tıkla → puan harca → modifier StatSet'e eklensin
8. **Milestone:** 2 sınıf var, her birinin kendi skill seti ve basit ağacı var, ağaçtan alınan node'lar modifier olarak StatSet'e giriyor

**Bitti mi kontrolü:** Warrior seçince melee çalışıyor, Archer seçince ok atıyor. Ağaçtan node alınca stat değişiyor (örn. +%10 damage alınca gerçekten hasar artıyor).

---

## Faz 1 — Meta Backend + Persistence (SENİN KONFOR ALANIN)

> **Amaç:** Hesap aç, karakterini buluta kaydet. Telefonunu değiştir, karakterin durur. Bu faz = senin bildiğin .NET Web API.

### Faz 1a — Backend İskeleti + Auth (1-2 hafta)

1. Yeni .NET solution oluştur (`Game.Api`), modular monolith yapısı: `Shared/`, `Modules/Account/`
2. PostgreSQL bağlantısı + EF Core + migration altyapısı
3. `Account` modülü: `POST /auth/register`, `POST /auth/login` → JWT üretimi
4. Auth middleware (JWT doğrulama)
5. **Test:** Postman/curl ile kayıt ol, login ol, token al

### Faz 1b — Karakter CRUD + Envanter (2-3 hafta)

1. `Modules/Character/` modülü: `POST /characters` (oluştur, sınıf seç), `GET /characters`, `DELETE /characters/{id}`
2. Karakter tablosu: accountId, classId, level, xp, stat snapshot (JSON veya ilişkisel)
3. `Modules/Inventory/` modülü: `GET /characters/{id}/inventory`, item ekle/çıkar/taşı
4. Item tablosu: itemTemplateId, characterId, slot, affix'ler (JSON)
5. **Test:** Karakter oluştur, item ekle, listele, sil — hepsi API üzerinden çalışıyor

### Faz 1c — PassiveTree + Unity Entegrasyonu (2-3 hafta)

1. `Modules/PassiveTree/` modülü: `POST /characters/{id}/passives/allocate`, `GET /characters/{id}/passives`
2. Puan/komşuluk doğrulama (alınan node komşusu mu, yeterli puan var mı)
3. `Modules/Catalog/` modülü: `GET /content/skills`, `GET /content/tree/{classId}` (read-only referans veri)
4. Unity client'ta `UnityWebRequest` ile login → karakter yükle → oyna → kaydet akışı
5. **Milestone:** Oyunu aç → login → karakter seç → oyna → kapat → tekrar aç → karakter duruyor

**Bitti mi kontrolü:** İki farklı cihazdan (veya emülatör) aynı hesaba gir, aynı karakteri gör.

---

## Faz 2 — Authoritative Realtime Instance Server (YENİ PARADİGMA)

> **Amaç:** Otorite client'tan alınır. Hasar/AI/loot sunucuda. Bu faz yeni öğrenilecek kısım.

### Faz 2a — Karar: Unity Headless mi, Saf .NET mi? (1 hafta)

1. Mevcut oyunun NavMesh/Physics bağımlılığını değerlendir
2. NavMesh kullanıyorsan (kullanıyorsun — EnemyScript) → Unity Headless + FishNet
3. FishNet'i öğren: basit bir "iki oyuncu aynı sahnede yürüyor" prototipi yap
4. **Karar ver ve dokümante et**

### Faz 2b — Tek Oyunculu Authoritative (3-4 hafta)

1. Unity headless server build (`-batchmode -nographics`)
2. FishNet entegrasyonu: client input gönderir (yön, skill komutu), server simüle eder
3. Faz 0'daki Core mantığı sunucuda koştur: `AttackSim`, `StatSet`, `CharacterState` zaten UnityEngine'siz — birebir çalışır
4. Client artık hasar hesaplamaz, sunucudan gelen state'i render eder
5. **Test:** Oyunu aç, client "fireball at" der, sunucu hasarı hesaplar, client sonucu gösterir

### Faz 2c — Co-op + Matchmaking (3-4 hafta)

1. Aynı instance'a 2+ oyuncu bağlansın
2. Matchmaking: "zone'a gir" → meta backend (Faz 1) Redis'e bakar → boş instance bul/oluştur → client'a adresi ver
3. Instance yaşam döngüsü: oluştur → oyuncuları kabul et → boşalınca kapat
4. Kalıcı sonuçları (XP, loot) Faz 1 API'sine yaz
5. **Milestone:** İki oyuncu aynı dungeon'da birlikte boss öldürüyor

---

## Faz 3 — Trade + Ekonomi + Güvenlik

### Faz 3a — Item Trade (2-3 hafta)
1. `Modules/Trade/` modülü: item listeleme (`POST /trade/listings`), arama (`GET /trade/search`)
2. Oyuncu-oyuncu takas onayı (her iki taraf kabul edince DB transaction ile item el değiştirir)
3. UI: basit trade arayüzü

### Faz 3b — Currency Exchange (2-3 hafta)
1. Redis order book: "100 altın satıyorum, 50 kristal istiyorum" emir sistemi
2. Otomatik eşleştirme (bid/ask matching)
3. Güvenlik: item dupe kontrolü, transaction bütünlüğü

### Faz 3c — Anti-Cheat Sertleştirme (1-2 hafta)
1. Server-side doğrulamalar: hız hack, imkansız hasar, geçersiz item
2. Rate limiting, input sanitization

---

## Faz 4 — Ligler / Sezonlar

### Faz 4a — League Altyapısı (2-3 hafta)
1. `Modules/League/`: `leagues` tablosu, karakter `league_id`'ye bağlanır
2. Lig izolasyonu: farklı liglerdeki ekonomiler birbirini görmez
3. Standard lig (kalıcı) + sezonluk lig

### Faz 4b — Reset/Migrate (1-2 hafta)
1. Lig bitince karakter + item'ları Standard'a taşıyan batch job
2. Lig-özel içerik bayrakları (yeni mekanik sadece o ligde)

---

## Öncelik Sırası ve Bağımlılık Haritası

```
Faz 0a (Stat Motoru) ← BURADAN BAŞLA, bağımlılık yok
  ↓
Faz 0b (Sim/View Ayrımı) ← 0a'ya bağlı
  ↓
Faz 0c (Sınıf + Skill Tree) ← 0b'ye bağlı
  ↓
Faz 1a → 1b → 1c (Meta Backend) ← 0c'nin veri modeline bağlı, ama 0b bitince paralel başlanabilir
  ↓
Faz 2a → 2b → 2c (Realtime Server) ← Faz 0 + Faz 1 gerekli
  ↓
Faz 3a → 3b → 3c (Trade) ← Faz 2 gerekli (authoritative item sahipliği)
  ↓
Faz 4a → 4b (Ligler) ← Faz 1-3 stabil
```

## Tahmini Toplam Süre (tek kişi, tam zamanlı)

| Faz | Süre | Kümülatif |
|-----|------|-----------|
| 0a | 1-2 hafta | 2 hafta |
| 0b | 2-3 hafta | 5 hafta |
| 0c | 3-4 hafta | 9 hafta |
| 1a-1c | 5-8 hafta | 17 hafta |
| 2a-2c | 7-9 hafta | 26 hafta |
| 3a-3c | 5-8 hafta | 34 hafta |
| 4a-4b | 3-5 hafta | 39 hafta |

~8-10 ay tam zamanlı. Yarı zamanlı ise ~14-18 ay.

---

## İlk Adımın (Yarın)

**Faz 0a, Adım 1:** `Assets/_Game/Core/Stats/` klasörünü oluştur, Core asmdef'ini yarat. İçine `StatSet.cs` yaz. 50 satırlık bir dosya. Sadece bu.
