namespace RaceDay.MVC.ViewModels
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal Distance { get; set; }

        public decimal EntryFee { get; set; }

        public int MaximumParticipants { get; set; }
    }
}