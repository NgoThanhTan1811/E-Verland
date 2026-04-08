# Kế hoạch triển khai: Payment SePay Integration

## Tổng quan

Triển khai theo thứ tự từ đơn giản đến phức tạp: lowercase endpoints → fix Google OAuth → refactor Payment module → tích hợp SePay → Stock Reservation → Chat SignalR → EF Migrations.

## Tasks

- [x] 1. Lowercase API Endpoints
  - [x] 1.1 Cấu hình RouteOptions trong `Host/Program.cs`
    - Thêm `builder.Services.Configure<RouteOptions>` với `LowercaseUrls = true` và `LowercaseQueryStrings = true` trước `builder.Build()`
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 2. Fix Google OAuth
  - [x] 2.1 Sửa `Extension/OAuthGoogle.cs`
    - Thêm `options.CallbackPath = "/api/auth/google/callback"` vào cấu hình `AddGoogle`
    - _Requirements: 5.2_
  - [x] 2.2 Sửa `Modules/Auth/Api/Controllers/OAuthGoogleController.cs`
    - Hardcode `RedirectUri` thành URL Azure App Service cố định thay vì dùng `Url.Action()`
    - _Requirements: 5.1_

- [x] 3. Refactor Payment Module
  - [x] 3.1 Tạo `Modules/Payment/Application/Helpers/PaymentCodeHelper.cs`
    - Tạo static class `PaymentCodeHelper` với method `Generate()` trả về string format `PAY-YYYYMMDD-XXXXXX`
    - _Requirements: 2.2_
  - [x] 3.2 Tạo `Modules/Payment/Application/Contracts/ISePayClient.cs`
    - Định nghĩa interface với method `CreatePaymentLinkAsync(paymentCode, amount, description, ct)`
    - _Requirements: 2.4_
  - [x] 3.3 Tạo `Modules/Payment/Application/Commands/InitiatePayment.cs`
    - Tạo `InitiatePaymentCommand` record với các field: `OrderId`, `UserId`, `Amount`, `Method`, `Items`
    - Tạo `InitiatePaymentResponseDto` record với: `Id`, `Code`, `Status`, `PaymentUrl`
    - Implement handler với idempotency check theo `OrderId`, gọi `IProductReservationService.ReserveStockAsync`, gọi `ISePayClient` nếu `Method = OnlineBanking`
    - _Requirements: 2.1, 2.3, 2.6, 3.2_
  - [x] 3.4 Xóa `Modules/Payment/Application/Commands/CreatePayment.cs` và `ProcessPayment.cs`
    - Xóa 2 file command cũ sau khi `InitiatePayment.cs` đã hoàn chỉnh
    - _Requirements: 2.1_
  - [x] 3.5 Thêm field `PaymentUrl` vào `Modules/Payment/Domain/Payment.cs`
    - Thêm property `public string? PaymentUrl { get; set; }` vào entity `Payment`
    - _Requirements: 3.2_
  - [x] 3.6 Cập nhật `Modules/Payment/Infrastructure/Persistence/PaymentDbContext.cs`
    - Thêm mapping cho column `PaymentUrl` (nullable text)
    - _Requirements: 3.2_
  - [x] 3.7 Cập nhật `Modules/Payment/Api/Controllers/PaymentController.cs`
    - Thay các lệnh gọi `CreatePaymentCommand`/`ProcessPaymentCommand` bằng `InitiatePaymentCommand`
    - Đảm bảo sub-routes dùng lowercase/kebab-case (ví dụ: `payment-order/{orderId}`)
    - _Requirements: 1.5, 2.1_

- [x] 4. Tích hợp SePay
  - [x] 4.1 Tạo `Modules/Payment/Infrastructure/Services/SePayClient.cs`
    - Implement `ISePayClient` gọi `POST https://my.sepay.vn/userapi/transactions/create`
    - Đọc `SEPAY_API` từ environment variable, set header `Authorization: Apikey {SEPAY_API}`
    - _Requirements: 3.1, 3.3_
  - [x] 4.2 Đăng ký `ISePayClient` trong `Modules/Payment/Infrastructure/Persistence/PaymentModule.cs`
    - Thêm `services.AddHttpClient<ISePayClient, SePayClient>()`
    - _Requirements: 2.5_
  - [x] 4.3 Thêm webhook endpoint vào `Modules/Payment/Api/Controllers/PaymentController.cs`
    - Tạo action `POST /api/payment/webhook/sepay`
    - Verify HMAC-SHA256 signature từ header `X-SePay-Signature` với `SEPAY_KEY`
    - Trả về 401 nếu signature không hợp lệ, 404 nếu không tìm thấy payment
    - Cập nhật trạng thái payment và gọi `ConfirmReservationAsync` hoặc `ReleaseReservationAsync` tương ứng
    - Đảm bảo idempotent: nếu payment đã `Success` thì trả về 200 ngay
    - _Requirements: 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10_

