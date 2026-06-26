# TechStore Customer Web

**TechStore Customer Web** là giao diện cửa hàng trực tuyến dành cho khách hàng, được xây dựng trên nền tảng **ASP.NET Core 10 MVC** kết hợp với **Tailwind CSS v4** và thư viện slide **Swiper**. Dự án tích hợp các công nghệ hiện đại nhất:

- **Authentication:** Google Firebase (Google, Facebook, SMS OTP, Email/Pass, Magic Link).
- **Payments:** VNPay, MoMo, SePay (Chuyển khoản ngân hàng tự động).
- **Storage:** Cloudinary.

---

## Yêu cầu hệ thống

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt các công cụ sau:

- **.NET 10 SDK** (hoặc mới hơn)
- **Node.js 20** (hoặc mới hơn) & **npm 10**
- **SQL Server** (Nếu muốn chạy với dữ liệu thật)

---

## Hướng dẫn Cài đặt & Khởi chạy (Getting Started)

### Bước 1: Clone dự án và cài đặt thư viện

Mở terminal tại thư mục gốc và chạy lệnh:

```powershell
# Restore các package backend (C#)
dotnet restore

# Cài đặt các thư viện frontend (Node.js)
npm install
```

### Bước 2: Khôi phục Cơ sở dữ liệu (Tùy chọn nếu dùng Mock Data)

Nếu bạn muốn sử dụng Database SQL Server thật, bạn cần chạy các script SQL để tạo bảng và dữ liệu mẫu. Hoặc nếu có file `.bak`, hãy Restore vào SQL Server của bạn.
*Lưu ý: Dự án có kèm các script cập nhật DB trong thư mục `Data/` (ví dụ: `20260623_AddSePayWebhookEvents.sql`). Hãy nhớ chạy chúng.*

### Bước 3: Cấu hình hệ thống (`appsettings.json`)

Copy file cấu hình mẫu để tạo file cấu hình thực tế (Không bao giờ commit file này lên Git):

```powershell
Copy-Item appsettings.example.json appsettings.json
```

Mở `appsettings.json` và cập nhật các thông số (Xem chi tiết ở các phần dưới).

### Bước 4: Khởi chạy dự án

```powershell
dotnet run
```

**Hậu trường tự động của lệnh `dotnet run`:**

1. **Auto NPM Install (`npm ci`):** Tự động khôi phục các thư viện Node.js nếu chưa có hoặc lỗi thời.
2. **MSBuild Copy Assets:** Tự động copy tài nguyên CSS/JS của `Swiper` từ `node_modules` vào thư mục `wwwroot/lib/swiper/`.
3. **Compile Tailwind CSS:** Biên dịch tệp `tailwind.css` để hiển thị đúng giao diện.

---

## Cấu hình Authentication (Firebase)

Hệ thống Đăng nhập/Đăng ký sử dụng 100% Firebase Authentication (Client SDK & Admin SDK).

**1. Cấu hình Frontend & Backend (Chỉ cần làm 1 lần ở appsettings.json):**

- Truy cập Firebase Console -> Project Settings -> General -> Your apps.
- Nhập thông tin cấu hình `firebaseConfig` của bạn vào file `appsettings.json` tại mục `"Firebase"`.
- *Lưu ý: Bạn không cần chạm vào file `firebase-init.js`, vì hệ thống Backend C# sẽ tự động bơm cấu hình từ `appsettings.json` sang cho Frontend Javascript.*

**2. Khóa bảo mật (Service Account cho Admin SDK):**

- Truy cập Firebase Console -> Project Settings -> Service accounts.
- Bấm "Generate new private key".
- Đổi tên file tải về thành `firebase-admin-key.json` và đặt vào thư mục gốc của dự án (Ngang hàng với file `Program.cs`).
- *Lưu ý: File `firebase-admin-key.json` đã được đưa vào `.gitignore` để tránh lộ khóa bảo mật.*

---

## Cấu hình Thanh toán (Payments)

Tất cả cấu hình thanh toán nằm trong file `appsettings.json`.

### 1. Ví điện tử MoMo

```json
"MoMo": {
  "PartnerCode": "YOUR_MOMO_PARTNER_CODE",
  "AccessKey": "YOUR_ACCESS_KEY",
  "SecretKey": "YOUR_SECRET_KEY",
  "BaseUrl": "https://test-payment.momo.vn",
  "RequestType": "payWithMethod"
}
```

### 2. Cổng thanh toán VNPay

Thêm cấu hình VNPay vào `appsettings.json` của bạn (nếu có):

```json
"VNPay": {
  "TmnCode": "YOUR_VNPAY_TMN_CODE",
  "HashSecret": "YOUR_VNPAY_HASH_SECRET",
  "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
}
```

### 3. Chuyển khoản ngân hàng tự động (SePay)

SePay được dùng để nhận webhook khi khách hàng chuyển khoản ngân hàng thành công.

```json
"SePayWebhook": {
  "IsTestMode": true,
  "AuthenticationMode": "HmacSha256",
  "ApiKey": "",
  "SecretKey": "YOUR_SEPAY_WEBHOOK_SECRET",
  "AllowedClockSkewSeconds": 300
},
"SePayPayment": {
  "BankCode": "Vietcombank",
  "BankName": "Vietcombank",
  "AccountNumber": "YOUR_ACCOUNT_NUMBER",
  "AccountName": "YOUR_ACCOUNT_NAME",
  "PaymentTimeoutMinutes": 15,
  "IsTestMode": true
}
```

*Lưu ý: Đảm bảo cấu hình Webhook URL trên Dashboard của SePay trỏ về: `https://your-domain.com/api/webhooks/sepay`.*

---

## Chế độ Mock Data vs Real Database

Dự án hỗ trợ cơ chế chuyển đổi linh hoạt giữa dữ liệu giả lập (Mock) để test UI nhanh và dữ liệu từ SQL Server thật:

```json
"DatabaseSettings": {
  "UseMockData": true
}
```

- **`"UseMockData": true` (Mặc định khi clone):** Hệ thống không kết nối SQL Server. Tự động trả về dữ liệu mẫu (MockAccountService, MockHomeService...) giúp bạn chạy web lên ngay lập tức.
- **`"UseMockData": false`:** Bật chế độ Real DB. Đảm bảo bạn đã cấu hình chuỗi kết nối SQL Server tại `"ConnectionStrings": { "DefaultConnection": "..." }`.

---

## Cấu hình Google Maps (Checkout)

Khi cần dùng bản đồ để chọn địa chỉ nhận hàng:

1. Copy file mẫu: `Copy-Item appsettings.GoogleMaps.example.json appsettings.GoogleMaps.json`
2. Điền API Key của Google (Yêu cầu bật `Maps JavaScript API` và `Geocoding API`).

---

## Hướng dẫn phát triển (Development)

Trong quá trình chỉnh sửa mã nguồn, để ứng dụng tự động tải lại giao diện khi có thay đổi:

1. **Chạy ứng dụng .NET ở chế độ theo dõi (Watch Mode):**

```powershell
dotnet watch run
```

1. **Biên dịch tự động Tailwind CSS (Nếu chỉnh sửa class HTML):**

Mở một terminal thứ 2 và chạy:

```powershell
npm run css:watch
```
