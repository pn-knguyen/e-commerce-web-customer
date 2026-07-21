using e_commerce_web_customer.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_customer.Controllers;

public sealed class StaticInfoController : Controller
{
    private static readonly IReadOnlyDictionary<string, StaticInfoPageViewModel> Pages =
        new Dictionary<string, StaticInfoPageViewModel>(StringComparer.OrdinalIgnoreCase)
        {
            ["stores"] = Create("stores", "He thong cua hang", "TechStore gan ban", "Tra cuu khu vuc phuc vu, thoi gian mo cua va kenh lien he cua he thong TechStore.", ["Ho tro tu van tai cua hang", "Dat hang online va nhan tai diem ban", "Cap nhat danh sach cua hang theo khu vuc"], "Ve trang chu", "/"),
            ["orders/track"] = Create("orders/track", "Tra cuu don hang", "Theo doi don hang", "Tinh nang tra cuu don hang dang duoc ket noi voi khu vuc tai khoan va lich su mua hang.", ["Dang nhap de xem lich su mua hang", "Kiem tra trang thai giao hang trong tai khoan", "Lien he ho tro neu can doi soat don"], "Den tai khoan", "/Account/Profile?tab=orders"),
            ["trade-in"] = Create("trade-in", "Thu cu doi moi", "Len doi tiet kiem", "Chuong trinh thu cu doi moi dang duoc TechStore cap nhat theo tung dong san pham va thuong hieu.", ["Danh gia tinh trang thiet bi", "Tu van san pham len doi phu hop", "Ap dung uu dai theo tung thoi diem"], "Xem san pham", "/catalog"),
            ["deals"] = Create("deals", "Khuyen mai", "Uu dai TechStore", "Cac chuong trinh uu dai noi bat duoc tong hop de ban de dang tim san pham phu hop.", ["Uu dai theo danh muc", "Gia tot trong ngay", "San pham dang duoc quan tam"], "Xem catalog", "/catalog"),
            ["deals/education"] = Create("deals/education", "Uu dai giao duc", "Education deals", "Uu dai danh cho hoc tap va lam viec dang duoc cap nhat theo chuong trinh hien hanh.", ["Laptop cho hoc tap", "May tinh bang va phu kien", "Ho tro tra gop linh hoat"], "Xem laptop", "/catalog?cat=laptop"),
            ["deals/student"] = Create("deals/student", "Uu dai sinh vien", "Student deals", "TechStore dang hoan thien khu vuc uu dai rieng cho sinh vien va giao vien.", ["Thiet bi hoc tap", "Phu kien can thiet", "Chuong trinh gia tot theo mua"], "Xem laptop", "/catalog?cat=laptop"),
            ["business/register"] = Create("business/register", "Dang ky S-Business", "Khach hang doanh nghiep", "Khu vuc S-Business ho tro doanh nghiep dang duoc TechStore hoan thien.", ["Tu van mua so luong", "Ho tro xuat hoa don", "Chinh sach cham soc rieng"], "Lien he", "/contact"),
            ["business/offers"] = Create("business/offers", "Uu dai S-Business", "Doanh nghiep", "Cac uu dai S-Business se duoc cap nhat theo nhu cau mua sam va chinh sach doanh nghiep.", ["Giai phap thiet bi van phong", "Bao hanh va ho tro sau ban", "Uu dai theo so luong"], "Xem catalog", "/catalog"),
            ["faq"] = Create("faq", "Cau hoi thuong gap", "Ho tro khach hang", "Nhung cau hoi ve dat hang, giao nhan, thanh toan va bao hanh se duoc TechStore cap nhat tai day.", ["Huong dan mua hang", "Thong tin thanh toan", "Chinh sach sau ban"], "Ve trang chu", "/"),
            ["return-policy"] = Create("return-policy", "Chinh sach doi tra", "Ho tro sau ban", "Thong tin doi tra duoc ap dung theo tinh trang san pham va quy dinh bao hanh hien hanh.", ["Kiem tra dieu kien doi tra", "Giu hoa don va phu kien", "Lien he ho tro truoc khi gui san pham"], "Lien he ho tro", "/contact"),
            ["warranty"] = Create("warranty", "Chinh sach bao hanh", "Bao hanh chinh hang", "TechStore ho tro thong tin bao hanh theo nha san xuat va tinh trang don hang.", ["Kiem tra san pham chinh hang", "Ho tro tiep nhan bao hanh", "Theo doi lich su mua hang trong tai khoan"], "Den tai khoan", "/Account/Profile?tab=orders"),
            ["shipping"] = Create("shipping", "Chinh sach giao hang", "Giao nhanh", "Thong tin giao hang duoc ap dung theo khu vuc, kho hang va phuong thuc thanh toan.", ["Giao nhanh tai khu vuc ho tro", "Theo doi trang thai don hang", "Nhan tai cua hang khi kha dung"], "Xem gio hang", "/Cart"),
            ["installment"] = Create("installment", "Tra gop 0%", "Thanh toan linh hoat", "Chuong trinh tra gop duoc ap dung theo san pham, ngan hang va doi tac tai chinh.", ["Tham khao gia san pham", "Chon phuong thuc thanh toan khi dat hang", "Kiem tra dieu kien voi doi tac"], "Xem san pham", "/catalog"),
            ["about"] = Create("about", "Ve TechStore", "Cong nghe chinh hang", "TechStore tap trung vao trai nghiem mua sam cong nghe ro rang, nhanh gon va dang tin cay.", ["San pham chinh hang", "Gia minh bach", "Ho tro khach hang tan tam"], "Xem trang chu", "/"),
            ["careers"] = Create("careers", "Tuyen dung", "Gia nhap TechStore", "Thong tin tuyen dung se duoc cap nhat theo nhu cau van hanh va phat trien cua TechStore.", ["Moi truong ban le cong nghe", "Doi ngu tu van san pham", "Co hoi hoc hoi va phat trien"], "Lien he", "/contact"),
            ["news"] = Create("news", "Tin tuc cong nghe", "Cap nhat moi", "Khu vuc tin tuc dang duoc hoan thien de tong hop thong tin san pham va xu huong cong nghe.", ["Tin san pham moi", "Meo chon mua", "Huong dan su dung"], "Xem san pham moi", "/catalog?isNew=true"),
            ["contact"] = Create("contact", "Lien he", "Ho tro TechStore", "TechStore san sang tiep nhan yeu cau ho tro qua hotline, email va cac kenh cham soc khach hang.", ["Hotline 1900 1234", "Email hotro@techstore.vn", "Thoi gian ho tro 8:00 - 22:00"], "Ve trang chu", "/"),
            ["privacy"] = Create("privacy", "Chinh sach bao mat", "Bao ve thong tin", "TechStore ton trong va bao ve thong tin ca nhan trong qua trinh tu van, dat hang va cham soc sau ban.", ["Chi su dung thong tin cho muc dich phuc vu don hang", "Bao ve thong tin tai khoan", "Ho tro cap nhat thong tin khi can"], "Ve trang chu", "/"),
            ["terms"] = Create("terms", "Dieu khoan su dung", "Quy dinh chung", "Dieu khoan su dung giup thong nhat cach truy cap, dat hang va su dung dich vu tren TechStore.", ["Su dung thong tin chinh xac khi dat hang", "Tuan thu chinh sach thanh toan", "Lien he ho tro khi can giai thich them"], "Ve trang chu", "/")
        };

