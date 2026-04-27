using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public class ChuyenKhoaViewModel
{
    public List<Department> KhoaLamSang { get; set; } = new();
    public List<Department> KhoaCanLamSang { get; set; } = new();
    public List<Department> KhoaCapCuu { get; set; } = new();
}
