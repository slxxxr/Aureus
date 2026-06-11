# Aureus

[English](#english) · [Русский](#русский)

[Tech Stack](#tech-stack) · [Getting started](#getting-started)

---

<details>
<summary><b>📸 Screenshots</b></summary>

<br>

![Dashboard](docs/images/dashboard.png)

![Category dynamics](docs/images/category-dynamics.png)

</details>

---

<a name="english"></a>

## English

Personal finance service. Track income and expenses across accounts and categories, organized by workspaces.

### What's done

- **Auth** — registration and login with JWT
- **Workspaces** — independent spaces for different finances: personal, work, family, and more
- **Financial accounts** — multi-currency accounts with running balance
- **Categories** — income and expense categories
- **Transactions** — list grouped by date with daily net; filter by account and type
- **Dashboard analytics** — three-tab view (Overview / Categories / Dynamics) navigated from the sidebar. Overview: income/expense/net summary cards, income-vs-expense bar chart, breakdowns by account. Categories: accordion list with progress bars; expanding a category loads a name-level breakdown rendered as an interactive donut chart. Dynamics: per-category small-multiples area charts. All tabs filter by period, accounts, and categories; per-currency view throughout

### What's next

- **Budgets** — per-category monthly limits with progress tracking
- **Export** — transactions to CSV, reports to PDF
- **CSV import** — bulk transaction import with duplicate detection and pre-import preview
- **Auto-categorization** — ML model that determines category from transaction name, trained per workspace
- **Expense forecasting** — 30-day forecast based on time series
- **LLM insights** — natural language summaries of spending patterns

---

<a name="русский"></a>

## Русский

Сервис учёта финансов. Учёт доходов и расходов по счетам и категориям, организованный по рабочим областям.

### Что сделано

- **Авторизация** — регистрация и вход по JWT
- **Рабочие области** — возможность создавать независимые пространства под разные финансы: личные, рабочие, семейные и другие
- **Счета** — мультивалютные счета с актуальным балансом
- **Категории** — категории доходов и расходов
- **Транзакции** — список, сгруппированный по датам с дневным нетто; фильтры по счёту и типу
- **Аналитический дашборд** — три вкладки (Обзор / Категории / Динамика) с навигацией через сайдбар. Обзор: карточки сводки доходов/расходов/нетто, бар-чарт доходов и расходов, разбивки по счетам. Категории: аккордеон с прогресс-барами; при открытии категории загружается разбивка по названиям транзакций в виде интерактивной donut-диаграммы. Динамика: мини-графики по каждой категории (small multiples). Все вкладки фильтруются по периоду, счетам и категориям; мультивалютный вид

### Что дальше

- **Бюджеты** — лимиты по категориям на месяц, отслеживание прогресса
- **Экспорт** — транзакции в CSV, отчёты в PDF
- **CSV-импорт** — массовая загрузка транзакций с определением дубликатов и предпросмотром
- **Авто-категоризация** — ML-модель, определяющая категорию по названию, обучается на данных воркспейса
- **Прогнозирование** — прогноз расходов на 30 дней вперёд на основе временных рядов
- **LLM-инсайты** — языковая модель генерирует текстовые выводы по аналитике

---

## Tech Stack

**Backend** — .NET 8 (C#), Entity Framework Core, PostgreSQL. Auth via JWT.

**Frontend** — React + TypeScript (Vite), TanStack Query, Tailwind CSS, react-i18next, Recharts (charts). UI in Russian and English.

**Tests** — unit tests with xUnit + Moq; integration tests with xUnit + Testcontainers.

## Getting started

```bash
# Start the database
docker compose up -d

# Run the API
cd backend/src/Aureus.Api
dotnet run

# Run the frontend
cd frontend
npm install
npm run dev
```
