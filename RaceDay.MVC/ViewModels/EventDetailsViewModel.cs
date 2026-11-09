namespace RaceDay.MVC.ViewModels
{
    public class EventDetailsViewModel
    {
        public EventViewModel Event { get; set; } = new();

        public List<CategoryViewModel> Categories { get; set; } = new();
    }
}