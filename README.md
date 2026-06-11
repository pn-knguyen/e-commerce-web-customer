# 💻 TechStore Customer Web

**TechStore Customer Web** là giao diện cửa hàng trực tuyến dành cho khách hàng, được xây dựng trên nền tảng **ASP.NET Core 10 MVC** kết hợp với **Tailwind CSS v4** và thư viện slide **Swiper**.

---

## 🛠️ Yêu cầu hệ thống

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt các công cụ sau:
- **.NET 10 SDK** (hoặc mới hơn)
- **Node.js 20** (hoặc mới hơn)
- **npm 10** (hoặc mới hơn)

---

## 🚀 Hướng dẫn khởi chạy nhanh (Sau khi Clone)

Dự án được tích hợp các cơ chế tự động hóa hoàn toàn. Sau khi clone dự án về, bạn chỉ cần mở terminal tại thư mục gốc và chạy lệnh duy nhất:

```powershell
dotnet run
```

### Hậu trường tự động của lệnh `dotnet run`:
1. **Restore NuGet:** Tải về các thư viện C# cần thiết.
2. **Auto NPM Install (`npm ci`):** Tự động khôi phục các thư viện Node.js nếu chưa có hoặc lỗi thời.
3. **MSBuild Copy Assets:** Tự động copy tài nguyên CSS/JS của `Swiper` từ `node_modules` vào thư mục `wwwroot/lib/swiper/`.
4. **Compile Tailwind CSS:** Biên dịch tệp `tailwind.css` để hiển thị đúng giao diện.
5. **Chạy Mock Data:** Ứng dụng tự động chạy bằng dữ liệu giả để hiển thị giao diện ngay lập tức mà không yêu cầu cài đặt cơ sở dữ liệu.

---

## ⚙️ Cấu hình chế độ dữ liệu (Mock & Database)

Dự án hỗ trợ cơ chế chuyển đổi linh hoạt giữa dữ liệu giả lập (Mock) để phát triển Frontend nhanh và dữ liệu từ Database SQL Server thật thông qua cấu hình trong file `appsettings.json`:

```json
"DatabaseSettings": {
  "UseMockData": true
}
```

*   **`"UseMockData": true` (Mặc định khi clone):** Sử dụng các file tĩnh trong thư mục `Application/Home/Mock/` để nạp dữ liệu lên giao diện. Không yêu cầu cài đặt SQL Server.
*   **`"UseMockData": false`:** Hệ thống chuyển sang sử dụng `DbHomePageViewModelFactory.cs`. Bạn cần cấu hình chuỗi kết nối SQL Server tại `"ConnectionStrings": { "DefaultConnection": "..." }` để nạp dữ liệu thật từ cơ sở dữ liệu.

---

## 👨‍💻 Hướng dẫn phát triển (Development)

Trong quá trình chỉnh sửa mã nguồn, để ứng dụng tự động tải lại giao diện khi có thay đổi:

1. **Terminal 1 - Chạy ứng dụng .NET ở chế độ theo dõi (Watch Mode):**
   ```powershell
   dotnet watch run
   ```

2. **Terminal 2 - Biên dịch tự động Tailwind CSS (khi chỉnh sửa class HTML):**
   ```powershell
   npm run css:watch
   ```

---

## 📂 Các thư viện Database đã tích hợp sẵn:
Dự án đã được cài đặt sẵn bộ công cụ **Entity Framework Core 10.0.9** kết nối SQL Server bao gồm:
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools`
