# 3DSimple — Modül Sınırları + Faz 0 Provası

> Tartışma notu. Projeye start vermeden önce modül sınırlarını çizmek ve Faz 0'ı bir dikey dilimde provasını yapmak için yazıldı.
> İlgili dokümanlar: GDD.md · ROADMAP.md · ARCHITECTURE_LAYERS.md · BACKEND_ARCHITECTURE.md

---

## Baştan: iki ayrı modül seti

Aslında **iki ayrı modül seti** var, ikisi de modüler mantıkla bölünür ama farklı dünyalar:

- **Faz 0 → Unity simülasyon çekirdeği** modülleri (Stats, Combat, Skills...). Zaman olarak ÖNCE bu gelir.
- **Faz 1 → Backend (Katman B)** modülleri (Account, Character, Trade...). "Modular monolith" dediğimiz şey asıl bu.

---

## 1. Backend modül sınırları (Katman B — modular monolith)

### Modül haritası

| Modül | Sahip olduğu tablolar | Sorumluluk | Dışa açtığı arayüz |
|---|---|---|---|
| **Account** | `accounts`, `refresh_tokens` | kayıt, login, JWT, oturum | `IAccountService` (ValidateToken, GetById) |
| **Character** | `characters` | karakter CRUD, sınıf, seviye, XP, stat snapshot | `ICharacterService` |
| **Inventory** | `items`, `inventory_slots`, `stash_tabs`, `equipment` | item sahipliği, taşıma, ekipman giy/çıkar | `IInventoryService` |
| **PassiveTree** | `passive_allocations` | alınan düğümler, puan/komşuluk doğrulama | `IPassiveTreeService` |
| **Catalog** (Content) | seed/read-only: `item_templates`, `skill_defs`, `tree_graph`, `affix_pool` | tüm "tasarım verisi" referansı | `ICatalogService` (salt-okunur) |
| **Trade** (Faz 3) | `listings`, `exchange_orders` | listeleme, arama, takas onayı | `ITradeService` |
| **League** (Faz 4) | `leagues` (+ `characters.league_id`) | sezon, izolasyon, reset/migrate | `ILeagueService` |
| **Shared / Kernel** | — | cross-cutting: auth middleware, `Result<T>`, `Money`, `BaseEntity` | — (herkes referans verir) |

### Bağımlılık yönü (tek yönlü, döngü YASAK)

```
        Shared/Kernel  ◄──────── (herkes buna bakar)
            ▲
   Account ◄── Character ◄── Inventory ◄── Trade
                  ▲    ▲          ▲
            PassiveTree │         │
                  ▲     │         │
                League ─┘         │
            Catalog ◄─────────────┘   (read-only, herkes okur)
```

- Ok = "bağımlıdır". Yön hep yukarı/içeri akar.
- **Kimse Trade/League'e bağlı değil** → onlar yaprak, geç fazlar, en son eklenir, sökmesi kolay.
- **Catalog'a herkes bakabilir** (içerik verisi salt-okunur, kimseyi mutasyona uğratmaz).

### Altın kurallar (modülerliği "modüler" yapan tek şey bunlar)

