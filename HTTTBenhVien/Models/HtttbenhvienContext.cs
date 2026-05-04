using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HTTTBenhVien.Models;

public partial class HtttbenhvienContext : DbContext
{
    public HtttbenhvienContext()
    {
    }

    public HtttbenhvienContext(DbContextOptions<HtttbenhvienContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Article> Articles { get; set; }

    public virtual DbSet<Bed> Beds { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<IcdDictionary> IcdDictionaries { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<MedicalService> MedicalServices { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<MedicalSupply> MedicalSupplies { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<PrescriptionDetail> PrescriptionDetails { get; set; }

    public virtual DbSet<RecordService> RecordServices { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }
    public virtual DbSet<DeptType> DeptTypes { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<WebMenu> WebMenus { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("data source=DESKTOP-B3RC6GD\\SQLEXPRESS; initial catalog=HTTTBENHVIEN; integrated security=True; TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Accounts__349DA586E964F386");

            entity.HasIndex(e => e.EmpId, "UQ__Accounts__AF2DBA781DED58BD").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Emp).WithOne(p => p.Account)
                .HasForeignKey<Account>(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Acc_Emp");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppId).HasName("PK__Appointm__8E2CF7D96CB096D0");

            entity.Property(e => e.AppId).HasColumnName("AppID");
            entity.Property(e => e.AppDate).HasColumnType("datetime");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("New");
            entity.Property(e => e.TimeSlot).HasMaxLength(20);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_App_Doctor");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_App_Patient");
        });

        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.ArticleId).HasName("PK__Articles__9C6270C88BD2C647");

            entity.HasIndex(e => e.Slug, "UQ__Articles__BC7B5FB634B076F4").IsUnique();

            entity.Property(e => e.ArticleId).HasColumnName("ArticleID");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.PublishDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Slug)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Published");
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.ThumbnailUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.ViewCount).HasDefaultValue(0);

            entity.HasOne(d => d.Author).WithMany(p => p.Articles)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Article_Author");
        });

        modelBuilder.Entity<Bed>(entity =>
        {
            entity.HasKey(e => e.BedId).HasName("PK__Beds__A8A7106004EE6D94");

            entity.Property(e => e.BedId).HasColumnName("BedID");
            entity.Property(e => e.BedNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IsOccupied).HasDefaultValue(false);
            entity.Property(e => e.PricePerDay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");

            entity.HasOne(d => d.Room).WithMany(p => p.Beds)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bed_Room");
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.ContactId).HasName("PK__Contacts__5C6625BB91B75D37");

            entity.Property(e => e.ContactId).HasColumnName("ContactID");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.ResolvedDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("New");
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.SubmitDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.ResolvedByNavigation).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.ResolvedBy)
                .HasConstraintName("FK_Contact_Emp");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DeptId).HasName("PK__Departme__0148818E93B9E716");

            entity.Property(e => e.DeptId).HasColumnName("DeptID");
            entity.Property(e => e.DeptName).HasMaxLength(100);
            entity.Property(e => e.DeptTypeId).HasColumnName("DeptTypeID");
            entity.Property(e => e.Location).HasMaxLength(100);

            entity.HasOne(d => d.DeptTypeNavigation).WithMany(p => p.Departments)
                .HasForeignKey(d => d.DeptTypeId)
                .HasConstraintName("FK_Dept_DeptType");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmpId).HasName("PK__Employee__AF2DBA7903F48E4E");

            entity.Property(e => e.EmpId).HasColumnName("EmpID");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DeptId).HasColumnName("DeptID");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.JobTitle).HasMaxLength(50);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Specialty).HasMaxLength(100);

            entity.HasOne(d => d.Dept).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Emp_Dept");
        });

        modelBuilder.Entity<IcdDictionary>(entity =>
        {
            entity.HasKey(e => e.IcdCode).HasName("PK__ICD_Dict__BCBDC73F2F901A72");

            entity.ToTable("ICD_Dictionary");

            entity.Property(e => e.IcdCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ICD_Code");
            entity.Property(e => e.Chapter).HasMaxLength(150);
            entity.Property(e => e.DiseaseName).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAD521F4F301");

            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.DiscountAmount).HasDefaultValue(0m).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BhytAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PatientAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);

            entity.HasOne(d => d.Patient).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Inv_Pat");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.InvDetailId).HasName("PK__InvoiceD__B2719D29C2615B29");

            entity.Property(e => e.InvDetailId).HasColumnName("InvDetailID");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ItemName).HasMaxLength(255);
            entity.Property(e => e.ItemType).HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ID_Inv");
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__MedicalR__FBDF78C9E1BCE01F");

            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.IcdCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ICD_Code");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.RecordType).HasMaxLength(50);
            entity.Property(e => e.VisitDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VitalSigns).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.Property(e => e.BloodPressure).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.Temperature).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 1)");

            entity.HasOne(d => d.Doctor).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rec_Doctor");

            entity.HasOne(d => d.IcdCodeNavigation).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.IcdCode)
                .HasConstraintName("FK_Record_ICD");

            entity.HasOne(d => d.Patient).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rec_Patient");
        });

        modelBuilder.Entity<MedicalService>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__MedicalS__C51BB0EAA0970EC1");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ServiceName).HasMaxLength(150);
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.MedicineId).HasName("PK__Medicine__4F2128F0DED9FA1C");

            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.MedicineName).HasMaxLength(150);
            entity.Property(e => e.MedicineCode).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.ActiveIngredient).HasMaxLength(150);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patients__970EC346ED987832");

            entity.HasIndex(e => e.PatientCode).IsUnique();
            entity.HasIndex(e => e.CitizenId, "UQ_Patient_CitizenID").IsUnique();

            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.PatientCode).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.Gender).HasColumnType("bit");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(15).IsUnicode(false);
            entity.Property(e => e.BhytCode).HasColumnName("BHYT_Code").HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Email).HasColumnName("Email").HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.AvatarUrl).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.Notes).HasColumnName("Notes").HasMaxLength(500);
            entity.Property(e => e.CitizenId).HasColumnName("CitizenID").HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Occupation).HasMaxLength(200);
            entity.Property(e => e.WorkplaceOrSchool).HasMaxLength(500);
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.PrescriptionId).HasName("PK__Prescrip__401308129EFB23C9");

            entity.Property(e => e.PrescriptionId).HasColumnName("PrescriptionID");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RecordId).HasColumnName("RecordID");

            entity.HasOne(d => d.Record).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.RecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pre_Rec");
        });

        modelBuilder.Entity<PrescriptionDetail>(entity =>
        {
            entity.HasKey(e => new { e.PrescriptionId, e.MedicineId }).HasName("PK__Prescrip__54E11A9DFB533D0F");

            entity.Property(e => e.PrescriptionId).HasColumnName("PrescriptionID");
            entity.Property(e => e.MedicineId).HasColumnName("MedicineID");
            entity.Property(e => e.Dosage).HasMaxLength(255);

            entity.HasOne(d => d.Medicine).WithMany(p => p.PrescriptionDetails)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PD_Med");

            entity.HasOne(d => d.Prescription).WithMany(p => p.PrescriptionDetails)
                .HasForeignKey(d => d.PrescriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PD_Pre");
        });

        modelBuilder.Entity<RecordService>(entity =>
        {
            entity.HasKey(e => e.RsId).HasName("PK__RecordSe__65965B2D18EE98C7");

            entity.Property(e => e.RsId).HasColumnName("RS_ID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.ExecuteDate).HasColumnType("datetime");
            entity.Property(e => e.RecordId).HasColumnName("RecordID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Record).WithMany(p => p.RecordServices)
                .HasForeignKey(d => d.RecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RS_Rec");

            entity.HasOne(d => d.Service).WithMany(p => p.RecordServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RS_Ser");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.ToTable("RoomTypes");

            entity.HasKey(e => e.RoomTypeId).HasName("PK_RoomTypes");

            entity.Property(e => e.RoomTypeId).HasColumnName("RoomTypeID");
            entity.Property(e => e.RoomTypeName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<DeptType>(entity =>
        {
            entity.ToTable("DeptTypes");

            entity.HasKey(e => e.DeptTypeId).HasName("PK_DeptTypes");

            entity.Property(e => e.DeptTypeId).HasColumnName("DeptTypeID");
            entity.Property(e => e.DeptTypeName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__32863919809AC254");

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.DeptId).HasColumnName("DeptID");
            entity.Property(e => e.RoomTypeId).HasColumnName("RoomTypeID");
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Dept).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.DeptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_Dept");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.RoomTypeId)
                .HasConstraintName("FK_Room_RoomType");
        });

        modelBuilder.Entity<WebMenu>(entity =>
        {
            entity.HasKey(e => e.MenuId).HasName("PK__WebMenus__C99ED250A14441C8");

            entity.Property(e => e.MenuId).HasColumnName("MenuID");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.IconClass)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MenuName).HasMaxLength(100);
            entity.Property(e => e.MenuUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("MenuURL");
            entity.Property(e => e.ParentId).HasColumnName("ParentID");
            entity.Property(e => e.RoleRequired).HasMaxLength(50);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_Menu_Parent");
        });

        modelBuilder.Entity<MedicalSupply>(entity =>
        {
            entity.HasKey(e => e.SupplyId).HasName("PK__MedicalS__SupplyID");

            entity.ToTable("MedicalSupplies");

            entity.Property(e => e.SupplyId).HasColumnName("SupplyID");
            entity.Property(e => e.SupplyName).HasMaxLength(200);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 0)");
            entity.Property(e => e.Supplier).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
