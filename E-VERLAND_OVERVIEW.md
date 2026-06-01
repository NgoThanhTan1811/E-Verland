# E-Verland Overview

E-Verland là một hệ thống backend thương mại điện tử xây dựng theo hướng **Modular Monolith** trên ASP.NET Core .NET 10. Hệ thống được tổ chức theo module nghiệp vụ, mỗi module giữ trách nhiệm riêng về domain, application, persistence và hạ tầng liên quan, trong khi host đóng vai trò điểm khởi động duy nhất.

Tài liệu này mô tả trạng thái tổng quan hiện tại của hệ thống: cấu trúc repo, các module chính, hạ tầng xung quanh, điểm giao cắt kỹ thuật, và các đặc tính vận hành quan trọng.

---

## Tech Stack

| Category      | Technology                                    |
| ------------- | --------------------------------------------- |
| Framework     | ASP.NET Core (.NET 10)                        |
| Architecture  | Modular Monolith, CQRS                        |
| ORM           | Entity Framework Core                         |
| Relational DB | PostgreSQL                                    |
| Cache         | Redis                                         |
| Search        | Meilisearch                                   |
| Auth          | JWT qua HttpOnly Cookie, OAuth Google, BCrypt |
| Validation    | FluentValidation                              |
| Mapping       | AutoMapper                                    |
| Real-time     | Server-Sent Events (SSE)                      |
| API Docs      | Swagger / Swashbuckle                         |
| Observability | Logging, Trace ID, exception middleware       |
| Infra         | AWS, Terraform, Docker                        |

---

## System Scope

E-Verland hiện bao phủ các mảng chức năng chính của một nền tảng e-commerce:

- quản lý tài khoản, xác thực, phân quyền
- quản lý hồ sơ người dùng và địa chỉ
- danh mục, sản phẩm, biến thể, thương hiệu và thuộc tính
- giỏ hàng và vòng đời đơn hàng
- thanh toán và ghi nhận trạng thái giao dịch
- chat hỗ trợ người dùng và quản trị viên
- thông báo thời gian thực qua SSE
- lưu trữ media và tài nguyên sản phẩm
- lớp dashboard phục vụ tổng hợp dữ liệu vận hành

---

## Repository Structure

```
E-Verland/
+-- Host/                        # ASP.NET Core startup project
+-- Extensions/                  # Cross-cutting concerns and middleware
+-- Infra/                       # AWS, Docker, Terraform, third-party integrations
+-- Modules/                     # Feature modules
|   +-- Auth/
|   +-- User/
|   +-- Product/
|   +-- Cart/
|   +-- Order/
|   +-- Payment/
|   +-- Chat/
|   +-- Notification/
|   +-- Redis/
|   +-- Dashboard/
|   +-- Media/
+-- SharedKernel/                # Shared abstractions, base entities, validators, pagination
+-- Tests/                       # Test projects and system flow coverage
```

### Host

- `Host/` là entrypoint duy nhất của ứng dụng.
- `Program.cs` chịu trách nhiệm ghép nối các module, middleware và cấu hình chạy.
- `appsettings.json`, môi trường, migration và các cấu hình runtime được gắn với host.

### Extensions

`Extensions/` chứa các thành phần cắt ngang toàn hệ thống, bao gồm:

- Swagger setup
- authentication
- authorization
- CORS
- exception handling
- logging
- Google OAuth integration
- rate limiting
- trace id middleware

### Infra

`Infra/` là lớp hạ tầng độc lập, chia theo miền chức năng kỹ thuật:

- `aws/` cho AWS service integration và cấu hình vận hành
- `docker/` cho container build, compose và pipeline liên quan
- `terraform/` cho khai báo hạ tầng
- `third-party/` cho tích hợp bên ngoài
- các nhóm phụ trợ như `Configuration/`, `Messaging/`, `Observability/`, `Resilience/`, `Search/`, `Storage/`

### Modules

`Modules/` là trung tâm của kiến trúc module hóa, mỗi module có boundary riêng và không phụ thuộc chéo trực tiếp vào entity của module khác.

### SharedKernel

