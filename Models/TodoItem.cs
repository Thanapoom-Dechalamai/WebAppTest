using System.ComponentModel.DataAnnotations;

namespace TodoListApp.Models
{
    public class TodoItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่องาน")]
        [StringLength(200, ErrorMessage = "ชื่องานต้องไม่เกิน 200 ตัวอักษร")]
        [Display(Name = "ชื่องาน")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "รายละเอียดต้องไม่เกิน 500 ตัวอักษร")]
        [Display(Name = "รายละเอียด")]
        public string? Description { get; set; }

        [Display(Name = "เสร็จแล้ว")]
        public bool IsCompleted { get; set; }

        [Display(Name = "วันที่สร้าง")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "กำหนดส่ง")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }
    }
}