1. **Bir modül kendi tablolarına sahiptir; başka modülün tablosuna JOIN atmaz.** Character'ın item'ı lazımsa `IInventoryService.GetItemsOf(charId)` çağırır, `items` tablosuna elle gitmez.
2. **Modüller arası iş yalnızca interface üzerinden** (DI ile enjekte). Entity dışarı sızmaz; sınırda **DTO** konuşulur.
3. **İç sınıflar `internal`** — modülün dışından sadece `Public` arayüzü görünür.
4. **Senkron çağrı serbest** (aynı process, aynı DB transaction'ı bile paylaşabilir) — bu microservices değil, avantaj bu.
5. **Döngüsel bağımlılık yasak.** A→B varsa B→A olamaz; oluyorsa sınırı yanlış çizmişsindir.

### Klasör iskeleti — pragmatik başla (ÖNERİLEN)

Başta ayrı `.csproj`'lara bölme; namespace + klasör disiplini yeter:

```
Game.Api/                       ← tek deploy edilen birim
  Shared/                       Result, Money, BaseEntity, JwtMiddleware
  Modules/
    Account/      Controller · IAccountService · AccountService · AccountRepo · Account(entity)
    Character/    Controller · ICharacterService · ...
    Inventory/
    PassiveTree/
    Catalog/
  AppDbContext.cs               ← tek context, her modül kendi IEntityTypeConfiguration'ını ekler
  Program.cs                    ← DI: her modülün AddXModule() uzantısı
```

Bu aşamada sınırı **disiplin + namespace** korur. Sıkışınca (Trade darboğaz olur ya da sınır ihlalleri başlar) her modülü `Account.Public` + `Account.Internal` diye **ayrı assembly**'ye çıkarırsın — o zaman ihlali **derleyici** yakalar. Önce buna ihtiyaç yok; erken bölmek seni boğan ceremony olur.

> Net mesaj: yukarıdaki her `Modules/X/` klasörü = senin bildiğin minik bir n-layer app. Modüler olmak = bunları yan yana koymak + 5 altın kural. Bitti.

---

## 2. Faz 0 provası (Unity çekirdeği)

Faz 0 backend değil — **offline, tek oyunculu Unity refactor'ı.** Ama aynı modüler mantık burada da `asmdef` ile uygulanır.

### Faz 0 modül haritası (asmdef sınırları)

```
Assets/_Game/
  Core/            ← asmdef: UnityEngine'e REFERANS YOK  (saf C#, sunucuda da koşacak olan)
    Stats/         Stat, ModType, Modifier, StatSet  → FinalStat hesabı
    Combat/        DamageCalc, HitResult, can/ölüm mantığı
    Skills/        SkillDef (data), modifier üreten model, registry (if/else yerine)
    Character/     CharacterState, ClassDef, seviye/XP
    AI/            karar mantığı (hedef seç) — NavMesh DEĞİL, saf karar
  View/            ← asmdef: Core'a + UnityEngine'e referans verir (tersi OLMAZ)
    PlayerView, EnemyView, AutoAttackView, SkillButtonUI, ArrowView...
```

**Tek kritik kural:** `Core` asmdef'i UnityEngine'e referans vermez → derleyici, mantığa yanlışlıkla `Vector3`/`MonoBehaviour` sızdırmanı **engeller**. İşte Katman A'da birebir koşacak "dikiş yeri" budur.

### Prova: MultiShot dikey dilimi, baştan sona

Provayı **kolay yoldan değil, en zor vakadan** yapalım ki tasarım gerçekten sınansın. En zor vaka dokümanların "buggy" dediği **RageMode** (diğer skilleri çarpan meta-skill) + `arrowCount` mutasyonu.

**Şu anki kod (atılacak):**

```
// MultiShotStrategy   → autoAttack.arrowCount += extra;     // doğrudan mutasyon
// RageModeStrategy     → değerleri kalıcı çarpıp böl         // buggy, kalıcı state bozar
```

**Yeni tasarım — adım adım:**

**(1) Stat motoru (Core/Stats, saf C#)**

```
enum ModType { Added, Increased, More }
record Modifier(string Stat, ModType Type, float Value, object Source);

class StatSet {
    // stat -> modifier listesi
    float Get(string stat, float base_) {
        float added = Σ (Added);
        float incr  = Σ (Increased);
        float more  = Π (1 + More);
        return (base_ + added) * (1 + incr) * more;   // GDD'deki formül
    }
    void Add(Modifier m);  void RemoveBySource(object src);
}
```

**(2) Skill artık modifier ÜRETİR, component'e dokunmaz (Core/Skills)**

```
MultiShot açıldı  → statSet.Add( new("ArrowCount", Added, 2, multiShot) )
MultiShot kapandı → statSet.RemoveBySource(multiShot)
// hiçbir yerde arrowCount += yok
```

**(3) RageMode artık temiz — "More" modifier'ı (buggy davranış çözülür)**

```
RageMode açıldı  → statSet.Add( new("Damage", More, 0.5f, rage) )   // +%50 more
RageMode kapandı → statSet.RemoveBySource(rage)
// kalıcı çarp/böl yok → aç-kapa-aç yapınca değer bozulmaz. Bug yapısal olarak imkânsız.
```

**(4) Simülasyon (Core/Combat, saf C# — UnityEngine yok)**

```
class AttackSim {
    int ArrowCount(StatSet s)  => (int) s.Get("ArrowCount", baseCount);   // float stat → int
    // hedef seçme, cooldown, hangi açılarla atılacağı → hepsi saf C#, struct'larla
    AttackResult Tick(WorldState w) { ... return "şu açılarla N ok at"; }
}
```

**(5) View (MonoBehaviour, görselleştirme — Core'u tetikler)**

```
class AutoAttackView : MonoBehaviour {
    void Update() {
        var result = sim.Tick(...);        // KARAR Core'da
        foreach (açı in result.Angles)
            Instantiate(arrowPrefab, ...);  // GÖRSEL View'da
    }
}
```

### Bu prova neyi kanıtladı

| İddia | Prova sonucu |
|---|---|
| RageMode bug'ı çözülür mü? | ✅ "More modifier ekle/çıkar" → kalıcı state yok, bug **yapısal olarak** ortadan kalkar |
| Yeni skill = kod mu, veri mi? | ✅ `SkillManager` if/else gitti → yeni skill = yeni `SkillDef` + modifier, registry'e kayıt |
| Mantık sunucuda koşar mı? | ✅ `AttackSim` + `StatSet` UnityEngine'siz → Katman A'da **aynen** çalışır |
| Sınıf sistemi oturur mu? | ✅ ClassDef = stat profili + skill listesi + ağaç → hepsi aynı StatSet'e modifier basar |

Provanın verdiği güven: **stat motoru + sim/view ayrımı bir kez doğru kurulursa**, sonrası (sınıf, ağaç, item affix, hatta authoritative sunucu) hep "aynı motora modifier basmak"a indirgenir. Dokümanların tezi kağıt üzerinde tutuyor.

---

## Sıradaki adım

1. **Bu provayı koda dök** — `Core` asmdef'i + `Stats/StatSet` + `Skills` registry'sini gerçekten oluşturup MultiShot/RageMode'u bu modele taşıyalım (Faz 0'ın ilk dikey dilimi, çalışan kod). **(Önerilen)**
2. **Önce sınırları daha da netleştir** — stat motorunu ne kadar PoE-derinliğinde (tag'li modifier, conditional) kuracağımıza karar ver (ROADMAP açık soru #4).