`SharedKernel/` chứa các thành phần dùng chung như:

- base entity và interface chung
- pagination
- validators
- persistence abstractions
- domain events và context helpers
- locations / shared value objects

### Tests

`Tests/` gom các bộ kiểm thử theo luồng nghiệp vụ và theo miền chức năng, thay vì dàn trải trong từng module.

---

## Module Map

| Module           | Responsibility                                                              | Own DB           |
| ---------------- | --------------------------------------------------------------------------- | ---------------- |
| **Auth**         | Đăng nhập, JWT, refresh token, Google OAuth, xác thực email, reset mật khẩu | `AuthDb`         |
| **User**         | Tài khoản, hồ sơ, địa chỉ, tài khoản ngân hàng                              | `UserDb`         |
| **Product**      | Catalog, SKU variants, thương hiệu, danh mục, thuộc tính, dữ liệu tìm kiếm  | `ProductDb`      |
| **Cart**         | Giỏ hàng và item trong giỏ                                                  | `CartDb`         |
| **Order**        | Vòng đời đơn hàng, snapshot người nhận, trạng thái fulfillment              | `OrderDb`        |
| **Payment**      | Ghi nhận giao dịch và trạng thái thanh toán                                 | `PaymentDb`      |
| **Chat**         | Hội thoại hỗ trợ giữa user và admin                                         | `ChatDb`         |
| **Notification** | Lưu notification và đẩy real-time qua SSE                                   | `NotificationDb` |
| **Media**        | Upload media, ánh xạ asset, tích hợp object storage                         | `MediaDb`        |
| **Dashboard**    | Dữ liệu tổng hợp cho màn hình quản trị                                      | `DashboardDb`    |
| **Redis**        | Cache, token state, dữ liệu tạm                                             | -                |

---

## Architecture

### Boundary model

- Mỗi module giữ domain riêng và persistence riêng.
- Quan hệ giữa các module được truyền qua ID, snapshot, hoặc hợp đồng dữ liệu thay vì navigation EF chéo module.
- Tránh shared DbContext cho toàn hệ thống.

### CQRS flow

- Write side xử lý command, validation và business rules.
- Read side tập trung vào query, projection và tối ưu truy vấn.
- Mỗi module có thể đăng ký assembly riêng cho MediatR hoặc pipeline liên quan.

### Cross-cutting concerns

- Authentication và authorization được xử lý thống nhất ở tầng chung.
- Rate limiting được áp theo module và ngữ cảnh request.
- Exception handling, logging và trace id được gom trong extension layer để đảm bảo nhất quán.

### Real-time model

- Notification dùng SSE để đẩy dữ liệu thời gian thực.
- Luồng real-time ưu tiên đơn giản hóa vận hành thay vì phụ thuộc vào stack websocket phức tạp.

### Search model

- Tìm kiếm được tách khỏi truy vấn nghiệp vụ thuần PostgreSQL.
- Meilisearch phục vụ truy vấn nhanh, lọc và ranking cho catalog.

### Cache model

- Redis phục vụ cache, token state và dữ liệu tạm.
- Cache được xem là lớp hỗ trợ, không thay thế nguồn dữ liệu chính.

---

## Domain Overview

### Users & Auth

- Ba vai trò chính: **Admin**, **User**, **Seller**.
- Hỗ trợ JWT access token và refresh token.
- Token được lưu trong HttpOnly cookies gồm `access_token` và `refresh_token`.
- Luồng xác thực đọc token từ cookie thay vì phụ thuộc vào header Authorization.
- Tích hợp Google OAuth như một luồng xác thực bổ sung.
- Email verification và password reset được xử lý theo luồng xác thực riêng.
- Hồ sơ người dùng bao gồm địa chỉ và tài khoản ngân hàng.

### Products

- Sản phẩm có base price, virtual price, ảnh, trạng thái và thuộc tính động.
- SKU được mô hình hóa theo option values để biểu diễn biến thể.
- Danh mục hỗ trợ cấu trúc cha-con nhiều cấp.
- Dữ liệu tìm kiếm và dữ liệu nghiệp vụ được tách theo mục đích sử dụng.

