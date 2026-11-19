using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Domain
{
    [Table("CalendarEvents")]
    public class CalendarEvent
    {
        [Key] public int EventId { get; set; }
        [Required, MaxLength(150)] public string Title { get; set; } = "";
        [Column(TypeName = "date")] public DateTime EventDate { get; set; }
        [Required, MaxLength(20)] public string Kind { get; set; } = "Event";
        [MaxLength(300)] public string? Notes { get; set; }
        [MaxLength(50)] public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("ClassCancellations")]
    public class ClassCancellation
    {
        [Key] public int CancellationId { get; set; }
        public int ClassId { get; set; }
        [Column(TypeName = "date")] public DateTime ClassDate { get; set; }
        [MaxLength(200)] public string? Reason { get; set; }
        [MaxLength(50)] public string? CancelledBy { get; set; }
        public DateTime CancelledAt { get; set; } = DateTime.Now;
    }
}