    [HttpGet("/stores")]
    [HttpGet("/orders/track")]
    [HttpGet("/trade-in")]
    [HttpGet("/deals")]
    [HttpGet("/deals/education")]
    [HttpGet("/deals/student")]
    [HttpGet("/business/register")]
    [HttpGet("/business/offers")]
    [HttpGet("/faq")]
    [HttpGet("/return-policy")]
    [HttpGet("/warranty")]
    [HttpGet("/shipping")]
    [HttpGet("/installment")]
    [HttpGet("/about")]
    [HttpGet("/careers")]
    [HttpGet("/news")]
    [HttpGet("/contact")]
    [HttpGet("/Home/Contact")]
    [HttpGet("/privacy")]
    [HttpGet("/terms")]
    public IActionResult Details()
    {
        var slug = Request.Path.Value?.Trim('/').ToLowerInvariant() ?? string.Empty;
        if (string.Equals(slug, "home/contact", StringComparison.OrdinalIgnoreCase))
        {
            slug = "contact";
        }

        if (!Pages.TryGetValue(slug, out var page))
        {
            return NotFound();
        }

        ViewData["Title"] = page.Title;
        return View("~/Views/Shared/StaticInfoPage.cshtml", page);
    }

    private static StaticInfoPageViewModel Create(
        string slug,
        string title,
        string eyebrow,
        string description,
        IReadOnlyList<string> highlights,
        string primaryActionLabel,
        string primaryActionUrl) =>
        new(slug, title, eyebrow, description, highlights, primaryActionLabel, primaryActionUrl);
}
