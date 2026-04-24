# E-Verland — Tài Liệu Trình Bày Dự Án

---

## 1. Tổng Quan Dự Án

**E-Verland** là một nền tảng thương mại điện tử (E-Commerce) được xây dựng theo kiến trúc **Modular Monolith**, sử dụng **.NET 10 / ASP.NET Core**. Ứng dụng cung cấp đầy đủ các tính năng từ quản lý sản phẩm, giỏ hàng, đặt hàng, thanh toán đến chat thời gian thực và thông báo đẩy (SSE).

---

## 2. Công Nghệ Sử Dụng

| Hạng mục         | Công nghệ                                |
| ---------------- | ---------------------------------------- |
| Framework        | ASP.NET Core (.NET 10)                   |
| Kiến trúc        | Modular Monolith + CQRS (MediatR 12)     |
| ORM              | Entity Framework Core 10                 |
| Cơ sở dữ liệu    | PostgreSQL (mỗi module 1 database riêng) |
| Cache            | Redis (StackExchange.Redis)              |
| Xác thực         | JWT Bearer + BCrypt                      |
| Validation       | FluentValidation 11                      |
| Object Mapping   | AutoMapper 12                            |
| Real-time        | Server-Sent Events (SSE)                 |
| API Docs         | Swagger / Swashbuckle                    |
| Secrets          | DotNetEnv (file `.env`)                  |
| Containerization | Docker + Docker Compose                  |

---

## 3. Kiến Trúc Hệ Thống

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            CLIENT (Browser / App)                        │
│                       http://localhost:3000 (Frontend)                   │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ HTTP / SSE
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    HOST — ASP.NET Core (port 8080)                       │
│  ┌──────────────┐  ┌─────────────┐  ┌────────────────────────────────┐  │
│  │  Swagger UI  │  │  JWT Auth   │  │  ApiExceptionExtension (global)│  │
│  └──────────────┘  └─────────────┘  └────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    Rate Limiter (Token Bucket)                    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌────────┐ ┌────────┐ ┌─────────┐ ┌──────┐ ┌───────┐ ┌────────────┐  │
│  │  Auth  │ │  User  │ │ Product │ │ Cart │ │ Order │ │  Payment   │  │
│  │ Module │ │ Module │ │ Module  │ │Module│ │Module │ │   Module   │  │
│  └────────┘ └────────┘ └─────────┘ └──────┘ └───────┘ └────────────┘  │
│  ┌──────────────────────┐  ┌──────────────────────────────────────────┐ │
│  │     Chat Module      │  │         Notification Module (SSE)        │ │
│  └──────────────────────┘  └──────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    SharedKernel (BaseEntity, IRepository, etc.)  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
   ┌────────────┐    ┌───────────────┐   ┌──────────────┐
   │ PostgreSQL │    │    PostgreSQL  │   │    Redis     │
   │ (per-module│    │  (per-module) │   │   (Cache)    │
   │  DB x8)   │    │               │   │              │
   └────────────┘    └───────────────┘   └──────────────┘
```

---

## 4. Cấu Trúc Thư Mục

```
E-Verland/
├── Host/                        ← Entry point (startup, DI, pipeline)
│   ├── Program.cs
│   ├── appsettings.json
│   └── E-Verland.csproj         ← Compile toàn bộ module vào 1 assembly
│
├── Extension/                  ← Cross-cutting concerns
│   ├── Authorization.cs         ← Custom policies (AdminOnly, UserOnly)
│   ├── Exception.cs             ← ApiExceptionExtension
│   └── RateLimit.cs             ← Token-bucket rate limiting
│
├── Modules/
│   ├── Auth/
│   ├── User/
│   ├── Product/
│   ├── Cart/
│   ├── Order/
│   ├── Payment/
│   ├── Chat/
│   ├── Notification/
│   └── Redis/
│
├── SharedKernel/                ← Dùng chung (BaseEntity, IRepository, Pagination, Validators)
│
├── .env                         ← Secrets (không commit)
├── dockerfile
└── docker-compose.yaml
```

### Cấu trúc mỗi Module (4 lớp)

```
Modules/<Module>/
├── Api/
│   └── Controllers/             ← HTTP endpoints (nhận request, trả response)
├── Application/
│   ├── Commands/                ← Write operations (MediatR IRequest)
│   ├── Queries/                 ← Read operations (MediatR IRequest)
│   ├── DTOs/                    ← Request/Response DTOs + AutoMapper profiles
│   └── Contracts/               ← Interfaces (IRepository, IDbContext)
├── Domain/
│   └── Entities/, Enums/        ← Nghiệp vụ thuần, không phụ thuộc framework
└── Infrastructure/
    ├── Persistence/             ← DbContext, Module DI Extension
    └── Repository/              ← Triển khai IRepository với EF Core
