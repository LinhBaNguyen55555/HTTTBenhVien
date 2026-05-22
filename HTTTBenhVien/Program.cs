using HTTTBenhVien.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<HtttbenhvienContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ═══════════════════════════════════════════════════════
// Bỏ qua Authentication tạm thời (Dev Mode)
var devMode = builder.Configuration.GetValue<bool>("DevMode:BypassAuth", false);
// ═══════════════════════════════════════════════════════

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Account/Login";
        options.LogoutPath = "/Admin/Account/Logout";
        options.AccessDeniedPath = "/Admin/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // Xử lý chuyển hướng dựa trên khu vực truy cập
        options.Events.OnRedirectToLogin = context =>
        {
            if (devMode)
            {
                context.Response.Redirect(context.Request.Path);
                return Task.CompletedTask;
            }

            if (context.Request.Path.StartsWithSegments("/Admin"))
            {
                context.Response.Redirect("/Admin/Account/Login?ReturnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
            }
            else
            {
                context.Response.Redirect("/Account/Login?ReturnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
            }
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (devMode)
            {
                context.Response.Redirect(context.Request.Path);
                return Task.CompletedTask;
            }

            if (context.Request.Path.StartsWithSegments("/Admin"))
            {
                context.Response.Redirect("/Admin/Account/AccessDenied");
            }
            else
            {
                context.Response.Redirect("/Account/Login");
            }
            return Task.CompletedTask;
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DoctorOnly", policy => policy.RequireRole("Admin", "BacSi"));
    options.AddPolicy("ReceptionistOnly", policy => policy.RequireRole("Admin", "LeTan"));
    options.AddPolicy("AccountingOnly", policy => policy.RequireRole("Admin", "KeToan"));
    options.AddPolicy("PharmacyOnly", policy => policy.RequireRole("Admin", "DuocSi"));
    options.AddPolicy("DoctorOrAdmin", policy => policy.RequireRole("Admin", "BacSi"));
    options.AddPolicy("TechnicianOnly", policy => policy.RequireRole("Admin", "KyThuat"));
    options.AddPolicy("CashierOnly", policy => policy.RequireRole("Admin", "ThuNgan"));
    options.AddPolicy("PatientOnly", policy => policy.RequireRole("Patient"));
});

// Đăng ký IMemoryCache cho session
builder.Services.AddMemoryCache();

// Connect VNPay API
builder.Services.AddScoped<HTTTBenhVien.Services.IVnPayService, HTTTBenhVien.Services.VnPayService>();

// Register HttpClient and AI Chatbot Service
builder.Services.AddHttpClient();
builder.Services.AddScoped<HTTTBenhVien.Services.IGeminiChatService, HTTTBenhVien.Services.GeminiChatService>();

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HtttbenhvienContext>();
    await SeedData(context);
}

