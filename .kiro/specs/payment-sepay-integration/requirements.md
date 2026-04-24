# Requirements Document

## Introduction

Tài liệu này mô tả các yêu cầu cho việc nâng cấp hệ thống thanh toán E-Verland (ASP.NET Core .NET 10), bao gồm:
- Chuẩn hóa tất cả API endpoint sang lowercase/kebab-case
- Refactor Payment,Chat, Notification module để loại bỏ code trùng lặp
- Tích hợp cổng thanh toán SePay (tạo payment link + xử lý webhook)
- Cơ chế lock/reserve stock sản phẩm trong quá trình thanh toán
- Sửa lỗi Google OAuth với redirect URI chính xác

Hệ thống sử dụng kiến trúc module-based với CQRS pattern (MediatR), PostgreSQL (Neon), Redis, và deploy trên Azure App Service.

## Glossary

- **System**: Toàn bộ ứng dụng E-Verland backend
- **Payment_Module**: Module xử lý thanh toán trong `Modules/Payment`
- **SePay_Client**: HTTP client gọi SePay API (`https://my.sepay.vn/userapi`)
- **SePay_Webhook_Handler**: Endpoint nhận callback từ SePay sau khi thanh toán
- **Stock_Reservation**: Cơ chế giảm available stock của SKU khi payment được tạo, trước khi payment hoàn tất
- **SKU**: Stock Keeping Unit — đơn vị tồn kho trong `Modules/Product/Domain/SKU.cs`
- **Payment_Code**: Mã định danh duy nhất của một payment, format `PAY-YYYYMMDD-XXXXXX`
- **OAuth_Google_Controller**: Controller xử lý Google OAuth flow tại `Modules/Auth/Api/Controllers/OAuthGoogleController.cs`
- **Route_Convention**: Cấu hình ASP.NET Core để tự động lowercase tất cả route

---

## Requirements

### Requirement 1: Lowercase API Endpoints

**User Story:** As a frontend developer, I want all API endpoints to use lowercase/kebab-case URLs, so that the API follows REST conventions and is consistent across all modules.

#### Acceptance Criteria

1. THE System SHALL configure `RouteOptions.LowercaseUrls = true` và `RouteOptions.LowercaseQueryStrings = true` trong `Program.cs`
2. WHEN a client sends a request to any API endpoint, THE System SHALL route the request correctly regardless of the case used in the URL
3. THE System SHALL replace all `[Route("api/[controller]")]` patterns so that controller names resolve to lowercase (e.g., `api/payment`, `api/product`, `api/cart`, `api/order`, `api/brand`, `api/category`, `api/sku`)
4. WHEN the `PaymentController` uses `[Route("api/[controller]")]`, THE System SHALL expose the base route as `api/payment` (not `api/Payment`)
5. THE System SHALL ensure all sub-routes within controllers use lowercase and kebab-case (e.g., `payment-order/{orderId}`, `payment-code/{code}`, `payment-user/{userId}`)

---

### Requirement 2: Refactor Payment, Chat, Notification Module

**User Story:** As a developer, I want the Payment module to have clean, non-duplicated code, so that it is easier to maintain and extend.

#### Acceptance Criteria

1. THE Payment_Module SHALL consolidate `CreatePaymentCommand` và `ProcessPaymentCommand` thành một command duy nhất `InitiatePaymentCommand` với đầy đủ tham số `OrderId`, `UserId`, `Amount`, `Method`
2. THE Payment_Module SHALL extract payment code generation logic vào một private static method hoặc helper class dùng chung
3. WHEN `InitiatePaymentCommand` is handled, THE Payment_Module SHALL check for existing payment by `OrderId` trước khi tạo mới
4. THE Payment_Module SHALL expose a `ISePayClient` interface trong `Application/Contracts` để tách biệt logic gọi SePay API khỏi command handler
5. THE Payment_Module SHALL register `ISePayClient` implementation trong `PaymentModule.cs` DI container
6. WHEN a `DbUpdateException` occurs during payment persistence, THE Payment_Module SHALL throw `InvalidOperationException` với message mô tả lỗi
7. dùng SignalR cho Chat, SSE cho Notification.
---

### Requirement 3: Tích hợp SePay

**User Story:** As a customer, I want to pay online via SePay, so that I can complete my purchase without cash on delivery.

#### Acceptance Criteria

