using System.Security.Claims;

namespace HTTTBenhVien.Helpers;

/// <summary>

/// </summary>
public static class RoleNavigationHelper
{
    /// <summary>

    /// </summary>
    public static WorkspaceInfo GetCurrentWorkspace(ClaimsPrincipal user)
    {
        var role = user.FindFirst("WorkspaceRole")?.Value
                   ?? user.FindFirst(ClaimTypes.Role)?.Value
                   ?? "Admin";

        return role switch
        {
            "BacSi" => new WorkspaceInfo
            {
                Role = "BacSi",
                RoleName = "Doctor",
                DisplayName = "Bác sĩ lâm sàng",
                Icon = "fa-user-md",
                Description = "Khám bệnh, kê đơn, chỉ định",
                Color = "#e74c3c",
                Url = "/Admin/BacSi"
            },
            "KyThuat" => new WorkspaceInfo
            {
                Role = "KyThuat",
                RoleName = "Technician",
                DisplayName = "Kỹ thuật viên",
                Icon = "fa-flask",
                Description = "Xét nghiệm, chẩn đoán hình ảnh",
                Color = "#36b9cc",
                Url = "/Admin/KyThuat"
            },
            "DuocSi" => new WorkspaceInfo
            {
                Role = "DuocSi",
                RoleName = "Pharmacist",
                DisplayName = "Dược sĩ / Thủ kho",
                Icon = "fa-pills",
                Description = "Cấp phát thuốc, quản lý kho",
                Color = "#1cc88a",
                Url = "/Admin/DuocSy"
            },
            "ThuNgan" => new WorkspaceInfo
            {
                Role = "ThuNgan",
                RoleName = "Cashier",
                DisplayName = "Thu ngân",
                Icon = "fa-cash-register",
                Description = "Thanh toán viện phí, hóa đơn",
                Color = "#f6c23e",
                Url = "/Admin/ThuNgan"
            },
            "LeTan" => new WorkspaceInfo
            {
                Role = "LeTan",
                RoleName = "Receptionist",
                DisplayName = "Lễ tân",
                Icon = "fa-headset",
                Description = "Tiếp đón, chăm sóc khách hàng",
                Color = "#9b59b6",
                Url = "/Admin/LeTan"
            },
            _ => new WorkspaceInfo
            {
                Role = "Admin",
                RoleName = "Admin",
                DisplayName = "Quản trị viên",
                Icon = "fa-shield-alt",
                Description = "Quản lý toàn diện hệ thống",
                Color = "#175cdd",
                Url = "/Admin"
            }
        };
    }

    /// <summary>

    /// </summary>
    public static string GetUserRole(ClaimsPrincipal user)
    {
        return user.FindFirst("WorkspaceRole")?.Value
               ?? user.FindFirst(ClaimTypes.Role)?.Value
               ?? "Admin";
    }

    /// <summary>

    /// </summary>
    public static bool HasRole(ClaimsPrincipal user, params string[] roles)
    {
        var userRole = GetUserRole(user);

        if (userRole == "Admin") return true;
        return roles.Contains(userRole);
    }

    /// <summary>

