using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;

	public EventsController(ILogger<EventsController> logger)
	{
		_logger = logger;
	}

	public IActionResult Index()
	{
		_logger.LogInformation("Handling request for the CEMS Events page.");

		var featuredEventTitle = "Library Research Night";

		return View(model: featuredEventTitle);
	}

}