1. WHEN a payment with `Method = OnlineBanking` is initiated, THE SePay_Client SHALL call the SePay API endpoint `POST https://my.sepay.vn/userapi/transactions/create` với `SEPAY_API` key trong header `Authorization: Apikey {SEPAY_API}`
2. WHEN the SePay API returns a payment URL, THE Payment_Module SHALL store the URL trong `Payment.PaymentUrl` field và trả về cho client
3. THE Payment_Module SHALL read `SEPAY_API` từ environment variable `SEPAY_API` và `SEPAY_KEY` từ environment variable `SEPAY_KEY`
4. WHEN a SePay webhook POST request arrives at `POST /api/payment/webhook/sepay`, THE SePay_Webhook_Handler SHALL verify the request signature bằng cách so sánh HMAC-SHA256 của request body với `SEPAY_KEY`
5. IF the webhook signature verification fails, THEN THE SePay_Webhook_Handler SHALL return HTTP 401 và log warning
6. WHEN the webhook signature is valid and `transaction_status = "success"`, THE SePay_Webhook_Handler SHALL update payment status to `Success` và release stock reservation
7. WHEN the webhook signature is valid and `transaction_status = "failed"`, THE SePay_Webhook_Handler SHALL update payment status to `Failed` và release stock reservation
8. THE SePay_Webhook_Handler SHALL return HTTP 200 với body `{"success": true}` sau khi xử lý thành công
9. IF the `Payment_Code` in the webhook payload does not match any existing payment, THEN THE SePay_Webhook_Handler SHALL return HTTP 404
10. FOR ALL valid SePay webhook callbacks, THE System SHALL ensure payment status is updated exactly once (idempotent — nếu payment đã ở trạng thái `Success`, webhook thứ hai không thay đổi gì)

---

### Requirement 4: Lock/Reserve Stock khi Thanh Toán

**User Story:** As a system administrator, I want products to be reserved during payment processing, so that customers cannot purchase out-of-stock items.

#### Acceptance Criteria

1. WHEN `InitiatePaymentCommand` is handled, THE Payment_Module SHALL call `IProductReservationService.ReserveStockAsync(skuId, quantity)` cho mỗi item trong order trước khi lưu payment
2. IF any SKU does not have sufficient stock during reservation, THEN THE Payment_Module SHALL throw `InvalidOperationException` với message `"Insufficient stock for SKU {skuId}"` và không tạo payment
3. THE `IProductReservationService` SHALL decrement `SKU.Stock` bằng quantity được reserve và persist thay đổi vào `ProductDb`
4. WHEN payment status changes to `Success`, THE Payment_Module SHALL call `IProductReservationService.ConfirmReservationAsync(paymentId)` để finalize stock deduction
5. WHEN payment status changes to `Failed` hoặc `Cancelled`, THE Payment_Module SHALL call `IProductReservationService.ReleaseReservationAsync(paymentId)` để hoàn trả stock
6. THE System SHALL store reservation records trong một `StockReservation` entity với fields: `Id`, `PaymentId`, `SkuId`, `Quantity`, `Status` (Reserved/Confirmed/Released), `CreatedAt`
7. WHILE a payment is in `Pending` status, THE System SHALL maintain the stock reservation để ngăn overselling
8. IF `ReserveStockAsync` is called twice for the same `PaymentId`, THEN THE `IProductReservationService` SHALL return the existing reservation without decrementing stock again (idempotent)
9. WHEN a payment expires (configurable timeout, default 15 phút) without reaching `Success`, THE System SHALL automatically call `ReleaseReservationAsync` để release stock

---

### Requirement 5: Sửa Lỗi Google OAuth

**User Story:** As a customer, I want to log in with my Google account, so that I can access the platform without creating a separate password.

#### Acceptance Criteria

1. THE OAuth_Google_Controller SHALL configure `RedirectUri` = `https://e-verland-czf8bbhqfyd3ecfb.southeastasia-01.azurewebsites.net/swagger/index.html` khi build `AuthenticationProperties`
2. WHEN `AddGoogleOAuth` is called, THE System SHALL read `Redirect_Uri` từ environment variable và set `options.CallbackPath` tương ứng
3. WHEN Google redirects back to the callback URL, THE OAuth_Google_Controller SHALL authenticate the user và return JWT token
4. IF Google OAuth credentials (`Client_Id`, `Client_Secret`) are missing from environment variables, THEN THE System SHALL throw `InvalidOperationException` với message mô tả thiếu credentials
5. WHEN a user successfully authenticates via Google, THE System SHALL create or retrieve the user account và return `LoginResponseDto` với valid JWT token
6. IF Google OAuth callback authentication fails, THEN THE OAuth_Google_Controller SHALL log the error và return HTTP 400 với message mô tả lỗi