- [x] 5. Stock Reservation
  - [x] 5.1 Tạo `Modules/Product/Domain/StockReservation.cs`
    - Tạo entity `StockReservation` với fields: `Id`, `PaymentId`, `SkuId`, `Quantity`, `Status`, `CreatedAt`
    - Tạo enum `ReservationStatus` với values: `Reserved`, `Confirmed`, `Released`
    - _Requirements: 4.6_
  - [x] 5.2 Tạo `Modules/Product/Application/Contracts/IProductReservationService.cs`
    - Định nghĩa interface với 3 methods: `ReserveStockAsync`, `ConfirmReservationAsync`, `ReleaseReservationAsync`
    - _Requirements: 4.1, 4.4, 4.5_
  - [x] 5.3 Cập nhật `Modules/Product/Infrastructure/Persistence/ProductDbContext.cs`
    - Thêm `DbSet<StockReservation> StockReservations`
    - Cấu hình index trên `PaymentId` và `SkuId`
    - _Requirements: 4.6_
  - [x] 5.4 Tạo `Modules/Product/Infrastructure/Services/ProductReservationService.cs`
    - Implement `ReserveStockAsync`: kiểm tra stock đủ, trừ `SKU.Stock`, tạo `StockReservation` với `Status=Reserved`; idempotent theo `PaymentId`
    - Implement `ConfirmReservationAsync`: cập nhật `Status=Confirmed` cho tất cả reservation của `paymentId`
    - Implement `ReleaseReservationAsync`: hoàn trả stock và cập nhật `Status=Released`
    - Throw `InvalidOperationException("Insufficient stock for SKU {skuId}")` nếu stock không đủ
    - _Requirements: 4.2, 4.3, 4.4, 4.5, 4.7, 4.8_
  - [x] 5.5 Tạo `Modules/Product/Infrastructure/Services/StockReservationExpiryService.cs`
    - Implement `BackgroundService` chạy mỗi 5 phút
    - Tìm các `StockReservation` có `Status=Reserved` và `CreatedAt < UtcNow - 15 phút`
    - Gọi `ReleaseReservationAsync` cho từng `PaymentId` hết hạn
    - _Requirements: 4.9_
  - [x] 5.6 Đăng ký services trong `Modules/Product/Infrastructure/Persistence/ProductModule.cs`
    - Thêm `services.AddScoped<IProductReservationService, ProductReservationService>()`
    - Thêm `services.AddHostedService<StockReservationExpiryService>()`
    - _Requirements: 4.1_

- [x] 6. Checkpoint — Đảm bảo build thành công
  - Đảm bảo project build không lỗi, kiểm tra tất cả dependency injection đã đăng ký đúng. Hỏi người dùng nếu có vấn đề phát sinh.

- [x] 7. Chat SignalR
  - [x] 7.1 Tạo `Modules/Chat/Api/Hubs/ChatHub.cs`
    - Tạo class `ChatHub : Hub` với attribute `[Authorize]`
    - Implement method `JoinConversation(string conversationId)` để join group
    - Implement method `SendMessage(string conversationId, string content)` để broadcast tới group
    - _Requirements: 2.7_
  - [x] 7.2 Đăng ký SignalR trong `Modules/Chat/Infrastructure/Persistence/ChatModule.cs`
    - Thêm `services.AddSignalR()`
    - _Requirements: 2.7_
  - [x] 7.3 Map hub trong `Host/Program.cs`
    - Thêm `app.MapHub<ChatHub>("/hubs/chat")` sau `app.MapControllers()`
    - _Requirements: 2.7_

- [x] 8. EF Migrations
  - [x] 8.1 Tạo migration `AddPaymentUrl` cho `PaymentDbContext`
    - Chạy: `dotnet ef migrations add AddPaymentUrl --context PaymentDbContext --project Host --output-dir Migrations/Payment`
    - Kiểm tra migration file được tạo đúng với column `PaymentUrl TEXT NULL`
    - _Requirements: 3.2_
  - [x] 8.2 Tạo migration `AddStockReservation` cho `ProductDbContext`
    - Chạy: `dotnet ef migrations add AddStockReservation --context ProductDbContext --project Host --output-dir Migrations/Product`
    - Kiểm tra migration file tạo bảng `StockReservations` với đầy đủ columns và indexes
    - _Requirements: 4.6_

- [x] 9. Checkpoint cuối — Đảm bảo tất cả tests pass
  - Đảm bảo project build thành công, tất cả DI registrations hợp lệ, migrations đã được apply. Hỏi người dùng nếu có vấn đề phát sinh.

## Ghi chú

- Tasks đánh dấu `*` là optional, có thể bỏ qua để triển khai MVP nhanh hơn
- Mỗi task tham chiếu đến requirements cụ thể để đảm bảo traceability
- Thứ tự task được thiết kế để tránh dependency chưa được tạo
- Biến môi trường cần thiết: `SEPAY_API`, `SEPAY_KEY`
