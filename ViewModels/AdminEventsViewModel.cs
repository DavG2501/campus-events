using CampusEvents.Models;

namespace CampusEvents.ViewModels;

public class AdminEventsViewModel
{
	public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();
	public int TotalCount { get; set; }
	public int CurrentPage { get; set; } = 1;
	public int PageSize { get; set; } = 10;
	public int TotalPages { get; set; }
	public IReadOnlyList<int> PageSizeOptions { get; set; } = new[] { 10, 25, 50 };
}