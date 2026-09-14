using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Domain
{
    [Table("Users")]
    public class User
    {
        [Key] public int UserId { get; set; }
        [Required, MaxLength(20)]  public string SystemId     { get; set; }
        [Required, MaxLength(150)] public string FullName     { get; set; }
        [Required, MaxLength(200)] public string Email        { get; set; }
        [Required, MaxLength(256)] public string PasswordHash { get; set; }
        [Required, MaxLength(20)]  public string Role         { get; set; }
        public bool      IsActive     { get; set; } = true;
        public DateTime  CreatedAt    { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt  { get; set; }

        public virtual Admin   Admin   { get; set; }
        public virtual Teacher Teacher { get; set; }
        public virtual Student Student { get; set; }
    }

    [Table("Admins")]
    public class Admin
    {
        [Key] public int AdminId { get; set; }
        public int UserId { get; set; }
        [Required, MaxLength(30)] public string AdminType { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal AuthLimit { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual User User { get; set; }
        public virtual ICollection<Expense> Expenses { get; set; }
    }

    [Table("Teachers")]
    public class Teacher
    {
        [Key] public int TeacherId { get; set; }
        public int UserId { get; set; }
        [MaxLength(200)] public string Qualifications  { get; set; }
        [MaxLength(500)] public string ShortBio        { get; set; }
        [MaxLength(20)]  public string ContactNumber   { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal CommissionRate { get; set; } = 20.00m;
        [Required, MaxLength(20)] public string PayoutFrequency { get; set; } = "Monthly";
        [MaxLength(100)] public string BankAccount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual User User { get; set; }
        public virtual ICollection<Class>         Classes { get; set; }
        public virtual ICollection<TeacherPayout> Payouts { get; set; }
    }

    [Table("Students")]
    public class Student
    {
        [Key] public int StudentId { get; set; }
        public int UserId { get; set; }
        [MaxLength(300)] public string Address       { get; set; }
        [MaxLength(20)]  public string GuardianPhone { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.Now;

        public virtual User User { get; set; }
        public virtual ICollection<Enrollment>    Enrollments    { get; set; }
        public virtual ICollection<FeeCollection> FeeCollections { get; set; }
    }

    [Table("Subjects")]
    public class Subject
    {
        [Key] public int SubjectId { get; set; }
        [Required, MaxLength(100)] public string SubjectName { get; set; }
        [MaxLength(50)] public string Level { get; set; }

        public virtual ICollection<Class> Classes { get; set; }
    }

    [Table("Classes")]
    public class Class
    {
        [Key] public int ClassId { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        [Required, MaxLength(150)] public string ClassName { get; set; }
        [MaxLength(50)] public string DayOfWeek { get; set; }
        [MaxLength(50)] public string Room      { get; set; }
        public TimeSpan StartTime  { get; set; }
        public TimeSpan EndTime    { get; set; }
        [Column(TypeName = "decimal(10,2)")] public decimal MonthlyFee  { get; set; } = 0;
        public int  MaxCapacity { get; set; } = 50;
        public bool IsActive    { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Teacher Subject2  { get; set; }
        public virtual Teacher Teacher   { get; set; }
        public virtual Subject Subject   { get; set; }
        public virtual ICollection<Enrollment>    Enrollments    { get; set; }
        public virtual ICollection<FeeCollection> FeeCollections { get; set; }
    }

    [Table("Enrollments")]
    public class Enrollment
    {
        [Key] public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public int ClassId   { get; set; }
        public DateTime EnrolledDate { get; set; } = DateTime.Today;
        public bool IsActive { get; set; } = true;

        public virtual Student Student { get; set; }
        public virtual Class   Class   { get; set; }
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; }
    }

    [Table("AttendanceRecords")]
    public class AttendanceRecord
    {
        [Key] public int AttendanceId { get; set; }
        public int EnrollmentId { get; set; }
        public DateTime ClassDate { get; set; }
        [Required, MaxLength(10)] public string Status   { get; set; }
        [MaxLength(50)]           public string MarkedBy { get; set; }
        public DateTime MarkedAt { get; set; } = DateTime.Now;

        public virtual Enrollment Enrollment { get; set; }
    }

    [Table("FeeCollections")]
    public class FeeCollection
    {
        [Key] public int FeeId { get; set; }
        public int StudentId { get; set; }
        public int ClassId   { get; set; }
        [Required, MaxLength(30)] public string ReceiptNo { get; set; }
        [Column(TypeName = "decimal(10,2)")] public decimal Amount { get; set; }
        [Required, MaxLength(10)] public string Status { get; set; } = "Pending";
        public DateTime  PeriodMonth { get; set; }
        public DateTime? PaidAt      { get; set; }
        [MaxLength(50)] public string CollectedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Student Student { get; set; }
        public virtual Class   Class   { get; set; }
    }

    [Table("Expenses")]
    public class Expense
    {
        [Key] public int ExpenseId { get; set; }
        public int AdminId { get; set; }
        [Required, MaxLength(300)] public string Description { get; set; }
        [Column(TypeName = "decimal(10,2)")] public decimal Amount { get; set; }
        [Required, MaxLength(50)] public string Category    { get; set; }
        [MaxLength(500)]          public string ReceiptPath { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.Now;

        public virtual Admin Admin { get; set; }
    }

    [Table("TeacherPayouts")]
    public class TeacherPayout
    {
        [Key] public int PayoutId { get; set; }
        public int TeacherId { get; set; }
        public DateTime PeriodMonth { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal GrossCollection   { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal FacilityDeduction { get; set; }
        [Column(TypeName = "decimal(12,2)")] public decimal NetPayout         { get; set; }
        [Required, MaxLength(20)] public string Status      { get; set; } = "Pending";
        [MaxLength(50)]           public string ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public virtual Teacher Teacher { get; set; }
    }
}
