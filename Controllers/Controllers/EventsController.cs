using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;
	private readonly ApplicationDbContext _context;

	public EventsController(
			ILogger<EventsController> logger,
			ApplicationDbContext context)
	{
		_logger = logger;
		_context = context;
	}

	public async Task<IActionResult> Details(int id)
	{
		var query = _context.Events
			.AsNoTracking()
			.Include(item => item.Category)
			.Include(item => item.Organizer)
			.Where(item =>
				item.Id == id &&
				item.Status == EventStatus.Published);

		string sql = query.ToQueryString();

		var eventItem = await query.SingleOrDefaultAsync();

		if (eventItem is null)
		{
			Response.StatusCode = StatusCodes.Status404NotFound;
			return View("EventNotFound");
		}

		return View(eventItem);
	}

}
