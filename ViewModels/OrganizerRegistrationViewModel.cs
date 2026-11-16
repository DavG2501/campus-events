namespace CampusEvents.ViewModels;

public class OrganizerRegistrationViewModel
{
	public int EventId { get; set; }
	public string EventTitle { get; set; } = string.Empty;
	public int Capacity { get; set; }
	public int CurrentRegistrationCount { get; set; }
	public int RemainingCapacity { get; set; }
	public IReadOnlyList<OrganizerRegistrationParticipantViewModel> Participants { get; set; } = [];
}

public class OrganizerRegistrationParticipantViewModel
{
	public string DisplayName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public DateTime RegisteredAt { get; set; }
}