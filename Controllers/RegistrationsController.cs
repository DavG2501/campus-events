using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

[Authorize]
public class RegistrationsController : Controller
{
	private readonly ApplicationDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly RegistrationService _registrationService;
	private readonly TimeProvider _timeProvider;

	public RegistrationsController(
		ApplicationDbContext context,
		UserManager<ApplicationUser> userManager,
		RegistrationService registrationService,
		TimeProvider timeProvider)
	{
		_context = context;
		_userManager = userManager;
		_registrationService = registrationService;
		_timeProvider = timeProvider;
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(int id)
	{
		var currentUser = await _userManager.GetUserAsync(User);
		if (currentUser is null) return Challenge();

		var result = await _registrationService.TryRegisterAsync(currentUser.Id, id);

		switch (result)
		{
			case RegistrationAttemptStatus.Success:
				TempData["StatusMessage"] = "Registration confirmed.";
				break;
			case RegistrationAttemptStatus.Duplicate:
				TempData["ErrorMessage"] = "You are already registered for this Event.";
				break;
			case RegistrationAttemptStatus.Full:
				TempData["ErrorMessage"] = "No places are currently available.";
				break;
			case RegistrationAttemptStatus.Unavailable:
				TempData["ErrorMessage"] = "Registration is unavailable for this Event.";
				return RedirectToAction("Index", "Events");
			case RegistrationAttemptStatus.EventNotFound:
				return NotFound();
		}

		return RedirectToAction("Details", "Events", new { id });
	}

	[HttpGet]
	public async Task<IActionResult> Mine()
	{
		var currentUser = await _userManager.GetUserAsync(User);
		if (currentUser is null) return Challenge();

		var registrations = await _context.Registrations
			.AsNoTracking()
			.Include(registration => registration.Event)
			.ThenInclude(eventItem => eventItem.Category)
			.Where(registration => registration.UserId == currentUser.Id)
			.OrderBy(registration => registration.Event.StartDateTime)
			.ToListAsync();

		return View(registrations);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Cancel(int id)
	{
		var currentUser = await _userManager.GetUserAsync(User);
		if (currentUser is null) return Challenge();

		var target = await _context.Registrations.AsNoTracking()
			.Where(item => item.Id == id && item.UserId == currentUser.Id)
			.Select(item => new { item.EventId })
			.SingleOrDefaultAsync();
		if (target is null) return NotFound();

		await using var transaction = await _context.Database.BeginTransactionAsync();
		var eventItem = await EventWriteLock.LoadAsync(_context, target.EventId);

		if (eventItem is null)
		{
			await transaction.RollbackAsync();
			return NotFound();
		}

		var registration = await _context.Registrations.SingleOrDefaultAsync(item =>
			item.Id == id && item.UserId == currentUser.Id && item.EventId == eventItem.Id);

		if (registration is null)
		{
			await transaction.RollbackAsync();
			return NotFound();
		}

		if (eventItem.StartDateTime <= _timeProvider.GetLocalNow().DateTime)
		{
			await transaction.RollbackAsync();
			TempData["ErrorMessage"] = "A Registration can be cancelled only before the Event begins.";
			return RedirectToAction(nameof(Mine));
		}

		_context.Registrations.Remove(registration);
		await _context.SaveChangesAsync();
		await transaction.CommitAsync();

		TempData["StatusMessage"] = "Your Registration was cancelled and the place is available again.";
		return RedirectToAction(nameof(Mine));
	}
}