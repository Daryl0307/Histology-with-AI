

namespace FYPProject.Model
{
    public class TissueInfo
    {
        public int TissueId { get; set; }
        public string TissueName { get; set; }
        public string TissueDescription { get; set; }

        // Navigation property for related Photos
        public ICollection<Photo> Photos { get; set; }
    }

}