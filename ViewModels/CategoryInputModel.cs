using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class CategoryInputModel
{
	[Required]
	[StringLength(100)]
	[Display(Name = "Category name")]
	public string Name { get; set; } = string.Empty;
}