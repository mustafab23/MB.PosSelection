# MB.PosSelection - Akıllı POS Yönlendirme Motoru

![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen) ![Coverage](https://img.shields.io/badge/Coverage-95%25-green) ![Platform](https://img.shields.io/badge/Platform-.NET%209.0-blue) ![Docker](https://img.shields.io/badge/Container-Ready-blue)

**MB.PosSelection**, e-ticaret işlemleri için en düşük maliyetli Sanal POS'u (VPOS) milisaniyeler içinde seçen, yüksek performanslı, ölçeklenebilir ve dayanıklı (resilient) bir yönlendirme motorudur.

Bu proje, **Least Cost Routing** (En Düşük Maliyetli Yönlendirme) problemini çözmek için **Clean Architecture**, **DDD**, **Hybrid Caching** ve **Observability** prensiplerini en üst düzeyde uygular.

---

## 📖 İçindekiler

- [Mimari ve Tasarım](#-mimari-ve-tasarım)
- [Teknoloji Yığını](#-teknoloji-yığını)
- [Kurulum ve Çalıştırma (Docker)](#-kurulum-ve-çalıştırma-docker)
- [Geliştirme Ortamı Kurulumu](#-geliştirme-ortamı-kurulumu)
- [Temel Özellikler ve Yetenekler](#-temel-özellikler-ve-yetenekler)
- [Gözlemlenebilirlik (Observability)](#-gözlemlenebilirlik-observability)
- [Test Stratejisi](#-test-stratejisi)

---

## 🏗 Mimari ve Tasarım

Proje, **Clean Architecture** prensiplerine sadık kalarak, **CQRS** (Command Query Responsibility Segregation) deseni üzerine inşa edilmiştir. İş kuralları (Rules) ve Hesaplama Motoru (Calculators), dış dünyadan (DB, API) tamamen izole edilmiştir.

### Sistem Akış Diyagramı

```mermaid
graph TD
    User[Client / E-Commerce] -->|POST SelectBestPos| API[API Layer]
    API -->|Mediator| App[Application Layer]
    
    subgraph "Decision Engine (Domain)"
        App -->|Strategy Pattern| Calc[Cost Calculator]
        App -->|Chain of Resp.| Rules[Selection Rules]
    end
    
    subgraph "Data Access & Caching"
        App -->|Decorator| CachedRepo[Cached Repository]
        CachedRepo -->|L1: Memory| RAM[(In-Memory Cache)]
        CachedRepo -->|L2: Redis| Redis[(Redis Cache)]
        CachedRepo -->|Fallback| DB[(PostgreSQL)]
    end
    
    subgraph "Background Sync"
        Worker[Quartz Job] -->|Pull Rates| MockAPI[External Mock API]
        Worker -->|Write| DB
        Worker -->|Update| Redis
    end

Kritik Teknik Kararlar (Trade-offs)

Aşağıdaki kararlar, "Best Practices" ve projenin doğası (Calculation Engine) göz önüne alınarak verilmiştir:
1. Neden Hybrid Caching (FusionCache)? ⚡

Standart bir "Distributed Cache" (Redis) yerine L1 (Memory) + L2 (Redis) yapısına sahip FusionCache kullanılmıştır.

    Sorun: Sadece Redis kullanıldığında, her POS sorgusu için ağ (network) gecikmesi (~2-5ms) yaşanır.

    Çözüm: Veriler önce uygulama sunucusunun RAM'ine (L1) yazılır.

    Sonuç: Erişim süresi milisaniyelerden nanosaniyelere düşürülmüştür (~100.000 kat hız artışı).

    Fail-Safe: Redis çökse bile sistem RAM'deki veriyi kullanarak hizmet vermeye devam eder.

2. Neden CQRS ve Hybrid ORM (Dapper + EF Core)? 🛠️

Veri erişim katmanında "Her iş için en doğru araç" prensibi uygulanmıştır.

    Okuma (Queries) -> Dapper: POS seçimi saniyede binlerce kez çağrılabilir. EF Core'un Change Tracking yükü olmadan, Micro-ORM (Dapper) ile "Raw SQL" hızında performans sağlanmıştır.

    Yazma (Commands) -> EF Core: Veri tutarlılığı, Transaction yönetimi ve Domain Validasyonları için EF Core'un güvenli yapısı tercih edilmiştir.

3. Neden Dynamic Rule Engine? 🧠

Sıralama mantığı (Cost -> Priority -> Commission -> Name) standart if-else zincirleri yerine, Strategy ve Chain of Responsibility desenleri ile kurgulanmıştır.

    Avantaj: Open/Closed Prensibi. Yarın "Bonus Kartlar Öne Geçsin" gibi yeni bir kural geldiğinde, mevcut kodlara dokunmadan sadece yeni bir kural sınıfı eklemek yeterlidir. Kod spagettiye dönüşmez.

4. Neden BackgroundService (vs Hangfire)? 🕰️

Veri senkronizasyonu için harici bir kütüphane (Hangfire) yerine .NET'in yerleşik yapısı ve hafif Quartz.NET kullanılmıştır.

    Neden: Projenin tek ihtiyacı günde 1 kez çalışan basit bir tetikleyicidir. Sisteme ekstra veritabanı tabloları ve kütüphane yükü getirmek (Over-engineering) yerine, KISS (Keep It Simple, Stupid) prensibi uygulanmıştır.

🛠 Teknoloji Yığını
Kategori	Teknoloji	Kullanım Amacı
Core	.NET 9	Ana Framework
Architecture	Clean Arch., CQRS	Ayrık Sorumluluklar (Mediator Pattern)
Database	PostgreSQL 15	İlişkisel Veri Saklama (EF Core & Dapper)
Caching	Redis & FusionCache	Hybrid Caching (L1+L2), Distributed Lock
Job Scheduling	Quartz.NET	Periyodik Veri Senkronizasyonu
Resilience	Polly	Retry, Circuit Breaker, Timeout Policies
Observability	OpenTelemetry	Trace ve Metric Toplama
Monitoring	Prometheus & Grafana	Sistem Metriklerini Görselleştirme
Tracing	Jaeger	Dağıtık İstek Takibi
Testing	xUnit, Moq, Testcontainers	Unit ve Integration Testler (Docker tabanlı)
🚀 Kurulum ve Çalıştırma (Docker)

En kolay ve en güvenilir kurulum yöntemi Docker Compose kullanmaktır.
Gereksinimler

    Docker Desktop (veya Docker Engine)

Adımlar

    Repoyu klonlayın:
    Bash

git clone [https://github.com/username/MB.PosSelection.git](https://github.com/username/MB.PosSelection.git)
cd MB.PosSelection

Sistemi temiz bir şekilde derleyin ve ayağa kaldırın:
Bash

    # Önbellek kullanmadan temiz kurulum (Önerilen)
    docker-compose build --no-cache
    docker-compose up -d

    Servislerin hazır olduğunu doğrulayın:

        API Swagger: http://localhost:5000/swagger

        Grafana (Metrics): http://localhost:3000 (User: admin, Pass: admin)

        Jaeger (Tracing): http://localhost:16686

        Health Check: http://localhost:5000/health/ready

Manuel Veri Tetikleme (Opsiyonel)

Sistem 23:59'da otomatik güncellenir. Ancak test için manuel tetiklemek isterseniz:
Bash

curl -X POST http://localhost:5000/api/v1/pos/sync-rates

💻 Geliştirme Ortamı Kurulumu

Docker kullanmadan (IDE üzerinden) çalıştırmak isterseniz:

    Altyapı: Yerel makinenizde PostgreSQL (Port 5432) ve Redis (Port 6379) çalıştığından emin olun.

    Config: appsettings.json dosyasındaki ConnectionString'leri yerel ayarlarınıza göre düzenleyin.

    Migration:
    Bash

    dotnet ef database update --project src/MB.PosSelection.Infrastructure --startup-project src/MB.PosSelection.Api

    Run: Projeyi Visual Studio veya VS Code üzerinden başlatın.

✨ Temel Özellikler ve Yetenekler
1. Akıllı Fiyatlandırma ve Seçim

Algoritma aşağıdaki hiyerarşiyi (Chain of Responsibility) uygular:

    Maliyet (Cost): Tutar * Komisyon (MinFee ve USD kur farkı dahil).

    Öncelik (Priority): Eşit maliyette önceliği yüksek banka kazanır.

    Komisyon Oranı: Eşitlikte daha düşük oranlı banka kazanır.

    İsim: Eşitlikte alfabetik sıra.

2. O(1) Performanslı Arama

Geleneksel Where sorguları yerine, veriler bellekte PosRateLookupIndex üzerinde Hash Map (Dictionary) olarak tutulur. Milyonlarca kayıt olsa bile arama süresi sabittir.
3. Operasyonel Esneklik

    Dynamic Config: USD çarpanı (1.01) gibi iş kuralları kod tekrarı olmadan konfigürasyonla yönetilir.

    Rate Limiting: IP bazlı kısıtlama ile sistem DDoS saldırılarına karşı korunur.

📊 Gözlemlenebilirlik (Observability)

Sistem "Kara Kutu" değildir. Tüm iç süreçler şeffaftır.

    Business Metrics: "Kaç istek geldi?", "Ortalama komisyon oranı ne?", "Cache Hit oranı ne?" soruları Grafana üzerinden anlık izlenir.

    Distributed Tracing: Bir isteğin Controller -> Cache -> Database yolculuğu Jaeger üzerinden adım adım takip edilebilir.

🧪 Test Stratejisi

Projede Test Piramidi prensibi uygulanmıştır.

    Unit Tests: İş mantığı (Rules, Calculators) dış bağımlılık olmadan test edilir.

        Komut: dotnet test tests/MB.PosSelection.UnitTests

    Integration Tests: Veritabanı ve Cache entegrasyonu, Testcontainers kütüphanesi kullanılarak gerçek Docker konteynerleri üzerinde test edilir. Mock DB kullanılmaz.

        Komut: dotnet test tests/MB.PosSelection.IntegrationTests