### Cart, Orders & Payments

- Cart phản ánh trạng thái mua hàng hiện tại của từng người dùng.
- Order lưu snapshot thông tin người nhận tại thời điểm tạo đơn.
- Payment gắn với order và lưu trạng thái xử lý giao dịch.
- Luồng đơn hàng bám theo các trạng thái nghiệp vụ đã định nghĩa trong module Order.

### Chat & Notifications

- Chat là luồng 1-1 giữa user và admin.
- Notification được lưu bền vững và đồng thời có thể được đẩy real-time.
- Message và notification là hai miền dữ liệu riêng nhưng được phối hợp trong trải nghiệm hỗ trợ người dùng.

### Media & Dashboard

- Media module xử lý tài nguyên file và tham chiếu asset.
- Dashboard module gom dữ liệu tổng hợp cho mục tiêu quản trị và giám sát.

---

## Infrastructure

### Host runtime

- Host là điểm chạy cuối cùng của hệ thống.
- Tất cả module được nạp vào cùng một ứng dụng nhưng vẫn giữ trách nhiệm nội bộ riêng.

### AWS and deployment

- Hạ tầng cloud được mô tả rõ trong `Infra/aws` và `Infra/terraform`.
- Cấu trúc hiện tại cho thấy hệ thống đã đi theo hướng deployment tách lớp, thay vì trộn logic hạ tầng vào code nghiệp vụ.

### Storage and assets

- Lưu trữ media được thiết kế độc lập với domain nghiệp vụ.
- Nguồn lưu trữ có thể là object storage hoặc các dịch vụ tương thích theo cấu hình hạ tầng.

### Search and messaging

- Search service được tách riêng để phù hợp với catalog và truy vấn sản phẩm.
- Các nhóm phụ trợ như messaging và resilience được tổ chức trong `Infra/` thay vì rải trong module domain.

### Docker and CI

- `Infra/docker` giữ vai trò liên quan đến containerization và triển khai tự động.
- Repo hiện tại đã có cấu trúc phù hợp cho môi trường build, test và deploy có tính lặp lại.

---

## Security and Access Control

- JWT là cơ chế xác thực chính của hệ thống nhưng được truyền qua cookie thay vì header Authorization.
- Google OAuth là luồng đăng nhập bổ sung.
- Authorization được tách khỏi authentication để dễ cấu hình policy theo module hoặc role.
- Rate limiting được áp theo module nhằm kiểm soát tải và lạm dụng.

---

## Rate Limiting

Token-bucket rate limits được áp dụng theo module, keyed theo JWT hoặc IP.

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

Hệ thống đọc cấu hình từ `appsettings.json` và biến môi trường `.env`.

### Nhóm cấu hình chính

| Group         | Keys / Purpose                          |
| ------------- | --------------------------------------- |
| App           | Backend URL, Frontend URL               |
| Auth          | JWT key, issuer, audience, Google OAuth |
| Database      | Connection string cho từng module       |
| Redis         | Connection string cache                 |
| Search        | Search endpoint và index config         |
| Email         | SMTP host, port, username, password     |
| Storage       | Media storage / object storage config   |
| Observability | Logging, trace, middleware config       |

### Runtime config shape

- cấu hình ứng dụng và URL nền tảng
- cấu hình từng database theo module
- cấu hình cache và token-related state
- cấu hình search service
- cấu hình email và xác thực ngoài
- cấu hình storage cho media và asset
- cấu hình vận hành cho logging và trace

---

## Operational Characteristics

- Hệ thống được thiết kế để dễ mở rộng thêm module mà không phá boundary hiện tại.
- Các mối phụ thuộc kỹ thuật được gom về `Extensions/` và `Infra/` để giảm nhiễu trong domain code.
- Cấu trúc hiện tại phù hợp cho việc theo dõi, kiểm thử và triển khai theo từng lớp thay vì theo từng file rời rạc.
- Tài liệu này đóng vai trò mô tả tổng quan hiện trạng, không bao gồm ví dụ code hay hướng dẫn chạy chi tiết.

---

**Document Version:** Overview  
**Last Updated:** May 2026