```

---

## 5. Luồng Xử Lý Request (CQRS + MediatR)

```
┌────────────────┐    DTO     ┌──────────────────┐   IRequest   ┌──────────────────────┐
│   Controller   │ ─────────► │    MediatR Bus    │ ────────────► │  Command/QueryHandler │
│   (API Layer)  │            │  (mediator.Send)  │              │  (Application Layer)  │
└────────────────┘            └──────────────────┘              └──────────┬───────────┘
                                                                             │
                                                                    IRepository call
                                                                             │
                                                                             ▼
                                                                  ┌──────────────────────┐
                                                                  │    Repository Impl    │
                                                                  │  (Infrastructure)     │
                                                                  └──────────┬───────────┘
                                                                             │
                                                                        EF Core query
                                                                             │
                                                                             ▼
                                                                  ┌──────────────────────┐
                                                                  │     PostgreSQL DB     │
                                                                  └──────────────────────┘
```

**Ví dụ cụ thể (tạo sản phẩm):**

```
POST /api/product
   → ProductController.CreateProduct(dto)
   → _mediator.Send(new CreateProductCommand(dto))
   → CreateProductCommandHandler.Handle(...)
   → IProductRepository.CreateAsync(product)
   → ProductDbContext.SaveChangesAsync()
   → 201 Created + ProductResponseDto