async Task SeedData(HtttbenhvienContext context)
{
    // Auto-migration for emergency columns
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'IsEmergency' AND Object_ID = OBJECT_ID('MedicalRecords'))
            BEGIN
                ALTER TABLE MedicalRecords ADD IsEmergency BIT NULL CONSTRAINT DF_MedicalRecords_IsEmergency DEFAULT 0;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'TriageLevel' AND Object_ID = OBJECT_ID('MedicalRecords'))
            BEGIN
                ALTER TABLE MedicalRecords ADD TriageLevel INT NULL;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'TriageTime' AND Object_ID = OBJECT_ID('MedicalRecords'))
            BEGIN
                ALTER TABLE MedicalRecords ADD TriageTime DATETIME NULL;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'TriageNotes' AND Object_ID = OBJECT_ID('MedicalRecords'))
            BEGIN
                ALTER TABLE MedicalRecords ADD TriageNotes NVARCHAR(1000) NULL;
            END
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error running emergency migration: " + ex.Message);
    }

    if (!await context.Accounts.AnyAsync())
    {
        if (!await context.Departments.AnyAsync())
        {
            var depts = new List<Department>
            {
                new() { DeptName = "Khoa Nội tổng hợp", Location = "Tầng 2 - Nhà A" },
                new() { DeptName = "Khoa Ngoại tổng hợp", Location = "Tầng 3 - Nhà A" },
                new() { DeptName = "Khoa Sản", Location = "Tầng 4 - Nhà B" },
                new() { DeptName = "Khoa Nhi", Location = "Tầng 3 - Nhà B" },
                new() { DeptName = "Khoa Tim mạch", Location = "Tầng 5 - Nhà A" },
                new() { DeptName = "Khoa Thần kinh", Location = "Tầng 4 - Nhà A" },
                new() { DeptName = "Khoa Xét nghiệm", Location = "Tầng 1 - Nhà C" },
                new() { DeptName = "Khoa Chẩn đoán hình ảnh", Location = "Tầng 1 - Nhà C" },
                new() { DeptName = "Khoa Dược", Location = "Tầng 1 - Nhà B" },
                new() { DeptName = "Phòng Khám đa khoa", Location = "Tầng 1 - Nhà A" },
                new() { DeptName = "Khoa Cấp cứu", Location = "Tầng trệt - Nhà A" },
                new() { DeptName = "Khoa Hồi sức tích cực", Location = "Tầng 5 - Nhà B" },
            };
            context.Departments.AddRange(depts);
            await context.SaveChangesAsync();
        }

        if (!await context.Employees.AnyAsync())
        {
            var deptList = await context.Departments.ToListAsync();
            var adminDept = deptList.FirstOrDefault(d => d.DeptName == "Khoa Nội tổng hợp");
            var doctorDept = deptList.FirstOrDefault(d => d.DeptName == "Khoa Tim mạch");
            var nurseDept = deptList.FirstOrDefault(d => d.DeptName == "Phòng Khám đa khoa");
            var pharmacyDept = deptList.FirstOrDefault(d => d.DeptName == "Khoa Dược");

            var emps = new List<Employee>
            {
                new() { FullName = "Nguyễn Văn Minh", Email = "minh.nv@benhvien.vn", Phone = "0901234567", DeptId = adminDept?.DeptId, JobTitle = "Giám đốc bệnh viện", Specialty = "Quản lý y tế" },
                new() { FullName = "Trần Thị Hương", Email = "huong.tt@benhvien.vn", Phone = "0902345678", DeptId = doctorDept?.DeptId, JobTitle = "Bác sĩ chuyên khoa II", Specialty = "Tim mạch" },
                new() { FullName = "Lê Đình Phong", Email = "phong.ld@benhvien.vn", Phone = "0903456789", DeptId = nurseDept?.DeptId, JobTitle = "Lễ tân trưởng", Specialty = "" },
                new() { FullName = "Phạm Thị Lan", Email = "lan.pt@benhvien.vn", Phone = "0904567890", DeptId = pharmacyDept?.DeptId, JobTitle = "Dược sĩ trưởng", Specialty = "Dược lâm sàng" },
                new() { FullName = "Bác sĩ Cấp cứu", Email = "capcuu.bs@benhvien.vn", Phone = "0909999999", DeptId = deptList.FirstOrDefault(d => d.DeptName == "Khoa Cấp cứu")?.DeptId, JobTitle = "Bác sĩ Cấp cứu", Specialty = "Hồi sức cấp cứu" },
            };
            context.Employees.AddRange(emps);
            await context.SaveChangesAsync();
        }

        var empsList = await context.Employees.ToListAsync();
        var admin = empsList.FirstOrDefault(e => e.FullName == "Nguyễn Văn Minh");
        var doctor = empsList.FirstOrDefault(e => e.FullName == "Trần Thị Hương");
        var nurse = empsList.FirstOrDefault(e => e.FullName == "Lê Đình Phong");
        var pharm = empsList.FirstOrDefault(e => e.FullName == "Phạm Thị Lan");

        var capcuu = empsList.FirstOrDefault(e => e.FullName == "Bác sĩ Cấp cứu");

        string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "HTTTBenhVien_Salt_2024"));
            return Convert.ToBase64String(bytes);
        }

        var accounts = new List<Account>();
        if (admin != null)
            accounts.Add(new() { EmpId = admin.EmpId, Username = "admin", Password = HashPassword("admin123"), RoleName = "Admin", IsActive = true });
        if (doctor != null)
            accounts.Add(new() { EmpId = doctor.EmpId, Username = "bacsi01", Password = HashPassword("123456"), RoleName = "BacSi", IsActive = true });
        if (nurse != null)
            accounts.Add(new() { EmpId = nurse.EmpId, Username = "letan01", Password = HashPassword("123456"), RoleName = "LeTan", IsActive = true });
        if (pharm != null)
            accounts.Add(new() { EmpId = pharm.EmpId, Username = "duocsi01", Password = HashPassword("123456"), RoleName = "DuocSi", IsActive = true });
        if (capcuu != null)
            accounts.Add(new() { EmpId = capcuu.EmpId, Username = "capcuu01", Password = HashPassword("123456"), RoleName = "CapCuu", IsActive = true });

        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
    }

    if (!await context.Articles.AnyAsync())
    {
        var adminEmp = await context.Employees.FirstOrDefaultAsync(e => e.FullName == "Nguyễn Văn Minh");

        var articles = new List<Article>
        {
            new()
            {
                Title = "Bệnh viện triển khai hệ thống đặt lịch khám trực tuyến",
                Slug = "benh-vien-trien-khai-dat-lich-kham-truc-tuyen",
                Summary = "Hệ thống đặt lịch khám trực tuyến giúp bệnh nhân chủ động sắp xếp thời gian, giảm thời gian chờ đợi tại bệnh viện.",
                Content = "<p>Bệnh viện chúng tôi chính thức triển khai hệ thống đặt lịch khám trực tuyến...</p>",
                Category = "Tin tức",
                AuthorId = adminEmp?.EmpId,
                PublishDate = DateTime.Now,
                Status = "Published",
                ViewCount = 156,
                ThumbnailUrl = "/assets/img/news-1.jpg"
            },
            new()
            {
                Title = "Cách phòng tránh bệnh tim mạch trong mùa nắng nóng",
                Slug = "cach-phong-tranh-benh-tim-mach-mua-nang-nong",
                Summary = "Mùa hè là thời điểm nguy hiểm cho sức khỏe tim mạch. Hãy cùng bác sĩ chuyên khoa Tim mạch tìm hiểu cách phòng tránh.",
                Content = "<p>Nắng nóng gay gắt kéo dài có thể gây ra nhiều biến chứng nguy hiểm cho tim mạch...</p>",
                Category = "Kiến thức y khoa",
                AuthorId = adminEmp?.EmpId,
                PublishDate = DateTime.Now.AddDays(-5),
                Status = "Published",
                ViewCount = 243,
                ThumbnailUrl = "/assets/img/news-2.jpg"
            },
            new()
            {
                Title = "Bệnh viện tổ chức khám sức khỏe định kỳ cho công nhân",
                Slug = "benh-vien-to-chuc-kham-suc-khoe-dinh-ky",
                Summary = "Đợt khám sức khỏe định kỳ tháng 4/2026 dành cho 500 công nhân tại các khu công nghiệp trên địa bàn.",
                Content = "<p>Nhằm nâng cao sức khỏe cho người lao động, bệnh viện phối hợp với các doanh nghiệp tổ chức khám định kỳ...</p>",
                Category = "Sự kiện",
                AuthorId = adminEmp?.EmpId,
                PublishDate = DateTime.Now.AddDays(-10),
                Status = "Published",
                ViewCount = 89,
                ThumbnailUrl = "/assets/img/news-3.jpg"
            },
            new()
            {
                Title = "Giới thiệu kỹ thuật mổ nội soi hiện đại tại bệnh viện",
                Slug = "gioi-thieu-ky-thuat-mo-noi-soi",
                Summary = "Bệnh viện đầu tư hệ thống nội soi hiện đại, giúp chẩn đoán và điều trị nhiều bệnh lý với độ chính xác cao.",
                Content = "<p>Công nghệ nội soi tiên tiến giúp rút ngắn thời gian phẫu thuật và hồi phục...</p>",
                Category = "Kiến thức y khoa",
                AuthorId = adminEmp?.EmpId,
                PublishDate = DateTime.Now.AddDays(-15),
                Status = "Published",
                ViewCount = 312,
                ThumbnailUrl = "/assets/img/news-4.jpg"
            },
        };
        context.Articles.AddRange(articles);
        await context.SaveChangesAsync();
    }

    if (!await context.WebMenus.AnyAsync())
    {
        var trangChu = new WebMenu { MenuName = "Trang chủ", MenuUrl = "/", DisplayOrder = 1, IconClass = "bi bi-house" };
        context.WebMenus.Add(trangChu);

        var gioiThieu = new WebMenu { MenuName = "Giới thiệu", MenuUrl = "/About", DisplayOrder = 2, IconClass = "bi bi-info-circle" };
        context.WebMenus.Add(gioiThieu);
        await context.SaveChangesAsync();

        var gioiThieuChildren = new List<WebMenu>
        {
            new() { MenuName = "Lịch sử hình thành", MenuUrl = "/About/LichSu", ParentId = gioiThieu.MenuId, DisplayOrder = 1 },
            new() { MenuName = "Tầm nhìn & sứ mệnh", MenuUrl = "/About/TamNhinSuMenh", ParentId = gioiThieu.MenuId, DisplayOrder = 2 },
            new() { MenuName = "Cơ sở vật chất", MenuUrl = "/About/CoSoVatChat", ParentId = gioiThieu.MenuId, DisplayOrder = 3 },
        };
        context.WebMenus.AddRange(gioiThieuChildren);

        var chuyenKhoa = new WebMenu { MenuName = "Chuyên khoa", MenuUrl = "/Specialty", DisplayOrder = 3, IconClass = "bi bi-bezier" };
        var bacSi = new WebMenu { MenuName = "Bác sĩ", MenuUrl = "/Doctor", DisplayOrder = 4, IconClass = "bi bi-person-badge" };
        var tinTuc = new WebMenu { MenuName = "Tin tức", MenuUrl = "/News", DisplayOrder = 5, IconClass = "bi bi-newspaper" };
        var lienHe = new WebMenu { MenuName = "Liên hệ", MenuUrl = "/Contact", DisplayOrder = 6, IconClass = "bi bi-envelope" };
        var datLich = new WebMenu { MenuName = "Đặt lịch khám", MenuUrl = "/Booking", DisplayOrder = 7, IconClass = "bi bi-calendar-check" };

        context.WebMenus.AddRange(chuyenKhoa, bacSi, tinTuc, lienHe, datLich);
        await context.SaveChangesAsync();
    }

    if (!await context.IcdDictionaries.AnyAsync())
    {
        var icds = new List<IcdDictionary>
        {
            new() { IcdCode = "J18.9", DiseaseName = "Viêm phổi, không xác định", Chapter = "Bệnh hệ hô hấp", IsActive = true },
            new() { IcdCode = "I10", DiseaseName = "Tăng huyết áp (Nguyên phát)", Chapter = "Bệnh hệ tuần hoàn", IsActive = true },
            new() { IcdCode = "E11", DiseaseName = "Đái tháo đường type 2", Chapter = "Bệnh hệ nội tiết", IsActive = true },
            new() { IcdCode = "J06.9", DiseaseName = "Nhiễm trùng hô hấp trên cấp tính", Chapter = "Bệnh hệ hô hấp", IsActive = true },
            new() { IcdCode = "K29.5", DiseaseName = "Viêm dạ dày mạn tính", Chapter = "Bệnh hệ tiêu hóa", IsActive = true },
            new() { IcdCode = "M54.5", DiseaseName = "Đau thắt lưng dưới", Chapter = "Bệnh hệ cơ xương", IsActive = true },
            new() { IcdCode = "N39.0", DiseaseName = "Nhiễm trùng đường tiết niệu", Chapter = "Bệnh hệ tiết niệu", IsActive = true },
            new() { IcdCode = "A09", DiseaseName = "Tiêu chảy và viêm dạ dày ruột", Chapter = "Bệnh lý tiêu hóa", IsActive = true },
            new() { IcdCode = "R50.9", DiseaseName = "Sốt không rõ nguyên nhân", Chapter = "Triệu chứng và dấu hiệu", IsActive = true },
            new() { IcdCode = "J45.9", DiseaseName = "Hen phế quản, không đặc hiệu", Chapter = "Bệnh hệ hô hấp", IsActive = true },
        };
        context.IcdDictionaries.AddRange(icds);
        await context.SaveChangesAsync();
    }

    if (!await context.MedicalServices.AnyAsync())
    {
        var services = new List<MedicalService>
        {
            new() { ServiceName = "Khám Lâm Sàng Chung", BasePrice = 150000, Description = "Phí khám ban đầu" },
            new() { ServiceName = "Khám Chuyên Khoa", BasePrice = 250000, Description = "Khám trực tiếp bác sĩ chuyên khoa" },
            new() { ServiceName = "Khám Giáo Sư/PGS", BasePrice = 500000, Description = "Khám chuyên gia" },
            new() { ServiceName = "Xét nghiệm Máu tổng quát", BasePrice = 120000, Description = "Công thức máu 32 chỉ số" },
            new() { ServiceName = "Xét nghiệm Sinh hóa", BasePrice = 180000, Description = "Men gan, đường huyết, mỡ máu" },
            new() { ServiceName = "Xét nghiệm Nước tiểu", BasePrice = 50000, Description = "Tổng phân tích nước tiểu" },
            new() { ServiceName = "Siêu âm Ổ bụng", BasePrice = 150000, Description = "Siêu âm tổng quát" },
            new() { ServiceName = "Siêu âm Thai 4D", BasePrice = 400000, Description = "Siêu âm hình thái học" },
            new() { ServiceName = "Siêu âm Tim", BasePrice = 350000, Description = "Siêu âm Doppler màu" },
            new() { ServiceName = "Chụp X-Quang Phổi thẳng", BasePrice = 100000, Description = "Chụp X-Quang kỹ thuật số" },
            new() { ServiceName = "Chụp X-Quang Xương", BasePrice = 120000, Description = "Chụp các khớp" },
            new() { ServiceName = "Chụp CT Scanner (Không tiêm)", BasePrice = 1200000, Description = "Chụp cắt lớp vi tính" },
            new() { ServiceName = "Chụp MRI 1.5 Tesla", BasePrice = 2500000, Description = "Chụp cộng hưởng từ" },
            new() { ServiceName = "Nội soi Dạ dày (Gây mê)", BasePrice = 1500000, Description = "Nội soi tiêu hóa trên" },
            new() { ServiceName = "Nội soi Đại tràng (Gây mê)", BasePrice = 2000000, Description = "Nội soi tiêu hóa dưới" },
            new() { ServiceName = "Đo Điện Tâm Đồ (ECG)", BasePrice = 80000, Description = "Ghi điện tim thường quy" },
            new() { ServiceName = "Đo Điện Não Đồ (EEG)", BasePrice = 250000, Description = "Đo sóng não" },
            new() { ServiceName = "Phẫu thuật nội soi ruột thừa", BasePrice = 7000000, Description = "Mổ nội soi cắt ruột thừa" },
            new() { ServiceName = "Nhổ răng khôn mọc ngầm", BasePrice = 2000000, Description = "Tiểu phẫu răng miệng" },
            new() { ServiceName = "Giường bệnh Nội trú (Ngày)", BasePrice = 300000, Description = "Tiền giường thường" },
        };
        context.MedicalServices.AddRange(services);
        await context.SaveChangesAsync();
    }

    if (!await context.Medicines.AnyAsync())
    {
        var meds = new List<Medicine>
        {
            new() { MedicineName = "Paracetamol 500mg", Unit = "Viên", Price = 500, StockQuantity = 5000, ExpiryDate = DateTime.Now.AddYears(2), Description = "Hạ sốt, giảm đau" },
            new() { MedicineName = "Amoxicillin 500mg", Unit = "Viên", Price = 800, StockQuantity = 3000, ExpiryDate = DateTime.Now.AddYears(1), Description = "Kháng sinh nhóm penicillin" },
            new() { MedicineName = "Omeprazole 20mg", Unit = "Viên", Price = 1500, StockQuantity = 2000, ExpiryDate = DateTime.Now.AddYears(2), Description = "Thuốc ức chế bơm proton" },
            new() { MedicineName = "Metformin 500mg", Unit = "Viên", Price = 600, StockQuantity = 4000, ExpiryDate = DateTime.Now.AddYears(1), Description = "Điều trị đái tháo đường type 2" },
            new() { MedicineName = "Amlodipine 5mg", Unit = "Viên", Price = 900, StockQuantity = 2500, ExpiryDate = DateTime.Now.AddYears(2), Description = "Thuốc hạ áp" },
            new() { MedicineName = "Ibuprofen 400mg", Unit = "Viên", Price = 700, StockQuantity = 3000, ExpiryDate = DateTime.Now.AddYears(1), Description = "Kháng viêm, giảm đau" },
            new() { MedicineName = "Vitamin B1 50mg", Unit = "Viên", Price = 300, StockQuantity = 6000, ExpiryDate = DateTime.Now.AddYears(3), Description = "Bổ sung vitamin B1" },
            new() { MedicineName = "Dexamethasone 0.5mg", Unit = "Viên", Price = 400, StockQuantity = 1500, ExpiryDate = DateTime.Now.AddYears(2), Description = "Corticosteroid kháng viêm" },
            new() { MedicineName = "Salbutamol 2mg", Unit = "Viên", Price = 500, StockQuantity = 2000, ExpiryDate = DateTime.Now.AddYears(1), Description = "Thuốc giãn phế quản" },
            new() { MedicineName = "Ranitidine 150mg", Unit = "Viên", Price = 400, StockQuantity = 3500, ExpiryDate = DateTime.Now.AddYears(2), Description = "Thuốc giảm tiết acid dạ dày" },
        };
        context.Medicines.AddRange(meds);
        await context.SaveChangesAsync();
    }
}

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Admin/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session phải đăng ký TRƯỚC dev auth bypass
app.UseSession();

