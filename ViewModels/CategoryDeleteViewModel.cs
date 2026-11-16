namespace CampusEvents.ViewModels;

public class CategoryDeleteViewModel
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public bool IsInUse { get; set; }
}