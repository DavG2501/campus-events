using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Services;

public static class EventWriteLock
{
	public static Task<Event?> LoadAsync(ApplicationDbContext context, int eventId)
	{
		return context.Events.FromSqlInterpolated($@"
            SELECT * FROM [Events] WITH (UPDLOCK, HOLDLOCK)
            WHERE [Id] = {eventId}").SingleOrDefaultAsync();
	}
}