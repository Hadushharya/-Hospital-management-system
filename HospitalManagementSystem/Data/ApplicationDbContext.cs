using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<HospitalSettings> HospitalSettings => Set<HospitalSettings>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Triage> Triages => Set<Triage>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<LabRequest> LabRequests => Set<LabRequest>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicalGood> MedicalGoods => Set<MedicalGood>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<FeeSetting> FeeSettings => Set<FeeSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---- Patient / Visit ----
        builder.Entity<Visit>()
            .HasOne(v => v.Patient)
            .WithMany(p => p.Visits)
            .HasForeignKey(v => v.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Triage: one-to-one with Visit ----
        builder.Entity<Triage>()
            .HasOne(t => t.Visit)
            .WithOne(v => v.Triage)
            .HasForeignKey<Triage>(t => t.VisitId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Invoice ----
        builder.Entity<Invoice>()
            .HasOne(i => i.Visit)
            .WithMany(v => v.Invoices)
            .HasForeignKey(i => i.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.CollectedByUser)
            .WithMany()
            .HasForeignKey(i => i.CollectedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .Property(i => i.Amount)
            .HasPrecision(10, 2);

        builder.Entity<Invoice>()
            .HasOne(i => i.FeeSetting)
            .WithMany()
            .HasForeignKey(i => i.FeeSettingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FeeSetting>()
            .Property(f => f.Amount)
            .HasPrecision(10, 2);

        // ---- Consultation ----
        builder.Entity<Consultation>()
            .HasOne(c => c.Visit)
            .WithMany(v => v.Consultations)
            .HasForeignKey(c => c.VisitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Consultation>()
            .HasOne(c => c.Doctor)
            .WithMany()
            .HasForeignKey(c => c.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- LabRequest: sent in parallel to Consultation (clinical) and Invoice (billing) ----
        builder.Entity<LabRequest>()
            .HasOne(lr => lr.Consultation)
            .WithMany(c => c.LabRequests)
            .HasForeignKey(lr => lr.ConsultationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LabRequest>()
            .HasOne(lr => lr.Invoice)
            .WithOne(i => i.LabRequest)
            .HasForeignKey<LabRequest>(lr => lr.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- LabResult: one-to-one with LabRequest ----
        builder.Entity<LabResult>()
            .HasOne(lres => lres.LabRequest)
            .WithOne(lr => lr.LabResult)
            .HasForeignKey<LabResult>(lres => lres.LabRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<LabResult>()
            .HasOne(lres => lres.Technician)
            .WithMany()
            .HasForeignKey(lres => lres.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Prescription ----
        builder.Entity<Prescription>()
            .HasOne(p => p.Consultation)
            .WithMany(c => c.Prescriptions)
            .HasForeignKey(p => p.ConsultationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PrescriptionItem>()
            .HasOne(pi => pi.Prescription)
            .WithMany(p => p.PrescriptionItems)
            .HasForeignKey(pi => pi.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PrescriptionItem>()
            .HasOne(pi => pi.Medicine)
            .WithMany(m => m.PrescriptionItems)
            .HasForeignKey(pi => pi.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Payment: one-to-one with Prescription (Cashier collects product fees) ----
        builder.Entity<Payment>()
            .HasOne(pay => pay.Prescription)
            .WithOne(p => p.Payment)
            .HasForeignKey<Payment>(pay => pay.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasOne(pay => pay.Cashier)
            .WithMany()
            .HasForeignKey(pay => pay.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .Property(pay => pay.Amount)
            .HasPrecision(10, 2);

        // ---- Medicine ----
        builder.Entity<Medicine>()
            .HasOne(m => m.Supplier)
            .WithMany(s => s.Medicines)
            .HasForeignKey(m => m.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Medicine>()
            .HasOne(m => m.RegisteredByUser)
            .WithMany()
            .HasForeignKey(m => m.RegisteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Medicine>().Property(m => m.CostPrice).HasPrecision(10, 2);
        builder.Entity<Medicine>().Property(m => m.SellingPrice).HasPrecision(10, 2);

        // ---- MedicalGood ----
        builder.Entity<MedicalGood>()
            .HasOne(mg => mg.Supplier)
            .WithMany(s => s.MedicalGoods)
            .HasForeignKey(mg => mg.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<MedicalGood>()
            .HasOne(mg => mg.RegisteredByUser)
            .WithMany()
            .HasForeignKey(mg => mg.RegisteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MedicalGood>().Property(mg => mg.CostPrice).HasPrecision(10, 2);
        builder.Entity<MedicalGood>().Property(mg => mg.SellingPrice).HasPrecision(10, 2);

        // ---- StockTransaction ----
        builder.Entity<StockTransaction>()
            .HasOne(st => st.Staff)
            .WithMany()
            .HasForeignKey(st => st.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // Speeds up "current stock" lookups that filter by item type + item id
        builder.Entity<StockTransaction>()
            .HasIndex(st => new { st.ItemType, st.ItemId });
    }
}
