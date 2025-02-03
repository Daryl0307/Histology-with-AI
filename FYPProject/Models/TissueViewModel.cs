namespace FYPProject.Models
{
    public class TissueViewModel
    {
        public int TissueId { get; set; }
        public string TissueName { get; set; }
        public string TissueDescription { get; set; }
        public string PhotoURL { get; set; } // No nested Photo object
    }
}
