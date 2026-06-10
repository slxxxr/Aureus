# Aureus

[English](#english) · [Русский](#русский)

[Tech Stack](#tech-stack) · [Getting started](#getting-started)

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
- **Dashboard analytics** — income/expense/net summary, income-vs-expense chart over time, category breakdowns, and per-category dynamics (small-multiples charts showing each category's spend over time); filter by period, accounts, and categories; per-currency view

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
- **Аналитический дашборд** — сводка доходов/расходов/нетто, график доходов и расходов по времени, разбивка по категориям и динамика по категориям (отдельный мини-график на каждую категорию — как менялись её расходы во времени); фильтры по периоду, счетам и категориям; отдельно по каждой валюте

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
