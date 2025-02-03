

namespace FYPProject.Models
{
    public class TissueInfo
    {
        public int Tissue_ID { get; set; }
        public string Tissue_Name { get; set; }
        public string Tissue_Description { get; set; }

        // Navigation property for related Photos
        public ICollection<Photos> Photos { get; set; }
    }

}