    /// </summary>
    public static List<MenuItem> GetDoctorMenu()
    {
        return new List<MenuItem>
        {
            new() { IsHeader = true, HeaderText = "Khám bệnh" },
            new() { Title = "Lịch khám hôm nay", Icon = "fa-calendar-check", Url = "/Admin/BacSi", Badge = "HOT", BadgeClass = "bg-danger" },
            new() { Title = "Gọi bệnh nhân", Icon = "fa-bullhorn", Url = "/Admin/BacSi/GoiKham" },
            new() { Title = "Hồ sơ bệnh án", Icon = "fa-file-medical-alt", Url = "/Admin/HoSoBenhAn" },
            new() { Title = "Chỉ định dịch vụ", Icon = "fa-laptop-medical", Url = "/Admin/ChiDinh" },
            new() { Title = "Kê đơn thuốc", Icon = "fa-prescription-bottle-alt", Url = "/Admin/DonThuoc" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Tra cứu" },
            new() { Title = "Lịch sử bệnh nhân", Icon = "fa-history", Url = "/Admin/BenhNhan" },
            new() { Title = "Danh mục ICD-10", Icon = "fa-book-medical", Url = "/Admin/DanhMuc" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Hệ thống" },
            new() { Title = "Đổi mật khẩu", Icon = "fa-key", Url = "/Admin/Account/ChangePassword" },
        };
    }

    /// <summary>

    /// </summary>
    public static List<MenuItem> GetTechnicianMenu()
    {
        return new List<MenuItem>
        {
            new() { IsHeader = true, HeaderText = "Cận lâm sàng" },
            new() { Title = "Chờ thực hiện", Icon = "fa-hourglass-half", Url = "/Admin/KyThuat", Badge = "NEW", BadgeClass = "bg-warning" },
            new() { Title = "Đang thực hiện", Icon = "fa-spinner", Url = "/Admin/KyThuat/DangThucHien" },
            new() { Title = "Hoàn thành", Icon = "fa-check-circle", Url = "/Admin/KyThuat/HoanThanh" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Danh mục" },
            new() { Title = "Dịch vụ CLS", Icon = "fa-list-ul", Url = "/Admin/KhamBenh" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Hệ thống" },
            new() { Title = "Đổi mật khẩu", Icon = "fa-key", Url = "/Admin/Account/ChangePassword" },
        };
    }

    /// <summary>

    /// </summary>
    public static List<MenuItem> GetPharmacistMenu()
    {
        return new List<MenuItem>
        {
            new() { IsHeader = true, HeaderText = "Dược phẩm" },
            new() { Title = "Cấp phát thuốc", Icon = "fa-hand-holding-medical", Url = "/Admin/DuocSy", Badge = "NEW", BadgeClass = "bg-success" },
            new() { Title = "Danh sách thuốc", Icon = "fa-pills", Url = "/Admin/KhoDuoc" },
            new() { Title = "Nhập kho", Icon = "fa-truck-loading", Url = "/Admin/KhoDuoc/NhapKho" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Cảnh báo" },
            new() { Title = "Thuốc sắp hết hàng", Icon = "fa-exclamation-triangle", Url = "/Admin/KhoDuoc/BaoCaoTon", BadgeClass = "bg-danger" },
            new() { Title = "Thuốc sắp hết hạn", Icon = "fa-clock", Url = "/Admin/KhoDuoc/SapHetHan" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Hệ thống" },
            new() { Title = "Đổi mật khẩu", Icon = "fa-key", Url = "/Admin/Account/ChangePassword" },
        };
    }

    /// <summary>

    /// </summary>
    public static List<MenuItem> GetCashierMenu()
    {
        return new List<MenuItem>
        {
            new() { IsHeader = true, HeaderText = "Thanh toán" },
            new() { Title = "Chờ thanh toán", Icon = "fa-receipt", Url = "/Admin/ThuNgan", Badge = "NEW", BadgeClass = "bg-warning" },
            new() { Title = "Hóa đơn", Icon = "fa-file-invoice-dollar", Url = "/Admin/ThanhToan" },
            new() { Title = "Hoàn tiền", Icon = "fa-undo-alt", Url = "/Admin/ThuNgan/HoanTien" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Báo cáo" },
            new() { Title = "Doanh thu hôm nay", Icon = "fa-chart-line", Url = "/Admin/ThuNgan/BaoCao" },
            new() { Title = "Báo cáo theo ca", Icon = "fa-clock", Url = "/Admin/ThuNgan/BaoCaoCa" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Hệ thống" },
            new() { Title = "Đổi mật khẩu", Icon = "fa-key", Url = "/Admin/Account/ChangePassword" },
        };
    }

    /// <summary>

    /// </summary>
    public static List<MenuItem> GetReceptionistMenu()
    {
        return new List<MenuItem>
        {
            new() { IsHeader = true, HeaderText = "Tiếp đón" },
            new() { Title = "Tiếp nhận mới", Icon = "fa-user-plus", Url = "/Admin/LeTan", Badge = "NEW", BadgeClass = "bg-success" },
            new() { Title = "Quản lý bệnh nhân", Icon = "fa-users", Url = "/Admin/BenhNhan" },
            new() { Title = "Lịch hẹn khám", Icon = "fa-calendar-alt", Url = "/Admin/LichKham" },
            new() { Title = "Phân phòng khám", Icon = "fa-door-open", Url = "/Admin/LeTan/PhanPhong" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Chăm sóc KH" },
            new() { Title = "Phản hồi khách hàng", Icon = "fa-headset", Url = "/Admin/LienHe", BadgeClass = "bg-info" },
            new() { Title = "Tin nhắn chờ", Icon = "fa-comment-dots", Url = "/Admin/LeTan/TinNhan" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Hệ thống" },
            new() { Title = "Đổi mật khẩu", Icon = "fa-key", Url = "/Admin/Account/ChangePassword" },
        };
    }

    /// <summary>

    /// </summary>
    public static List<MenuItem> GetAdminMenu()
    {
        return new List<MenuItem>
        {
            new() { IsHeader = true, HeaderText = "Tổng quan" },
            new() { Title = "Dashboard", Icon = "fa-chart-pie", Url = "/Admin" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Khám bệnh" },
            new() { Title = "Bệnh nhân", Icon = "fa-users", Url = "/Admin/BenhNhan" },
            new() { Title = "Tiếp nhận khám", Icon = "fa-clipboard-list", Url = "/Admin/TiepNhan" },
            new() { Title = "Lịch hẹn khám", Icon = "fa-calendar-check", Url = "/Admin/LichKham" },
            new() { Title = "Phòng khám", Icon = "fa-stethoscope", Url = "/Admin/KhamBenh" },
            new() { Title = "Hồ sơ bệnh án", Icon = "fa-file-medical", Url = "/Admin/HoSoBenhAn" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Dược & CLS" },
            new() { Title = "Kê đơn thuốc", Icon = "fa-pills", Url = "/Admin/DonThuoc" },
            new() { Title = "Kho dược", Icon = "fa-mortar-pestle", Url = "/Admin/KhoDuoc" },
            new() { Title = "Chỉ định dịch vụ", Icon = "fa-flask", Url = "/Admin/ChiDinh" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Tài chính" },
            new() { Title = "Thanh toán viện phí", Icon = "fa-file-invoice-dollar", Url = "/Admin/ThanhToan" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Quản trị hệ thống" },
            new() { Title = "Nhân sự", Icon = "fa-user-tie", Url = "/Admin/NhanVien" },
            new() { Title = "Khoa - Phòng", Icon = "fa-building", Url = "/Admin/KhoaPhong" },
            new() { Title = "Danh mục ICD", Icon = "fa-list-ul", Url = "/Admin/DanhMuc" },
            new() { Title = "Quản lý nội dung", Icon = "fa-newspaper", Url = "/Admin/BaiViet" },
            new() { Title = "Liên hệ", Icon = "fa-envelope", Url = "/Admin/LienHe" },
            new() { Title = "Tài khoản", Icon = "fa-user-circle", Url = "/Admin/Account" },
            new() { IsDivider = true },
            new() { IsHeader = true, HeaderText = "Hệ thống" },
            new() { Title = "Đổi mật khẩu", Icon = "fa-key", Url = "/Admin/Account/ChangePassword" },
            new() { Title = "Đăng xuất", Icon = "fa-sign-out-alt", Url = "/Admin/Account/Logout" },
        };
    }

    /// <summary>

    /// </summary>
    public static List<MenuItem> GetSidebarMenu(ClaimsPrincipal user)
    {
        var role = GetUserRole(user);
        return role switch
        {
            "BacSi" => GetDoctorMenu(),
            "KyThuat" => GetTechnicianMenu(),
            "DuocSi" => GetPharmacistMenu(),
            "ThuNgan" => GetCashierMenu(),
            "LeTan" => GetReceptionistMenu(),
            _ => GetAdminMenu()
        };
    }

    /// <summary>

    /// </summary>
    public static List<WorkspaceInfo> GetAllWorkspaces()
    {
        return new List<WorkspaceInfo>
        {
            new()
            {
                Role = "Admin",
                RoleName = "Admin",
                DisplayName = "Quản trị viên",
                Icon = "fa-shield-alt",
                Description = "Quản lý toàn diện hệ thống",
                Color = "#175cdd",
                Url = "/Admin"
            },
            new()
            {
                Role = "BacSi",
                RoleName = "Doctor",
                DisplayName = "Bác sĩ lâm sàng",
                Icon = "fa-user-md",
                Description = "Khám bệnh, kê đơn, chỉ định",
                Color = "#e74c3b",
                Url = "/Admin/BacSi"
            },
            new()
            {
                Role = "KyThuat",
                RoleName = "Technician",
                DisplayName = "Kỹ thuật viên",
                Icon = "fa-flask",
                Description = "Xét nghiệm, chẩn đoán hình ảnh",
                Color = "#36b9cc",
                Url = "/Admin/KyThuat"
            },
            new()
            {
                Role = "DuocSi",
                RoleName = "Pharmacist",
                DisplayName = "Dược sĩ / Thủ kho",
                Icon = "fa-pills",
                Description = "Cấp phát thuốc, quản lý kho",
                Color = "#1cc88a",
                Url = "/Admin/DuocSy"
            },
            new()
            {
                Role = "ThuNgan",
                RoleName = "Cashier",
                DisplayName = "Thu ngân",
                Icon = "fa-cash-register",
                Description = "Thanh toán viện phí, hóa đơn",
                Color = "#f6c23e",
                Url = "/Admin/ThuNgan"
            },
            new()
            {
                Role = "LeTan",
                RoleName = "Receptionist",
                DisplayName = "Lễ tân",
                Icon = "fa-headset",
                Description = "Tiếp đón, chăm sóc khách hàng",
                Color = "#9b59b6",
                Url = "/Admin/LeTan"
            },
        };
    }
}
