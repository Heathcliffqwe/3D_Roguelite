# 3DSimple — 3 Katmanlı Mimari (Detaylı Açıklama)

> Bu doküman **tek bir şeyi** çok ayrıntılı anlatır: oyunun online mimarisindeki 3 katman (+ client)
> tam olarak nedir, içinde ne çalışır, ne çalışMAZ, nasıl konuşurlar, ne nerede saklanır.
> Amaç: kafa karışmasın. Genel mimari kararları → BACKEND_ARCHITECTURE.md, yol haritası → ROADMAP.md.

---

## 0. Önce büyük resim ve "altın kural"

```
┌─────────────────────────────────────────────────────────┐
│  UNITY CLIENT (oyuncunun telefonu/PC'si)                 │
│  - Sadece görsel + input. OTORİTE YOK.                   │
│  - Simülasyonu çalıştırmaz, sonucu gösterir.             │
└───────────────┬──────────────────────┬──────────────────┘
                │ realtime (UDP/WS)     │ HTTPS (REST)
                ▼                       ▼
┌──────────────────────────┐  ┌─────────────────────────────┐
│ KATMAN A: GAME / INSTANCE │  │ KATMAN B: META / PLATFORM   │
│ SERVER (authoritative)    │  │ BACKEND (kalıcı dünya)      │
│ "canlı dövüş sunucusu"    │  │ "web API'n"                 │
└───────────┬───────────────┘  └──────────────┬──────────────┘
            │                                  │
            └──────────────►  KATMAN C: DB  ◄──┘
               (PostgreSQL + Redis)
```

**Altın kural — neyi nereye koyacağını belirleyen tek soru:**
> "Bu bilgi/işlem **kalıcı mı (oyunu kapatınca durmalı mı)**, yoksa **anlık mı (sadece o dövüş sürerken mi yaşıyor)**?"

