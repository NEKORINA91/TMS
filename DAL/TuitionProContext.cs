using Microsoft.EntityFrameworkCore;
using TMS.Models.Domain;

namespace TMS.DAL
{
    public class TuitionProContext : DbContext
    {
        public TuitionProContext(DbContextOptions<TuitionProContext> options) : base(options) { }

        public DbSet<User>             Users             { get; set; }
        public DbSet<Admin>            Admins            { get; set; }
        public DbSet<Teacher>          Teachers          { get; set; }
        public DbSet<Student>          Students          { get; set; }
        public DbSet<Subject>          Subjects          { get; set; }
        public DbSet<Class>            Classes           { get; set; }
        public DbSet<Enrollment>       Enrollments       { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<FeeCollection>    FeeCollections    { get; set; }
        public DbSet<Expense>          Expenses          { get; set; }
        public DbSet<TeacherPayout>    TeacherPayouts    { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── User unique indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.SystemId).IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            // ── User → Admin (one-to-one)
            modelBuilder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.UserId);

            // ── User → Teacher (one-to-one)
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId);

            // ── User → Student (one-to-one)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId);

            // ── Class relationships
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Teacher)
                .WithMany(t => t.Classes)
                .HasForeignKey(c => c.TeacherId);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.Subject)
                .WithMany(s => s.Classes)
                .HasForeignKey(c => c.SubjectId);

            // ── Enrollment unique (StudentId + ClassId)
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.ClassId }).IsUnique();

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Class)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.ClassId);

            // ── Attendance unique (EnrollmentId + ClassDate)
            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(a => new { a.EnrollmentId, a.ClassDate }).IsUnique();

            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(a => a.Enrollment)
                .WithMany(e => e.AttendanceRecords)
                .HasForeignKey(a => a.EnrollmentId);

            // ── FeeCollection
            modelBuilder.Entity<FeeCollection>()
                .HasIndex(f => f.ReceiptNo).IsUnique();

            modelBuilder.Entity<FeeCollection>()
                .HasOne(f => f.Student)
                .WithMany(s => s.FeeCollections)
                .HasForeignKey(f => f.StudentId);

            modelBuilder.Entity<FeeCollection>()
                .HasOne(f => f.Class)
                .WithMany(c => c.FeeCollections)
                .HasForeignKey(f => f.ClassId);

            // ── Expense
            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Admin)
                .WithMany(a => a.Expenses)
                .HasForeignKey(e => e.AdminId);

            // ── TeacherPayout unique (TeacherId + PeriodMonth)
            modelBuilder.Entity<TeacherPayout>()
                .HasIndex(p => new { p.TeacherId, p.PeriodMonth }).IsUnique();

            modelBuilder.Entity<TeacherPayout>()
                .HasOne(p => p.Teacher)
                .WithMany(t => t.Payouts)
                .HasForeignKey(p => p.TeacherId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