```

---

## 6. Các Module & Chức Năng

### 6.1 Auth Module

- Đăng nhập bằng Email + Password (BCrypt hash)
- Đăng ký tài khoản có xác thực OTP qua Email (6 chữ số, hết hạn 10 phút, tối đa 3 lần thử)
- Reset mật khẩu qua OTP Email
- Đổi mật khẩu (đã đăng nhập)
- Phát hành JWT Access Token (10 phút) + Refresh Token (12 giờ, lưu trong Redis)
- Refresh Token xoay vòng (rotating): mỗi lần refresh thu hồi token cũ

### 6.2 User Module

- Quản lý tài khoản (Account): email, username, role, status
- Quản lý hồ sơ (Profile): tên, ảnh đại diện, ngày sinh, giới tính, bio
- Quản lý địa chỉ (Address): City / Province / District / Ward / Street, có thể đặt mặc định
- Quản lý tài khoản ngân hàng (BankAccount)

### 6.3 Product Module

- CRUD sản phẩm (chỉ Admin)
- Quản lý Brand và Category (phân cấp — cây danh mục không giới hạn cấp)
- Quản lý SKU (biến thể sản phẩm): giá, tồn kho, option values (Color/Size/...)
- Quản lý trạng thái sản phẩm: `Active | Inactive | OutOfStock | Pending`
- Tìm kiếm sản phẩm (public + admin filter)

### 6.4 Cart Module

- Thêm / cập nhật / xóa sản phẩm khỏi giỏ hàng
- Xóa toàn bộ giỏ hàng
- Xem giỏ hàng theo UserId

### 6.5 Order Module

- Tạo đơn hàng từ giỏ hàng (snapshot giá + thông tin người nhận)
- Vòng đời đơn hàng: `Pending → Confirmed → Shipping → Completed | Canceled`
- Admin: xem và cập nhật trạng thái đơn hàng
- Lọc đơn hàng theo nhiều tiêu chí (phân trang)

### 6.6 Payment Module

- Tạo và theo dõi bản ghi thanh toán
- Hỗ trợ phương thức: `OnlineBanking | COD`
- Trạng thái thanh toán: `Pending | Success | Failed | Refunded`
- Webhook handler nhận callback từ cổng thanh toán (`AllowAnonymous`)

### 6.7 Chat Module

- Nhắn tin 1-1 giữa User và Admin
- Xem danh sách cuộc hội thoại, preview tin nhắn cuối
- Gửi / xem tin nhắn trong cuộc hội thoại

### 6.8 Notification Module

- Gửi thông báo thời gian thực qua **Server-Sent Events (SSE)**
- Admin: gửi đến 1 user hoặc broadcast đến nhiều user
- Xem thông báo chưa đọc, đánh dấu đã đọc
- Quản lý danh sách kết nối SSE đang hoạt động

### 6.9 Redis Module

- Cache JWT Refresh Token (`JwtCacheService`)
- Cache dữ liệu sản phẩm (`ProductCacheService`)
- Cache giỏ hàng (`CartCacheService`)

---

## 7. Mô Hình Dữ Liệu (Domain Entities)

Tất cả entity kế thừa từ `BaseEntity`:

```csharp
public abstract class BaseEntity {
    public Guid Id { get; }           // Primary Key
    public DateTime CreatedAt { get; }
    public string? CreatedBy { get; }
    public DateTime? UpdatedAt { get; }
    public string? UpdatedBy { get; }
}
```

### Quan hệ giữa các module (theo ID — không có FK cross-module):

```
Account (UserDb)
  └── Profile
        ├── Address[]
        └── BankAccount[]

Product (ProductDb)
  ├── SKU[]
  ├── Brand  (BrandId FK trong ProductDb)
  └── Category (self-referencing)

Cart (CartDb)
  └── CartItem[]  (ProductId, SkuId — chỉ lưu Guid, không FK sang ProductDb)

Order (OrderDb)
  ├── OrderItem[]  (snapshot giá tại thời điểm đặt hàng)
  └── ReceiverSnapshot (value object — tên, SĐT, địa chỉ)

Payment (PaymentDb)
  └── (OrderId, UserId — chỉ lưu Guid)

Conversation (ChatDb)
  └── Message[]

Notification (NotificationDb)

EmailVerificationOtp (AuthDb)
```

---

## 8. Bảo Mật & Phân Quyền

### Luồng xác thực JWT

```
1. Client gửi POST /api/auth/login {email, password}
2. Server xác thực BCrypt hash
3. Server phát hành:
   - Access Token  (JWT, 10 phút, HMAC-SHA256)
   - Refresh Token (64-byte random, 12 giờ, lưu Redis)
4. Client gửi Access Token qua Header: Authorization: Bearer <token>
5. Khi hết hạn → gọi POST /api/auth/refresh để lấy token mới
   (Refresh Token xoay vòng — token cũ bị thu hồi ngay)