- **Kalıcı** → Katman B + C (karakterin, item'ların, seviyesi, parası).
- **Anlık** → Katman A (canavarın o anki HP'si, okun havadaki pozisyonu, kimin nerede durduğu).

Bu ayrımı bir kez oturtursan gerisi kendiliğinden gelir.

---

## 1. CLIENT (Unity) — "aptal terminal"

### Tek cümlede
Oyuncunun cihazında çalışan, **sadece gösteren ve input toplayan** Unity uygulaması. Karar vermez.

### Görevleri (ne YAPAR)
- Ekranı çizer: karakterler, animasyon, efekt, UI, can barları.
- Input toplar: joystick hareketi, skill butonu, item sürükleme.
- Bu input'u sunucuya **istek/komut** olarak yollar ("şu yöne gitmek istiyorum", "fireball atmak istiyorum").
- Sunucudan gelen **durumu** alır ve ekrana yansıtır ("canavar B 50 hasar aldı, HP %40").
- Görsel yumuşatma: interpolation/prediction (sunucu cevabı gelene kadar hareketi tahmini gösterme — sadece görsellik, otorite değil).

### Ne YAPMAZ (kritik sınırlar)
- ❌ Hasar hesaplamaz. "Fireball 200 vurdu" demez; "fireball atmak istiyorum" der, **kararı sunucu verir**.
- ❌ Loot belirlemez. "Bu canavar kılıç düşürdü" diyemez.
- ❌ Item/para/XP değiştiremez. Sadece sunucunun söylediğini gösterir.
- ❌ "Şu canavar öldü" diye karar veremez.

> **Neden bu kadar katı?** Çünkü client oyuncunun cihazında çalışır = **hile yapılabilir**. Client'a güvenirsen oyuncu belleği değiştirip "1.000.000 altınım var" der. Authoritative mimarinin tüm fikri: client'a asla güvenme. (Anti-cheat'i ertelesen bile mimariyi böyle kurmak şart, sonradan dönüştürmek çok pahalı.)

### Senin oyununa örnek
Şu an `BowScript` okun çarpışmasını client'ta hesaplıyor ve `EnemyScript.TakeDamage` çağırıyor. Online'da bu **tersine döner**: client "ok attım, şu yöne" der; **sunucu** okun nereye gittiğini, neye çarptığını, ne hasar verdiğini hesaplar ve sonucu tüm client'lara yollar.

---

## 2. KATMAN B: META / PLATFORM BACKEND — "senin bildiğin web API"

### Tek cümlede
Oyunun **kalıcı dünyasını** yöneten klasik web servisi. Login, karakter, envanter, trade, lig — hepsi burada. **Bu katman senin n-layer web API tecrübenin birebir karşılığı.**

### Zihinsel model
> Bu = bir e-ticaret/SaaS backend'i. "Kullanıcı" yerine "oyuncu", "sipariş" yerine "karakter/item" koy. CRUD + auth + iş kuralları. Yeni hiçbir paradigma yok.

### Çalışma şekli
- **Stateless request/response (HTTP/REST).** İstek gelir → işler → cevap döner → unutur. Tıpkı normal API'n gibi.
- Gerçek zamanlı **değil**. "Karakterimi kaydet" milisaniye hassasiyeti istemez.

### İçinde ne çalışır (modüller — modular monolith)
| Modül | Sorumluluğu | Örnek endpoint |
|---|---|---|
| **Account** | Kayıt, login, JWT token, oturum | `POST /auth/register`, `POST /auth/login` |
| **Character** | Karakter oluştur/sil/listele, sınıf, seviye, stat, hangi ligde | `GET /characters`, `POST /characters` |
| **Inventory / Stash** | Envanter, depo, ekipman; item ekle/çıkar/taşı | `GET /characters/{id}/inventory` |
| **PassiveTree / Skills** | Alınan ağaç düğümleri, skill seçimleri | `POST /characters/{id}/passives` |
| **Trade** (Faz 3) | Item listeleme, arama, takas onayı | `POST /trade/listings`, `GET /trade/search` |
| **League** (Faz 4) | Sezon tanımı, hangi karakter hangi ligde, reset/migrate | `GET /leagues/current` |
| **Catalog/Content** | Item şablonları, skill tanımları, ağaç verisi (read-only referans) | `GET /content/skills` |

### Ne YAPMAZ
- ❌ Canlı dövüş simüle etmez (canavar kovalama, ok uçuşu Katman A'nın işi).
- ❌ Saniyede 30 kez tick atmaz. İstek geldikçe çalışır.
- ❌ Oyuncular arası anlık pozisyon senkronu yapmaz.

### Teknoloji
- **ASP.NET Core Web API** (senin alanın). C#/.NET.
- ORM: EF Core (veya Dapper).
- Auth: JWT.
- Önünde reverse proxy (nginx) + TLS.

### Nasıl ölçeklenir
- Stateless olduğu için **kolay**: aynı API'den N kopya çalıştır, önüne load balancer koy. State DB'de (Katman C) olduğu için kopyalar birbirini umursamaz. (Senin normal web API ölçeklemenle aynı.)

### Senin oyununa örnek
Oyuncu uygulamayı açar → `POST /auth/login` → JWT alır → `GET /characters` ile necromancer'ını yükler → envanteri, aldığı ağaç düğümleri, seviyesi gelir. Bunların hepsi tanıdık CRUD. **Faz 1 tamamen budur.**

---

## 3. KATMAN A: GAME / INSTANCE SERVER — "canlı dövüş sunucusu" (YENİ paradigma)

### Tek cümlede
Bir **zone instance'ını** (bir harita oturumunu) sunucuda gerçek zamanlı simüle eden, oyunun kurallarını **otoriter** işleten process. Asıl "oyun sunucusu" budur ve web API'den **farklıdır.**

### Zihinsel model
> SignalR ile yazdığın gerçek-zamanlı bir uygulamayı düşün — ama "sadece mesaj iletmek" yerine **tüm oyun dünyasını sunucu hesaplıyor** ve saniyede ~20-30 kez (tick) güncelliyor. Bağlantı **açık kalır** (request/response değil).

### Çalışma şekli: "tick loop" (en önemli kavram)
Web API "istek gelince çalışır". Game server **sürekli döner**:
```
oyuncu bağlanır → kalıcı bağlantı (UDP/WebSocket) açılır
state belleğe yüklenir (bu instance'taki canavarlar, oyuncular, loot)

while (instance yaşıyor) {          // saniyede ~20-30 kez = "tick"
    1. client input'larını oku      ("oyuncu sağa gidiyor", "fireball attı")
    2. dünyayı ilerlet:
         - canavar AI (kimi kovalıyor, ne zaman vuruyor)
         - hareket, çarpışma
         - HASAR hesapla (stat motoru burada koşar!)
         - ölüm, loot düşürme
    3. yeni durumu tüm client'lara gönder  ("canavar B öldü, X kılıç düştü")
}
instance boşalınca → kapanır, kalıcı sonuçlar Katman B/C'ye yazılır
```

### İçinde ne çalışır
- **Oyun simülasyonu**: hareket, çarpışma, AI, hasar, ölüm, loot.
- **Stat / Modifier motoru**: "bu fireball kaç hasar verir" = (base + added) × increased × more. Otoriter hesap burada. (Faz 0'da yazacağın saf C# çekirdek **birebir burada koşar.**)
- **Instance yaşam döngüsü**: oluştur, oyuncuları kabul et, bitince kapat.
- **Geçici durum**: canavarların o anki HP'si, oyuncu pozisyonları, yere düşmüş ama henüz alınmamış loot.

### Ne YAPMAZ
- ❌ Kalıcı veri tutmaz. Instance kapanınca canavarların HP'si yok olur (zaten geçici). Sadece **kalıcı sonuçları** (alınan loot, kazanılan XP) Katman B/C'ye yazar.
- ❌ Login/hesap işlemez (o Katman B).
- ❌ Trade yapmaz.

### "Geçici / stateless-ish" ne demek?
- Instance **çalışırken** belleğinde state vardır (stateful). AMA bu state **harcanabilir**: instance çökerse o oturum kaybolur, dünya çökmez — çünkü kalıcı her şey zaten Katman C'de.
- Yani: instance içinde stateful, ama **sistem açısından** instance'lar gelip geçici → kolay ölçeklenir.

### Teknoloji (Faz 2 kararı)
İki seçenek:
- **A) Unity headless server** (`-batchmode -nographics`) + **FishNet**: Faz 0'daki C# oyun mantığın sunucuda **birebir** koşar. NavMesh/physics kullanıyorsan bu. **Önerilen.**
- **B) Saf .NET realtime server** (Unity'siz): zone mantığı tamamen deterministik/grid ise. Faz 0 çekirdeği UnityEngine'siz olduğu için bu da mümkün, daha ucuz.

### Nasıl ölçeklenir
- **Instance başına process.** 1000 oyuncu = aynı dünyada 1000 kişi değil; her biri/partisi **ayrı küçük instance'larda**. Yük arttıkça daha çok instance (daha çok makine) açarsın.
- Bir "orchestrator/matchmaker" boş bir instance bulur ya da yenisini başlatır, oyuncuyu ona yönlendirir.

### Senin oyununa örnek
Oyuncu "Boss Arena"ya girer → matchmaker bir instance ayırır → client o instance'a bağlanır → `Moderator`/`EnemyManager` mantığının **sunucu versiyonu** boss'u ve düşmanları orada spawn'lar, tick'ler, AI'yı koşturur. Client sadece sonucu render eder.

---

## 4. KATMAN C: VERİ — "kalıcı hafıza"

### Tek cümlede
Tüm kalıcı verinin saklandığı yer. İki farklı araç, iki farklı iş.

### 4.1 PostgreSQL — kalıcı, ilişkisel "ana hafıza"
**Ne için:** oyunu kapatınca durması gereken her şey.
- Hesaplar, karakterler (sınıf, seviye, XP, hangi lig).
- Item'lar, envanter, stash, ekipman.
- Alınan pasif ağaç düğümleri, skill seçimleri.
- Trade listeleri, ekonomi kayıtları, lig tanımları.

**Neden ilişkisel (NoSQL değil):** Karakter→item, oyuncu→karakter, trade→item gibi **ilişkiler** ve **tutarlılık** (transaction) kritik. "Item A oyuncudan B'ye geçti ve para ters yönde gitti" ya tamamen olur ya hiç olmaz (ACID). Bu garantiyi ilişkisel DB verir; ekonomi/trade için şart.

> Senin için tanıdık: bu normal web API'nin DB'si. EF Core + migration + tablo şeması. Hiç yeni bir şey yok.

### 4.2 Redis — hızlı, geçici "ön bellek / oturum"
**Ne için:** çok hızlı erişilmesi gereken, çoğu zaman geçici veri.
- **Oturum/token cache**: "bu JWT geçerli mi", aktif oturumlar.
- **Matchmaking/instance kaydı**: hangi instance hangi makinede, kaç kişi var, nerede boş yer var.
- **Currency exchange order book** (Faz 3): "100 oyuncu şu kuru satıyor" — anlık eşleştirme için RAM hızı gerekir, Postgres yavaş kalır.
- **Cache**: sık okunan ama az değişen veri (content tabloları, leaderboard).
- **Pub/sub**: sunucular arası anlık mesajlaşma (instance ↔ meta backend haberleşmesi).

**Neden Redis (Postgres yetmez mi):** Redis RAM'de çalışır, mikrosaniye seviyesinde hızlı; ama kalıcı/ilişkisel garantileri zayıf. O yüzden **iş bölümü**: Postgres = doğruluk + kalıcılık, Redis = hız + geçicilik.

### Net kural
> Kaybolursa **felaket** olan veri → PostgreSQL. Kaybolursa sadece "yeniden hesaplanır/yeniden login olunur" olan veri → Redis.

---

## 5. Katmanlar nasıl konuşur (protokoller)

| Yön | Protokol | Neden |
|---|---|---|
| Client → **Katman B** | **HTTPS / REST** | Kalıcı işlemler (login, karakter yükle). Anlık olması gerekmez; istek/cevap yeterli. |
| Client ↔ **Katman A** | **UDP veya WebSocket** (kalıcı bağlantı) | Dövüş gerçek zamanlı. Saniyede onlarca güncelleme, düşük gecikme şart. REST burada çok yavaş ve uygunsuz. |
| Katman A → **Katman B/C** | İç ağ: gRPC/REST + DB driver | Instance bitince "şu XP'yi/loot'u kalıcı yaz" der. |
| Tüm sunucular → **Katman C** | DB bağlantısı (Postgres driver, Redis client) | Okuma/yazma. |

**Neden client iki ayrı yere bağlanır?** Çünkü iki farklı ihtiyaç var: "karakterimi yükle" (yavaş, güvenilir, REST → B) ve "şu an dövüşüyorum" (hızlı, sürekli, UDP/WS → A). Aynı kanal ikisini birden iyi yapamaz.

---

## 6. Ne nerede çalışır / saklanır — hızlı referans tablosu

| Şey | Karar/işlem nerede | Kalıcı veri nerede |
|---|---|---|
| Login / hesap | Katman B | Postgres (+ Redis oturum) |
| Karakter oluştur/yükle | Katman B | Postgres |
| Envanter görüntüle/düzenle | Katman B | Postgres |
| Pasif ağaç düğümü alma | Katman B (kural kontrolü) | Postgres |
| Hareket | Katman A (otorite) | Hiç (geçici) |
| Hasar hesabı | **Katman A** (stat motoru) | Hiç (geçici) |
| Canavar AI / ölüm | Katman A | Hiç (geçici) |
| Loot **düşmesi** (drop roll) | **Katman A** | Hiç (yere düştü, henüz alınmadı) |
| Loot **alınması** (envantere) | Katman A karar → Katman B yazar | Postgres |
| XP kazanma | Katman A hesaplar → Katman B yazar | Postgres |
| Trade / currency exchange | Katman B | Postgres (+ Redis order book) |
| Instance kaydı / matchmaking | Orchestrator | Redis |

> Dikkat çeken nokta: **loot ve XP "Katman A'da olur ama Katman B/C'de kalıcılaşır".** Bu köprü çok önemli — anlık olay A'da gerçekleşir, sonucu B'ye yazılır.

---

## 7. Uçtan uca akış örneği (her şeyi birbirine bağlar)

Bir oyuncunun **uygulamayı açıp bir canavarı öldürüp loot alana** kadar olan yolculuğu, katman katman:

1. **Login** — Client → `POST /auth/login` → **Katman B** doğrular, Postgres'ten hesabı okur, JWT üretir, Redis'e oturum yazar → Client token alır. *(tanıdık web API)*
2. **Karakter seç** — Client → `GET /characters` → **Katman B** Postgres'ten necromancer'ı (sınıf, seviye, envanter, ağaç) yükler → Client'a döner. *(tanıdık web API)*
3. **Zone'a gir** — Client → "Boss Arena'ya gir" → **Katman B/Orchestrator** Redis'e bakar, boş bir **Katman A instance**'ı bulur/başlatır, adresini Client'a verir.
4. **Bağlan** — Client, o **Katman A** instance'ına **kalıcı bağlantı** (UDP/WS) açar. Instance, karakterin stat'larını Katman B'den/snapshot'tan yükler.
5. **Dövüş (tick loop)** — Client "fireball attım" der. **Katman A**: stat motoruyla hasarı hesaplar, canavarın geçici HP'sini düşürür, AI'yı tick'ler, sonucu tüm client'lara push'lar. Bu saniyede onlarca kez döner. *(yeni paradigma)*
6. **Ölüm + loot drop** — Canavar HP 0 → **Katman A** "öldü" der, loot tablosundan **drop roll** yapar (otoriter), yere "bir kılıç düştü" diye yayınlar. (Henüz kalıcı değil, geçici.)
7. **Loot al** — Oyuncu kılıca basar → Client "almak istiyorum" der → **Katman A** onaylar (gerçekten orada mı, hak ediyor mu) → **Katman B**'ye "bu item bu karaktere eklensin" der → **Katman B** Postgres'e yazar. Artık kalıcı.
8. **Çıkış** — Oyuncu zone'dan çıkar → **Katman A** kazanılan XP/değişiklikleri Katman B'ye yazdırır → instance boşalınca kapanır (geçici state silinir, kalıcı her şey zaten Postgres'te).

> Bu akıştaki ders: **1-2-3 ve 7-8'in yazma kısmı = senin bildiğin web API (Katman B/C).** Sadece **4-5-6 = yeni öğreneceğin realtime kısım (Katman A).** Yani işin yarısı zaten konfor alanında.

---

## 8. Sık karışan noktalar (kafa netleştirici)

- **"Katman A ve B aynı sunucu mu?"** Hayır, ayrı process'ler (biri belki Unity headless, biri ASP.NET). Ama bu **microservices değil** — fiziksel zorunluluk. Her birinin *kendi içi* modular monolith.
- **"Neden DB'ye client doğrudan bağlanmıyor?"** Asla. Client güvenilmez; DB'ye sadece sunucu katmanları erişir. Client her zaman bir API/sunucu üzerinden geçer.
- **"Stat motorunu iki kere mi yazacağım (client + server)?"** Hayır. Saf C# çekirdeği bir kez yaz (Faz 0); sunucu (Katman A) onu otoriter koşar, client sadece görsel tahmin için aynı kodu *kullanabilir* ama karar sunucunundur.
- **"Redis olmadan olmaz mı?"** Başta olur — Faz 1'de sadece Postgres yeter. Redis matchmaking/order book/cache ihtiyacı doğunca (Faz 2-3) girer. Erken eklemek zorunda değilsin.
- **"Instance = sahne mi?"** Kabaca evet: bir zone'un, belirli oyunculara özel, sunucuda yaşayan, dövüş bitince ölen canlı bir kopyası.

---

## 9. Tek paragraf özet

Client **gösterir ve input yollar, asla karar vermez**. **Katman B** senin bildiğin **web API'dir** (login/karakter/envanter/trade — kalıcı dünya, REST, stateless). **Katman A** yeni öğreneceğin **canlı dövüş sunucusudur** (tick loop, authoritative simülasyon, geçici instance'lar, UDP/WS). **Katman C**, Postgres'te (kalıcı + ilişkisel + tutarlı) ve Redis'te (hızlı + geçici) veriyi tutar. Neyin nereye gideceğini tek soruyla bul: **"Bu kalıcı mı, anlık mı?"** Kalıcı → B/C, anlık → A.