if (devMode)
{
    app.Use(async (context, next) =>
    {
        // Tự động tạo ClaimsPrincipal giả nếu chưa đăng nhập
        if (context.User.Identity?.IsAuthenticated != true)
        {
            // Mặc định là Admin, có thể thay đổi bằng query string: ?role=BacSi
            var selectedRole = context.Request.Query["role"].FirstOrDefault() ?? "Admin";
            
            // Map query string value to actual role
            var roleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "admin", "Admin" },
                { "bacsi", "BacSi" },
                { "letan", "LeTan" },
                { "duocsi", "DuocSi" },
                { "kythuat", "KyThuat" },
                { "thungan", "ThuNgan" }
            };
            
            var actualRole = roleMap.GetValueOrDefault(selectedRole, "Admin");
            
            var devClaims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "0"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "DevUser"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, actualRole),
                new System.Security.Claims.Claim("EmpId", "0"),
                new System.Security.Claims.Claim("Username", "dev"),
                new System.Security.Claims.Claim("WorkspaceRole", actualRole)
            };
            var devIdentity = new System.Security.Claims.ClaimsIdentity(devClaims, "DevAuth");
            context.User = new System.Security.Claims.ClaimsPrincipal(devIdentity);
        }
        await next();
    });
}
// ═══════════════════════════════════════════════════════

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
