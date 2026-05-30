# E-Verland

E-Verland is an e-commerce backend API built with ASP.NET Core (.NET 10), following a **Modular Monolith** architecture. The application is organized into self-contained feature modules — each with its own database context, domain layer, and infrastructure — all deployed as a single host.

---

## Tech Stack

| Category      | Technology                           |
| ------------- | ------------------------------------ |
| Framework     | ASP.NET Core (.NET 10)               |
| Architecture  | Modular Monolith, CQRS (MediatR 12)  |
| ORM           | Entity Framework Core 10             |
| Relational DB | PostgreSQL (per-module, via Npgsql)  |
| Cache         | Redis (StackExchange.Redis)          |
| Auth          | JWT Bearer + BCrypt password hashing |
| Validation    | FluentValidation                     |
| Mapping       | AutoMapper 12                        |
| Real-time     | Server-Sent Events (SSE)             |
| API Docs      | Swagger / Swashbuckle 6              |
| Env Config    | DotNetEnv                            |

---

## Project Structure

```
E-Verland/
+-- Host/                        # ASP.NET Core startup project
+-- Extension/                  # Global exception handling, authorization policies, rate limiting
+-- Modules/
|   +-- Auth/
|   +-- User/
|   +-- Product/
|   +-- Cart/
|   +-- Order/
|   +-- Payment/                #COD
|   +-- Chat/
|   +-- Notification/
|   +-- Redis/
+-- SharedKernel/                # BaseEntity, IRepository, Pagination, Validators
```

Each module (except Redis) follows a 4-layer layout:

```
Modules/<Module>/
+-- Api/Controllers/
+-- Application/
|   +-- Commands/
|   +-- Queries/
|   +-- DTOs/
|   +-- Contracts/
+-- Domain/
+-- Infrastructure/
    +-- Persistence/   (DbContext, Migrations, Module registration)
    +-- Repository/
```

---

## Modules

| Module           | Responsibility                                                             | Own DB           |
| ---------------- | -------------------------------------------------------------------------- | ---------------- |
| **Auth**         | Login, JWT refresh, OTP-based email verification, password reset           | `AuthDb`         |
| **User**         | Account management, profiles, addresses, bank accounts                     | `UserDb`         |
| **Product**      | Product catalog, SKU variants, brands, hierarchical categories             | `ProductDb`      |
| **Cart**         | Per-user shopping cart and cart items                                      | `CartDb`         |
| **Order**        | Order lifecycle (Pending -> Confirmed -> Shipping -> Completed / Canceled) | `OrderDb`        |
| **Payment**      | Payment record tracking linked to orders                                   | `PaymentDb`      |
| **Chat**         | 1-1 support conversations between users and admins                         | `ChatDb`         |
| **Notification** | Real-time push notifications via SSE                                       | `NotificationDb` |
| **Redis**        | JWT cache, product cache, cart cache (configured; toggleable)              | -                |

---

## Architecture

**Key design decisions:**

- **Isolated databases per module** — no cross-module EF navigation properties; modules reference each other by ID only.
- **CQRS via MediatR** — all business logic flows through Commands and Queries; each module registers its own MediatR assembly.
- **Real-time via SSE** — `NotificationService` is a singleton that manages in-memory `StreamWriter` connections per user. No SignalR dependency.
- **No shared DbContext** — each module runs its own EF migrations independently.
- **No message bus** — no inter-module eventing at this stage; cross-module coupling is at the DI/service level.
- **Single host deployment** — all modules are compiled into the host via `<Compile Include="..\Modules\**\*.cs" />`.

---

## Domain Overview

### Users & Auth

- Three roles: **Admin**, **User**, **Seller**.
- Accounts include profile, multiple addresses (Province/District/Ward), and bank accounts.
- Email-verified registration via 6-digit OTP (10 min expiry, max 3 attempts).
- JWT access + refresh token flow; Redis-backed token blacklist (when Redis module is active).

### Products

- Products have virtual price, base price, JSON attributes, image URLs, and status (`Active` / `Inactive` / `OutOfStock` / `Pending`).
- SKUs model product variants with `OptionValues` (e.g. `{"Color": "Red", "Size": "M"}`).
- GIN indexes on JSONB columns for efficient attribute/variant filtering.
- Categories support unlimited parent-child nesting.

### Orders & Payments

- Orders snapshot receiver info at creation time (name, phone, address).
- Payment methods: **Online Banking**, **COD**.
- Payment webhook endpoint is publicly accessible for gateway callbacks.
- Strictest rate limiting on the Payment module (5 req/min per user).

### Chat & Notifications

- One conversation per user-admin pair (enforced by unique constraint).
- Messages are editable.
- Notifications are stored in PostgreSQL and pushed in real-time over SSE.

---

## Rate Limiting

Token-bucket rate limits per module (requests/minute, keyed per user JWT or IP):

| Module       | Limit     |
| ------------ | --------- |
| Auth         | 10 / min  |
| Payment      | 5 / min   |
| Order        | 20 / min  |
| User         | 30 / min  |
| Cart         | 50 / min  |
| Chat         | 50 / min  |
| Product      | 100 / min |
| Notification | 100 / min |
| Default      | 60 / min  |

---

## Configuration

The application reads configuration from `appsettings.json` and a `.env` file at the repo root (loaded via DotNetEnv). The `.env` file provides all secret values and connection strings and **must not be committed**.

Create a `.env` file at the repo root with the following variables:

```dotenv
# JWT
JWT_KEY=<your-base64-secret>
JWT_ISSUER=http://localhost:8080
JWT_AUDIENCE=http://localhost:8080

# PostgreSQL — one database per module
AuthDb=Host=...;Database=Auth;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
UserDb=Host=...;Database=User;Username=...;Password=...
ProductDb=Host=...;Database=Product;Username=...;Password=...
CartDb=Host=...;Database=Cart;Username=...;Password=...
OrderDb=Host=...;Database=Order;Username=...;Password=...
PaymentDb=Host=...;Database=Payment;Username=...;Password=...
ChatDb=Host=...;Database=Chat;Username=...;Password=...
NotificationDb=Host=...;Database=Notification;Username=...;Password=...

# Redis
Redis=<host>:<port>,user=default,password=<password>,ssl=true,abortConnect=false

# Email (Gmail SMTP)
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__UserName=<gmail-address>
Email__Smtp__Password=<gmail-app-password>
Email__Smtp__SmtpEnableSsl=true
Email__Smtp__FromName=E-Verland
```

Non-secret defaults in `appsettings.json`:

```json
{
  "App": {
    "BackendUrl": "http://localhost:8080",
    "FrontendUrl": "http://localhost:3000"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:8080" }
    }
  }
}
```

---

## Getting Started

1. **Prerequisites:** .NET 10 SDK, PostgreSQL, Redis.

2. Create a `.env` file at the repo root (see [Configuration](#configuration) above).

3. Apply migrations for each module:

   ```bash
   dotnet ef database update --context AuthDbContext --project Host
   dotnet ef database update --context UserDbContext --project Host
   dotnet ef database update --context ProductDbContext --project Host
   dotnet ef database update --context CartDbContext --project Host
   dotnet ef database update --context OrderDbContext --project Host
   dotnet ef database update --context PaymentDbContext --project Host
   dotnet ef database update --context ChatDbContext --project Host
   dotnet ef database update --context NotificationDbContext --project Host
   ```

4. Run the host:

   ```bash
   dotnet run --project Host
   ```

5. Open Swagger at `http://localhost:8080/swagger`.
