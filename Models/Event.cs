using System.ComponentModel.DataAnnotations;

namespace CampusEvents.Models;

public class Event
{
	public int Id { get; set; }

	[Required]
	public string Title { get; set; } = string.Empty;

	[Required]
	public string Description { get; set; } = string.Empty;

	[Required]
	public string Location { get; set; } = string.Empty;

	public DateTime StartDateTime { get; set; }

	public DateTime EndDateTime { get; set; }

	public int Capacity { get; set; }

	public EventStatus Status { get; set; }

	public DateTime CreatedAt { get; set; }

	public int CategoryId { get; set; }

	public Category Category { get; set; } = null!;

	public string OrganizerId { get; set; } = string.Empty;

	public ApplicationUser Organizer { get; set; } = null!;

	public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}