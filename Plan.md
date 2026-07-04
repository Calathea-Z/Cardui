Monarch Lite

A personal finance dashboard inspired by Monarch Money, built for learning and personal use.

Goals

The purpose of this project is to:

Learn how financial aggregation platforms work.
Build a polished, production-quality full-stack application.
Create a personal finance dashboard without paying for a subscription service.
Gain experience integrating with real-world financial APIs.
Serve as a portfolio project demonstrating modern full-stack architecture.
Tech Stack
Frontend
Next.js (App Router)
React
TypeScript
Tailwind CSS
shadcn/ui
Recharts
react-plaid-link
Backend
.NET 8 Web API
Entity Framework Core
PostgreSQL
Going.Plaid SDK
Infrastructure
Docker
PostgreSQL 16
Git
Planned Architecture
+------------------+
|     Next.js      |
|  Mobile First UI |
+--------+---------+
|
HTTPS / REST
|
+--------v---------+
|    .NET Web API  |
+--------+---------+
|
+-----------+------------+
|                        |
PostgreSQL                 Plaid API
|                        |
+-----------+------------+
|
Background Sync
(Future)
Current Project Structure
monarch-lite/
│
├── MonarchLite.sln
│
├── api/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── api.csproj
│
├── frontend/
│
├── docker-compose.yml
│
└── README.md
Current Progress
Completed
Repository initialized
Git configured
Docker Compose configured
PostgreSQL container running
.NET 8 Web API created
Next.js application created
Tailwind configured
shadcn/ui installed
Recharts installed
Plaid React SDK installed
Going.Plaid SDK added to API
Solution file created
API and frontend successfully running locally
Current Development Environment
Database

PostgreSQL running in Docker

Host: localhost
Port: 5433
Database: monarch_lite
Username: monarch
Password: monarch_dev_password
Frontend

Runs using:

npm run dev
Backend

Runs using:

dotnet run
Phase 1 MVP

The first milestone is intentionally simple.

Users should be able to:

Connect a bank account using Plaid
Retrieve accounts
Retrieve transactions
Store transactions in PostgreSQL
View transactions
View account balances
View spending by category
View monthly spending

If those features work, the application already replaces much of the core functionality of a paid budgeting app.

Planned Database

Initial tables:

Accounts
Transactions
Categories
PlaidItems

Later additions:

Budgets
MonthlySnapshots
Assets
Debts
Goals
Rules
Tags
NetWorthHistory
Planned Screens
Dashboard

Displays:

Net worth
Cash balance
Credit card balance
Monthly spending
Income vs expenses
Recent transactions
Accounts

Displays:

Linked accounts
Current balances
Available balances
Transactions

Features:

Search
Filtering
Manual categorization
Merchant details
Notes
Budgets

Displays:

Monthly category budgets
Current progress
Remaining budget
Debt

Displays:

Current balances
Interest rates
Monthly payments
Snowball vs avalanche payoff projections
Future Enhancements
Background transaction synchronization
Webhooks
Spending insights
AI transaction categorization
Cash-flow forecasting
Investment tracking
Net worth history
Bill reminders
Receipt attachment support
Dark mode improvements
Progressive Web App (PWA)
Native mobile app using React Native / Expo
Long-Term Vision

This project is intended to become a polished personal finance platform that combines the best features of Monarch Money with custom functionality tailored to my own financial goals.

Rather than simply cloning an existing product, the objective is to build an application that is:

Fast
Mobile-first
Highly customizable
Educational to build
Useful in everyday life
Representative of production-quality software engineering practices