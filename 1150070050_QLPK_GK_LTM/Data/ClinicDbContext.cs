using _1150070050_QLPK_GK_LTM.Models.Entities;
using Microsoft.EntityFrameworkCore;

public partial class tuvyContext : DbContext
{
    public tuvyContext()
    {
    }

    public tuvyContext(DbContextOptions<tuvyContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Patient> Patients { get; set; }
    public virtual DbSet<Doctor> Doctors { get; set; }
    public virtual DbSet<Service> Services { get; set; }
    public virtual DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // EF Scaffold đã generate mapping theo DB thật
        // Ví dụ:
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.Doctor)
                .WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId);

            entity.HasOne(p => p.Patient)
                .WithMany(a => a.Appointments)
                .HasForeignKey(p => p.PatientId);

            entity.HasOne(s => s.Service)
                .WithMany(a => a.Appointments)
                .HasForeignKey(s => s.ServiceId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
