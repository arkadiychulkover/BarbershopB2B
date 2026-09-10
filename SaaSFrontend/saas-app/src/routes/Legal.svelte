<script>
  import { onMount } from 'svelte';
  import { 
    Shield, 
    FileText, 
    Lock, 
    Scale, 
    ArrowLeft, 
    Sun, 
    Moon, 
    CheckCircle2, 
    ExternalLink, 
    Printer, 
    Send, 
    Building2,
    Database,
    Cpu,
    HelpCircle,
    UserCheck,
    Coins,
    Globe
  } from 'lucide-svelte';
  import { theme, toggleTheme } from '../lib/theme';
  import { currentLocale } from '../lib/locale.js';
  import LanguageSwitcher from '../components/LanguageSwitcher.svelte';
  import { m } from '../lib/paraglide/messages.js';

  // Active tab: 'privacy' or 'terms'
  let activeTab = 'privacy';

  onMount(() => {
    // Check url hash or query param if user came directly to #terms or /terms
    const hash = window.location.hash;
    const pathname = window.location.pathname;
    if (pathname.includes('terms') || hash === '#terms') {
      activeTab = 'terms';
    } else {
      activeTab = 'privacy';
    }
  });

  function setTab(tab) {
    activeTab = tab;
    if (typeof window !== 'undefined') {
      window.history.replaceState(null, '', tab === 'terms' ? '/terms' : '/privacy');
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  function handlePrint() {
    if (typeof window !== 'undefined') {
      window.print();
    }
  }

  $: isEn = $currentLocale === 'en';
</script>

<svelte:head>
  <title>{activeTab === 'privacy' ? (isEn ? 'Privacy Policy — ARCH SYSTEM' : 'Політика конфіденційності — ARCH SYSTEM') : (isEn ? 'Terms of Service — ARCH SYSTEM' : 'Умови використання та Публічна оферта — ARCH SYSTEM')}</title>
  <meta name="description" content="Офіційні юридичні документи хмарної платформи ARCH SYSTEM: Політика конфіденційності, захист персональних даних та умови використання сервісу." />
</svelte:head>

<div class="legal-page">
  <!-- Header -->
  <header class="legal-header">
    <div class="container header-container">
      <div class="header-left">
        <a href="/" class="brand-link" title="ARCH SYSTEM">
          <span class="brand-dot"></span>
          <span class="brand-title">ARCH SYSTEM</span>
        </a>
        <span class="legal-badge">
          <Scale size={13} />
          <span>Legal Center</span>
        </span>
      </div>

      <div class="header-actions">
        <LanguageSwitcher />
        <button class="btn btn-ghost theme-btn" on:click={toggleTheme} aria-label="Toggle theme">
          {#if $theme === 'dark'}
            <Sun size={17} />
          {:else}
            <Moon size={17} />
          {/if}
        </button>
        <button class="btn btn-ghost btn-sm print-btn" on:click={handlePrint} title={isEn ? "Print / Save PDF" : "Друкувати / Зберегти в PDF"}>
          <Printer size={15} />
          <span class="btn-text">{isEn ? "Print" : "Друк"}</span>
        </button>
        <a href="/" class="btn btn-primary btn-sm back-link">
          <ArrowLeft size={15} />
          <span>{m.common_back()}</span>
        </a>
      </div>
    </div>
  </header>

  <!-- Hero Section -->
  <section class="legal-hero">
    <div class="container hero-container">
      <div class="hero-chip">
        <Shield size={14} />
        <span>{isEn ? "Legal & Data Protection Compliance" : "Юридична безпека та захист персональних даних"}</span>
      </div>
      <h1 class="hero-title">
        {activeTab === 'privacy' 
          ? (isEn ? "Privacy & Data Protection Policy" : "Політика конфіденційності та захисту даних") 
          : (isEn ? "Terms of Service & Public Offer" : "Умови використання та Публічна оферта")}
      </h1>
      <p class="hero-subtitle">
        {isEn 
          ? "Compliant with the Law of Ukraine «On Personal Data Protection» No. 2297-VI, the Civil Code of Ukraine, and EU GDPR standards."
          : "Складено відповідно до Закону України «Про захист персональних даних» № 2297-VI, Цивільного кодексу України, Закону «Про електронну комерцію» та стандартів GDPR ЄС."}
      </p>

      <!-- Document Navigation Tabs -->
      <div class="tab-switcher" role="tablist">
        <button 
          class="tab-btn" 
          class:active={activeTab === 'privacy'} 
          on:click={() => setTab('privacy')}
          role="tab"
          aria-selected={activeTab === 'privacy'}
        >
          <Lock size={16} />
          <span>{isEn ? "Privacy Policy" : "Політика конфіденційності"}</span>
        </button>

        <button 
          class="tab-btn" 
          class:active={activeTab === 'terms'} 
          on:click={() => setTab('terms')}
          role="tab"
          aria-selected={activeTab === 'terms'}
        >
          <FileText size={16} />
          <span>{isEn ? "Terms of Service" : "Умови використання (Оферта)"}</span>
        </button>
      </div>

      <div class="doc-meta-info">
        <span>{isEn ? "Last updated: September 2026" : "Редакція діє з: Вересень 2026"}</span>
        <span class="meta-dot">•</span>
        <span>{isEn ? "Jurisdiction: Ukraine / International" : "Юрисдикція: Україна / Міжнародне право"}</span>
      </div>
    </div>
  </section>

  <!-- Document Body Content -->
  <main class="container legal-main">
    <div class="content-card">
      
      {#if activeTab === 'privacy'}
        <!-- PRIVACY POLICY (UKR / EN) -->
        {#if !isEn}
          <!-- Ukrainian / RU Legal Version -->
          <article class="doc-article">
            <div class="doc-intro-box">
              <div class="intro-icon"><Shield size={22} /></div>
              <div class="intro-text">
                Ця Політика конфіденційності регулює порядок збору, обробки, захисту та зберігання персональних даних користувачів хмарної платформи <strong>ARCH SYSTEM</strong> (далі — «Сервіс» або «Платформа»), включаючи веб-панель керування та клієнтські Telegram Mini App інтерфейси онлайн-запису.
              </div>
            </div>

            <h2>1. Загальні положення та правові підстави</h2>
            <p>
              1.1. Справжня Політика розроблена відповідно до вимог <strong>Закону України «Про захист персональних даних» № 2297-VI від 01.06.2010</strong>, Цивільного кодексу України, Закону України «Про електронну комерцію» № 675-VIII, а також з урахуванням положень Загального регламенту захисту даних Європейського Союзу <strong>(GDPR, Regulation EU 2016/679)</strong>.
            </p>
            <p>
              1.2. Реєструючись у Сервісі, підключаючи Telegram-бота або оформлюючи підписку, Користувач підтверджує, що він повністю ознайомлений із цією Політикою і надає добровільну та однозначну згоду на обробку персональних даних на зазначених умовах.
            </p>

            <h2>2. Розмежування ролей (B2B модель: Контролер та Розпорядник)</h2>
            <p>
              Оскільки ARCH SYSTEM є платформою автоматизації для підприємців (барбершопів, салонів краси, приватних майстрів):
            </p>
            <ul>
              <li>
                <strong>Власник бізнесу / Партнер</strong> виступає <em>Володільцем (Контролером даних / Data Controller)</em> щодо клієнтської бази свого закладу (ПІБ клієнтів, телефони, історія візитів). Партнер самостійно забезпечує законність збору персональних даних від своїх відвідувачів.
              </li>
              <li>
                <strong>ARCH SYSTEM</strong> виступає <em>Розпорядником (Оператором даних / Data Processor)</em>, надаючи виключно захищену хмарну інфраструктуру, бази даних та технічну взаємодію з Telegram API для забезпечення функціоналу запису.
              </li>
            </ul>

            <h2>3. Які дані збирає та обробляє Сервіс</h2>
            <p>3.1. <strong>Дані облікового запису партнера:</strong></p>
            <ul>
              <li>Прізвище, ім'я або псевдонім власника закладу/адміністратора;</li>
              <li>Контактний номер мобільного телефону та адреса електронної пошти (Email);</li>
              <li>Ідентифікатор Telegram (Telegram ID) та ім'я користувача (@username);</li>
              <li>Хешовані криптографічні паролі (паролі у відкритому вигляді ніколи не зберігаються).</li>
            </ul>
            <p>3.2. <strong>Дані закладу та інтеграцій:</strong></p>
            <ul>
              <li>Назва закладу, фактична адреса, графік роботи, часовий пояс;</li>
              <li>Списки співробітників, графіки змін, каталог та вартість послуг;</li>
              <li>API-токен Telegram-бота (Bot Token), який шифрується та використовується суворо для передачі вебхуків розкладу та відправлення сповіщень.</li>
            </ul>
            <p>3.3. <strong>Дані клієнтів закладу (в рамках CRM):</strong></p>
            <ul>
              <li>Ім'я або нікнейм клієнта у Telegram;</li>
              <li>Номер телефону (вказаний клієнтом під час бронювання);</li>
              <li>Історія записів, обрані майстри, час та статус процедури.</li>
            </ul>
            <p>3.4. <strong>Технічні дані:</strong> IP-адреси, параметри браузера, файли cookie сесій та токени авторизації (JWT), необхідні для безпечного входу в панель. Сервіс не зберігає платіжні дані банківських карток — оплата здійснюється децентралізовано в блокчейні TON.</p>

            <h2>4. Мета обробки персональних даних</h2>
            <ul>
              <li>Створення та адміністрування облікового запису в системі ARCH SYSTEM;</li>
              <li>Забезпечення безперебійної роботи онлайн-запису клієнтів через Telegram Mini App;</li>
              <li>Миттєве інформування майстрів та клієнтів про нові записи, перенесення або скасування;</li>
              <li>Формування фінансової та статистичної аналітики для керівника закладу;</li>
              <li>Захист від несанкціонованого доступу, кібератак та шахрайства;</li>
              <li>Надання технічної підтримки користувачам.</li>
            </ul>

            <h2>5. Захист та безпека даних</h2>
            <p>
              5.1. ARCH SYSTEM застосовує багаторівневі організаційні та технічні заходи безпеки: шифрування трафіку за протоколом TLS/HTTPS, ізоляцію баз даних замовників (Multi-Tenancy), соління та хешування облікових даних, захист токенів ботів.
            </p>
            <p>
              5.2. Платформа <strong>ні за яких обставин не продає, не передає в оренду та не відчужує</strong> персональні дані клієнтів та партнерів стороннім рекламним брокерам, маркетинговим агенціям чи третім особам.
            </p>

            <h2>6. Передача даних третім особам</h2>
            <p>Передача даних можлива виключно у таких випадках:</p>
            <ul>
              <li><strong>Telegram FZ-LLC (Telegram API):</strong> для доставки повідомлень ботом та відображення веб-інтерфейсу Mini App;</li>
              <li><strong>Хмарні провайдери та дата-центри:</strong> для безпечного хостингу серверних потужностей;</li>
              <li><strong>На законну вимогу державних органів України:</strong> виключно за наявності рішення суду або в межах чинного законодавства України.</li>
            </ul>

            <h2>7. Права суб'єктів персональних даних</h2>
            <p>Згідно зі <strong>статтею 8 Закону України «Про захист персональних даних»</strong> та положеннями GDPR, ви маєте право:</p>
            <ul>
              <li>Знати про джерела збирання, місцезнаходження своїх даних та мету їх обробки;</li>
              <li>Отримувати інформацію про умови надання доступу до персональних даних;</li>
              <li>Пред'являти вмотивовану вимогу щодо зміни або знищення своїх персональних даних;</li>
              <li>На захист своїх даних від незаконної обробки та випадкової втрати;</li>
              <li>Відкликати згоду на обробку персональних даних або вимагати видалення облікового запису (Right to be Forgotten);</li>
              <li>Звертатися зі скаргами до Уповноваженого Верховної Ради України з прав людини або до суду.</li>
            </ul>

            <h2>8. Термін зберігання даних та видалення</h2>
            <p>
              Дані зберігаються протягом усього терміну дії облікового запису та активної підписки. У разі запиту на видалення облікового запису через підтримку або налаштування, персональні дані видаляються або безповоротно знеособлюються протягом 30 календарних днів, за винятком даних, обов'язкових для збереження згідно з фінансовим законодавством.
            </p>

            <h2>9. Контакти та звернення</h2>
            <p>
              З усіх питань щодо конфіденційності, експорту або видалення даних ви можете звернутися до офіційної служби підтримки:
            </p>
            <div class="contact-card">
              <Send size={18} />
              <span>Офіційний Telegram підтримки:</span>
              <a href="https://t.me/Eyed_Graff" target="_blank" rel="noopener noreferrer">@Eyed_Graff</a>
            </div>
          </article>
        {:else}
          <!-- English Legal Version -->
          <article class="doc-article">
            <div class="doc-intro-box">
              <div class="intro-icon"><Shield size={22} /></div>
              <div class="intro-text">
                This Privacy Policy outlines how <strong>ARCH SYSTEM</strong> ("Service", "We", "Platform") collects, uses, protects, and stores personal data across our cloud management panel and customer-facing Telegram Mini App booking interfaces.
              </div>
            </div>

            <h2>1. Legal Framework & General Provisions</h2>
            <p>
              1.1. This Policy complies with the <strong>Law of Ukraine «On Personal Data Protection» No. 2297-VI</strong>, the Civil Code of Ukraine, the Law of Ukraine «On Electronic Commerce», and adheres to the standards of the <strong>General Data Protection Regulation (EU GDPR 2016/679)</strong>.
            </p>
            <p>
              1.2. By creating an account, connecting a Telegram Bot, or purchasing a subscription, the User unconditionally agrees to the terms of this Privacy Policy and consents to personal data processing.
            </p>

            <h2>2. Roles (Data Controller vs. Data Processor)</h2>
            <p>
              Under our B2B SaaS architecture:
            </p>
            <ul>
              <li>
                <strong>Salon Owner / Partner</strong> is the <em>Data Controller</em> regarding the database of their salon's clients (names, phone numbers, appointment history). The Partner guarantees lawful basis for collecting customer information.
              </li>
              <li>
                <strong>ARCH SYSTEM</strong> acts as the <em>Data Processor</em>, providing secure cloud infrastructure, databases, and Telegram API routing required to operate the booking system.
              </li>
            </ul>

            <h2>3. Categories of Collected Data</h2>
            <p>3.1. <strong>Partner Account Data:</strong> Name, phone number, email address, Telegram ID, @username, and securely salted/hashed passwords.</p>
            <p>3.2. <strong>Business & Integration Data:</strong> Salon name, address, working hours, services, master profiles, and encrypted Telegram Bot API Tokens used solely to operate notifications and webhooks.</p>
            <p>3.3. <strong>Client Appointment Data (CRM):</strong> Client name, phone number, visit dates, preferred barbers, procedure notes.</p>
            <p>3.4. <strong>Technical Data:</strong> IP address, browser signatures, authentication session cookies/JWT tokens. No credit card details are stored (crypto payments are processed decentralized on the TON blockchain).</p>

            <h2>4. Purposes of Processing</h2>
            <ul>
              <li>Providing SaaS platform functionality and booking management;</li>
              <li>Sending automated reminders and schedule updates via Telegram Bot;</li>
              <li>Generating revenue and workload analytics for salon managers;</li>
              <li>Ensuring security, rate limiting, and fraud prevention;</li>
              <li>Delivering technical customer support.</li>
            </ul>

            <h2>5. Data Security & Storage</h2>
            <p>
              We implement industry-grade encryption (TLS/HTTPS), isolated multi-tenant database access, token salting, and firewalls. We <strong>never sell or rent</strong> personal data to advertising brokers or third-party marketers.
            </p>

            <h2>6. Third-Party Disclosures</h2>
            <p>Data may only be processed by:</p>
            <ul>
              <li><strong>Telegram FZ-LLC:</strong> to deliver bot messages and render Mini App components;</li>
              <li><strong>Cloud Hosting Infrastructure:</strong> for reliable high-availability hosting;</li>
              <li><strong>Law Enforcement Authorities:</strong> only when strictly mandated by a valid Ukrainian or international court warrant.</li>
            </ul>

            <h2>7. User Rights</h2>
            <p>Under statutory Ukrainian Law (Art. 8, Law No. 2297-VI) and GDPR, you have the right to:</p>
            <ul>
              <li>Request access to and a portable copy of your stored personal data;</li>
              <li>Rectify inaccurate or outdated records;</li>
              <li>Withdraw consent and request total deletion of your account (Right to Erasure);</li>
              <li>Lodge a complaint with the Ukrainian Parliament Commissioner for Human Rights or competent supervisory authority.</li>
            </ul>

            <h2>8. Contact Information</h2>
            <div class="contact-card">
              <Send size={18} />
              <span>Official Telegram Support:</span>
              <a href="https://t.me/Eyed_Graff" target="_blank" rel="noopener noreferrer">@Eyed_Graff</a>
            </div>
          </article>
        {/if}

      {:else}
        <!-- TERMS OF SERVICE (UKR / EN) -->
        {#if !isEn}
          <article class="doc-article">
            <div class="doc-intro-box">
              <div class="intro-icon"><FileText size={22} /></div>
              <div class="intro-text">
                Цей документ є <strong>Публічною офертою (договором приєднання)</strong> відповідно до ст. 633 та 634 Цивільного кодексу України між сервісом <strong>ARCH SYSTEM</strong> та будь-якою дієздатною фізичною чи юридичною особою (Користувачем).
              </div>
            </div>

            <h2>1. Предмет договору</h2>
            <p>
              1.1. ARCH SYSTEM надає Користувачеві право обмеженого, невиключного доступу до хмарного програмного комплексу (SaaS) для автоматизації запису клієнтів, управління розкладом майстрів, ведення клієнтської бази та інтеграції з Telegram Mini App.
            </p>
            <p>
              1.2. Реєстрація облікового запису на сайті, натискання кнопки «Зареєструвати заклад» або оплата тарифу вважається повним і беззастережним акцептом цієї Оферти.
            </p>

            <h2>2. Реєстрація та безпека облікового запису</h2>
            <p>
              2.1. Користувач зобов'язується надати достовірні контактні дані під час реєстрації (номер телефону, ім'я, актуальний Telegram ID).
            </p>
            <p>
              2.2. Користувач несе повну персональну відповідальність за збереження свого пароля та API-токена Telegram-бота. Будь-які дії, здійснені в кабінеті з використанням облікових даних Користувача, визнаються вчиненими самим Користувачем.
            </p>

            <h2>3. Вартість послуг, оплата та блокчейн-транзакції</h2>
            <p>
              3.1. Доступ до функціоналу платформи надається на умовах передплати (підписки). Базова вартість становить <strong>18 TON</strong> (або актуальна сума, визначена у системі) за 30 календарних днів користування.
            </p>
            <p>
              3.2. Оплата здійснюється цифровими активами в децентралізованій мережі <strong>The Open Network (TON)</strong> шляхом переказу коштів на смарт-гаманець платформи. Активація або продовження підписки відбувається автоматично після підтвердження транзакції валідаторами блокчейну.
            </p>
            <p>
              3.3. <strong>Політика повернень:</strong> Враховуючи специфіку та незворотність блокчейн-транзакцій у мережі TON, сплачені кошти не підлягають автоматичному поверненню. У разі виникнення форс-мажорних обставин або технічних збоїв сервісу, питання компенсації чи продовження періоду підписки вирішується індивідуально через підтримку.
            </p>

            <h2>4. Правила використання Telegram-бота</h2>
            <p>
              4.1. Користувач гарантує, що підключений Telegram-бот використовується виключно для законних цілей надання послуг запису клієнтів.
            </p>
            <p>
              4.2. <strong>Суворо забороняється:</strong> використання платформи для розсилки несанкціонованого спаму, шахрайства, поширення шкідливого програмного забезпечення або контенту, що порушує законодавство України чи правила Telegram Terms of Service.
            </p>
            <p>
              4.3. ARCH SYSTEM залишає за собою право тимчасово заблокувати доступ закладу у разі виявлення грубих порушень правил Telegram або спроб злому системи.
            </p>

            <h2>5. Обмеження відповідальності (Disclaimer)</h2>
            <p>
              5.1. Програмний комплекс надається за міжнародним стандартом <strong>«AS IS» («ЯК Є»)</strong>. ARCH SYSTEM докладає всіх комерційно обґрунтованих зусиль для забезпечення безперебійної роботи 24/7, проте не гарантує абсолютної відсутності технічних помилок чи збоїв, спричинених зовнішніми факторами.
            </p>
            <p>
              5.2. Платформа <strong>не несе відповідальності</strong> за:
            </p>
            <ul>
              <li>Збої, затримки або блокування ботів на стороні інфраструктури месенджера Telegram;</li>
              <li>Перебої в роботі блокчейн-мережі The Open Network (TON) чи затримки підтвердження транзакцій;</li>
              <li>Втрату прибутку або непрямі збитки закладу через неявку клієнтів чи невірне налаштування графіка майстрів власником;</li>
              <li>Дії третіх осіб або обставини непереборної сили (форс-мажор).</li>
            </ul>

            <h2>6. Інтелектуальна власність</h2>
            <p>
              Усі права на вихідний код, дизайн, логотипи, інтерфейси та торговельні знаки ARCH SYSTEM належать правовласникам сервісу і захищені законодавством України про інтелектуальну власність.
            </p>

            <h2>7. Зміни умов та розірвання договору</h2>
            <p>
              ARCH SYSTEM має право вносити зміни до цих Умов, публікуючи оновлену редакцію на цій сторінці. Продовження використання сервісу після набрання чинності змінами означає згоду з новою редакцією.
            </p>

            <h2>8. Служба підтримки та вирішення спорів</h2>
            <p>
              Усі спори вирішуються шляхом переговорів. Офіційний канал зв'язку з адміністрацією сервісу:
            </p>
            <div class="contact-card">
              <Send size={18} />
              <span>Telegram підтримки:</span>
              <a href="https://t.me/Eyed_Graff" target="_blank" rel="noopener noreferrer">@Eyed_Graff</a>
            </div>
          </article>
        {:else}
          <article class="doc-article">
            <div class="doc-intro-box">
              <div class="intro-icon"><FileText size={22} /></div>
              <div class="intro-text">
                This document constitutes a <strong>Public Terms of Service & Subscription Agreement</strong> between <strong>ARCH SYSTEM</strong> and any legal entity or individual ("User", "Partner") utilizing the SaaS platform.
              </div>
            </div>

            <h2>1. Scope of Service</h2>
            <p>
              1.1. ARCH SYSTEM grants the User a non-exclusive, revocable, non-transferable license to access the cloud management dashboard and client Telegram Mini App booking infrastructure.
            </p>
            <p>
              1.2. Creating an account or paying a subscription fee indicates full, unconditional acceptance of these Terms.
            </p>

            <h2>2. Account Security & Responsibilities</h2>
            <p>
              2.1. The User is solely responsible for maintaining the confidentiality of their login credentials and Telegram Bot API tokens.
            </p>
            <p>
              2.2. The User guarantees that their business operates legally and does not use Telegram Bots for prohibited activities or unsolicited spam.
            </p>

            <h2>3. Subscription Fees & Blockchain Transactions</h2>
            <p>
              3.1. The service operates on a subscription model (standard rate: <strong>18 TON</strong> / 30 days).
            </p>
            <p>
              3.2. Payments are completed via digital assets on <strong>The Open Network (TON)</strong>. Access unlocks automatically upon on-chain block validation.
            </p>
            <p>
              3.3. <strong>Refund Policy:</strong> Due to the immutable, irreversible nature of blockchain transactions, payments are non-refundable. Exceptional technical issues will be evaluated on a case-by-case basis via customer support.
            </p>

            <h2>4. Limitation of Liability</h2>
            <p>
              4.1. ARCH SYSTEM is provided on an <strong>"AS IS"</strong> and <strong>"AS AVAILABLE"</strong> basis.
            </p>
            <p>
              4.2. ARCH SYSTEM shall not be liable for Telegram API outages, bot suspensions enforced by Telegram moderation, network congestions on the TON blockchain, or indirect business losses.
            </p>

            <h2>5. Termination & Contact</h2>
            <p>
              You may terminate your account at any time. For legal and billing inquiries:
            </p>
            <div class="contact-card">
              <Send size={18} />
              <span>Telegram Support:</span>
              <a href="https://t.me/Eyed_Graff" target="_blank" rel="noopener noreferrer">@Eyed_Graff</a>
            </div>
          </article>
        {/if}
      {/if}

    </div>

    <!-- Quick Navigation Sidebar / Footer Card -->
    <div class="quick-nav-box">
      <div class="quick-nav-left">
        <span class="quick-icon"><Building2 size={18} /></span>
        <div>
          <div class="quick-title">ARCH SYSTEM — Enterprise Cloud SaaS</div>
          <div class="quick-sub">{isEn ? "Designed for Barbershops, Salons & Solo Masters" : "Розроблено для барбершопів, салонів краси та приватних майстрів"}</div>
        </div>
      </div>
      <div class="quick-nav-right">
        <a href="/register" class="btn btn-primary btn-sm glow">
          <span>{m.auth_register_title()}</span>
        </a>
      </div>
    </div>
  </main>
</div>

<style>
  .legal-page {
    min-height: 100vh;
    background-color: var(--bg-canvas);
    color: var(--text-primary);
    padding-bottom: 5rem;
  }

  /* Header */
  .legal-header {
    position: sticky;
    top: 0;
    z-index: 40;
    background: var(--bg-surface);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-bottom: 1px solid var(--border-subtle);
    padding: 0.85rem 0;
  }

  .header-container {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .brand-link {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    text-decoration: none;
  }

  .brand-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--pastel-rose), var(--pastel-sage));
    box-shadow: 0 0 12px var(--pastel-rose-glow);
  }

  .brand-title {
    font-size: 1.15rem;
    font-weight: 800;
    letter-spacing: -0.02em;
    color: var(--text-primary);
  }

  .legal-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.2rem 0.6rem;
    border-radius: var(--radius-pill);
    background: var(--pastel-sage-dim);
    color: var(--pastel-sage);
    border: 1px solid rgba(152, 193, 169, 0.25);
    font-size: 0.76rem;
    font-weight: 600;
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 0.65rem;
  }

  .theme-btn {
    padding: 0.5rem 0.65rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
  }

  .print-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 0.84rem;
  }

  .back-link {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.45rem 1rem;
    font-size: 0.84rem;
  }

  /* Hero */
  .legal-hero {
    padding: 3.5rem 0 2.5rem;
    text-align: center;
    background: radial-gradient(ellipse 60% 40% at 50% 0%, var(--pastel-rose-dim), transparent 70%);
  }

  .hero-container {
    max-width: 820px;
  }

  .hero-chip {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.35rem 0.9rem;
    border-radius: var(--radius-pill);
    background: var(--pastel-rose-dim);
    color: var(--pastel-rose);
    border: 1px solid rgba(223, 158, 142, 0.2);
    font-size: 0.82rem;
    font-weight: 600;
    margin-bottom: 1.25rem;
  }

  .hero-title {
    font-size: 2.5rem;
    font-weight: 800;
    letter-spacing: -0.025em;
    line-height: 1.2;
    margin-bottom: 0.85rem;
  }

  .hero-subtitle {
    font-size: 1.05rem;
    color: var(--text-secondary);
    line-height: 1.55;
    margin-bottom: 2rem;
  }

  /* Tab switcher */
  .tab-switcher {
    display: inline-flex;
    padding: 0.35rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-pill);
    gap: 0.35rem;
    box-shadow: var(--shadow-sm);
  }

  .tab-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.65rem 1.4rem;
    border-radius: var(--radius-pill);
    background: transparent;
    border: none;
    color: var(--text-secondary);
    font-size: 0.92rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
  }

  .tab-btn:hover {
    color: var(--text-primary);
  }

  .tab-btn.active {
    background: var(--pastel-rose);
    color: #ffffff;
    box-shadow: 0 2px 10px var(--pastel-rose-glow);
  }

  .doc-meta-info {
    margin-top: 1.25rem;
    font-size: 0.82rem;
    color: var(--text-muted);
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.6rem;
  }

  .meta-dot {
    opacity: 0.5;
  }

  /* Main Card */
  .legal-main {
    max-width: 900px;
    margin-top: 1rem;
  }

  .content-card {
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-xl);
    padding: 3rem 3.5rem;
    box-shadow: var(--shadow-md);
  }

  .doc-intro-box {
    display: flex;
    gap: 1rem;
    align-items: flex-start;
    padding: 1.25rem 1.5rem;
    border-radius: var(--radius-md);
    background: var(--pastel-sage-dim);
    border: 1px solid rgba(152, 193, 169, 0.25);
    margin-bottom: 2.25rem;
  }

  .intro-icon {
    color: var(--pastel-sage);
    flex-shrink: 0;
    margin-top: 0.1rem;
  }

  .intro-text {
    font-size: 0.96rem;
    line-height: 1.6;
    color: var(--text-primary);
  }

  .doc-article {
    line-height: 1.7;
    font-size: 0.97rem;
    color: var(--text-secondary);
  }

  .doc-article h2 {
    font-size: 1.35rem;
    font-weight: 700;
    color: var(--text-primary);
    margin-top: 2.5rem;
    margin-bottom: 1rem;
    letter-spacing: -0.015em;
    border-bottom: 1px solid var(--border-subtle);
    padding-bottom: 0.4rem;
  }

  .doc-article h2:first-of-type {
    margin-top: 0;
  }

  .doc-article p {
    margin-bottom: 1rem;
  }

  .doc-article ul {
    margin: 0.75rem 0 1.25rem 1.5rem;
    padding: 0;
  }

  .doc-article li {
    margin-bottom: 0.5rem;
  }

  .doc-article strong {
    color: var(--text-primary);
  }

  .contact-card {
    display: inline-flex;
    align-items: center;
    gap: 0.6rem;
    padding: 0.75rem 1.25rem;
    background: var(--bg-surface-elevated);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-md);
    margin-top: 0.5rem;
    color: var(--text-primary);
  }

  .contact-card a {
    color: var(--pastel-rose);
    font-weight: 700;
    text-decoration: none;
  }

  .contact-card a:hover {
    text-decoration: underline;
  }

  /* Quick Nav footer */
  .quick-nav-box {
    margin-top: 2rem;
    background: var(--bg-surface);
    border: 1px solid var(--border-subtle);
    border-radius: var(--radius-lg);
    padding: 1.25rem 2rem;
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .quick-nav-left {
    display: flex;
    align-items: center;
    gap: 0.85rem;
  }

  .quick-icon {
    color: var(--pastel-rose);
  }

  .quick-title {
    font-weight: 700;
    font-size: 0.95rem;
    color: var(--text-primary);
  }

  .quick-sub {
    font-size: 0.82rem;
    color: var(--text-muted);
  }

  @media (max-width: 768px) {
    .content-card {
      padding: 2rem 1.5rem;
    }
    .hero-title {
      font-size: 1.9rem;
    }
    .tab-btn {
      padding: 0.55rem 1rem;
      font-size: 0.85rem;
    }
    .btn-text {
      display: none;
    }
  }

  @media print {
    .legal-header, .tab-switcher, .quick-nav-box, .print-btn, .back-link {
      display: none !important;
    }
    .legal-page {
      background: #ffffff !important;
      color: #000000 !important;
      padding: 0 !important;
    }
    .content-card {
      border: none !important;
      box-shadow: none !important;
      padding: 0 !important;
      background: #ffffff !important;
    }
    .doc-article {
      color: #000000 !important;
    }
    .doc-article h2 {
      color: #000000 !important;
      border-color: #cccccc !important;
    }
    .doc-intro-box {
      border: 1px solid #cccccc !important;
      background: #f9f9f9 !important;
      color: #000000 !important;
    }
  }
</style>