```

### JWT Claims

| Claim                    | Nội dung                    |
| ------------------------ | --------------------------- |
| `sub` / `NameIdentifier` | UserId (Guid)               |
| `email`                  | Email người dùng            |
| `name`                   | Username                    |
| `role`                   | `Admin` / `User` / `Seller` |
| `jti`                    | Token ID (unique)           |

### Phân quyền

| Role     | Quyền hạn                                                                             |
| -------- | ------------------------------------------------------------------------------------- |
| `Admin`  | Toàn quyền: CRUD sản phẩm, cập nhật đơn hàng, quản lý thanh toán, broadcast thông báo |
| `User`   | Mua hàng, quản lý giỏ hàng, đặt hàng, xem thông báo của mình                          |
| `Seller` | (Dự kiến mở rộng)                                                                     |

### Custom Authorization Policies

- `AdminOnly` → `RequireRole("Admin")`
- `UserOnly` → `RequireRole("User")`

---

## 9. Rate Limiting

**Thuật toán:** Token Bucket (`System.Threading.RateLimiting`)

**Keyed by:**

- Route có JWT → dùng claim `sub` (UserId)
- Route public / anonymous → dùng địa chỉ IP

Mỗi module Controller được annotate `[EnableRateLimiting("module-name")]` riêng biệt.

---

## 10. Thông Báo Thời Gian Thực (SSE)

```
Client A                        Server (NotificationService)
   │                                      │
   │  GET /api/notification/subscribe     │
   │ ─────────────────────────────────►  │
   │         (kết nối HTTP mở)            │
   │  ◄──── SSE stream (ping mỗi 30s) ── │
   │                                      │
   │            (Admin gửi thông báo)     │
   │  ◄──────── data: {notification} ─── │
   │                                      │
   │  POST /notification/{id}/mark-as-read│
   │ ─────────────────────────────────►  │
   │  ◄─────────── 200 OK ─────────────  │
```

`NotificationService` là **Singleton** duy trì `Dictionary<Guid, StreamWriter>` trong bộ nhớ — map UserId → kết nối SSE đang mở.

---

## 11. Infrastructure & Deployment

### Docker (multi-stage build)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
RUN dotnet restore && dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "E-Verland.dll"]
```

### Docker Compose

```yaml
services:
  e-verland:
    image: thanhtan1811/everland:latest
    ports:
      - "${APP_PORT:-8080}:8080"
    env_file: .env
    restart: unless-stopped
```

### Biến môi trường (.env)

| Biến                                        | Mô tả                                   |
| ------------------------------------------- | --------------------------------------- |
| `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`     | Cấu hình JWT                            |
| `AuthDb`, `UserDb`, `ProductDb`, ...        | Connection string PostgreSQL mỗi module |
| `Redis_URL`, `Redis_Port`, `Redis_Password` | Kết nối Redis                           |
| `Email__Smtp__*`                            | SMTP gửi email OTP                      |

---

## 12. API Endpoints Tóm Tắt

| Module       | Prefix                                                             | Ghi chú                                            |
| ------------ | ------------------------------------------------------------------ | -------------------------------------------------- |
| Auth         | `/api/auth`                                                        | Login, Register (OTP), Refresh, Đổi/Reset mật khẩu |
| User         | `/api/account`, `/api/profile`, `/api/address`, `/api/bankaccount` | CRUD tài khoản & hồ sơ                             |
| Product      | `/api/product`, `/api/sku`, `/api/brand`, `/api/category`          | Public đọc, Admin viết                             |
| Cart         | `/api/cart`                                                        | Thêm/xóa/xem giỏ hàng                              |
| Order        | `/api/order`                                                       | Đặt hàng, xem đơn, Admin quản lý                   |
| Payment      | `/api/payment`                                                     | Tạo thanh toán, webhook, Admin cập nhật            |
| Chat         | `/api/chat`                                                        | Cuộc hội thoại, tin nhắn                           |
| Notification | `/api/notification`                                                | SSE subscribe, gửi thông báo, đánh dấu đọc         |

---

## 13. Điểm Nổi Bật

- **Tách biệt dữ liệu hoàn toàn**: Mỗi module có PostgreSQL database riêng — không có cross-module foreign key, chỉ tham chiếu qua `Guid`.
- **CQRS rõ ràng**: Controller không gọi repository trực tiếp — tất cả đi qua MediatR pipeline.
- **Bảo mật nhiều lớp**: JWT strict (ClockSkew = 0) + Refresh Token rotating trong Redis + Rate Limiting per user/IP.
- **Real-time không cần WebSocket**: SSE đơn giản, lightweight, phù hợp cho luồng thông báo một chiều (server → client).
- **Triển khai đơn giản**: 1 Docker image duy nhất, cấu hình hoàn toàn qua `.env`.
