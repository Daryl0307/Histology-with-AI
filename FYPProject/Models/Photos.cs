namespace FYPProject.Models;


using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;



public class Photos
{
    [Key]
    public int Photo_ID { get; set; }

    [Required]
    [StringLength(225)]
    public string Photo_Description { get; set; }

    [Required]
    [StringLength(225)]
    public string Photo_URL { get; set; }

    [Column("Tissue_ID")] 
    public int? Tissue_ID { get; set; }

    public int? Question_ID { get; set; }
    public string? Photo_Description_Text { get; set; }
    public int? Quiz_ID { get; set; }
    public byte[]? Photo_Data { get; set; }

    [NotMapped]
    public IFormFile? PhotoFile { get; set; }
}
