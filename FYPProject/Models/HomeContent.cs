using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FYPProject.Models
{
    public class HomeContent
    {
        [Key]
        public int Id { get; set; } // ✅ Primary Key

        [Required]
        [StringLength(255)]
        public string Url { get; set; } // ✅ Image/Content URL

        [Required]
        [StringLength(500)]
        public string Description { get; set; } // ✅ Content Description

        public byte[]? Photo_Data { get; set; }

    }
}
