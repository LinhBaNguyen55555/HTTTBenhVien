namespace HTTTBenhVien.Models;

public class DatLichViewModel
{
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }
    public string? Address { get; set; }
    public string? BhytCode { get; set; }
    public int? DoctorId { get; set; }
    public DateTime? AppDate { get; set; }
    public string? TimeSlot { get; set; }
    public string? Reason { get; set; }
}
