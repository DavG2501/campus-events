using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using CampusEvents.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class AdminController : Controller
{
	private const int DefaultPageSize = 10;
	private static readonly int[] AllowedPageSizes = { 10, 25, 50 };
	private readonly ApplicationDbContext _context;
	private readonly TimeProvider _timeProvider;

	public AdminController(ApplicationDbContext context, TimeProvider timeProvider)
	{
		_context = context;
		_timeProvider = timeProvider;
	}

	[HttpGet]
	public IActionResult Index() => View();

	[HttpGet]
	public async Task<IActionResult> Events(int? page, int? pageSize)
	{
		if (Request.Query.ContainsKey(nameof(pageSize)) &&
			(!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
		{
			Response.StatusCode = StatusCodes.Status400BadRequest;
			return View("BadRequest", new ErrorPageViewModel
			{
				Title = "Invalid Page Size",
				Message = "Choose a page size of 10, 25, or 50.",
				ReturnUrl = Url.Action(nameof(Events), "Admin"),
				ReturnLabel = "Return to Administrator Event Oversight"
			});
		}

		var selectedPageSize = pageSize.GetValueOrDefault(DefaultPageSize);
		var query = _context.Events.AsNoTracking();
		var totalCount = await query.CountAsync();
		var totalPages = (int)Math.Ceiling(totalCount / (double)selectedPageSize);
		var currentPage = Math.Max(1, page.GetValueOrDefault(1));
		if (totalPages == 0) currentPage = 1;
		else if (currentPage > totalPages) currentPage = totalPages;

		var offset = ((long)currentPage - 1) * selectedPageSize;
		if (offset > int.MaxValue)
		{
			Response.StatusCode = StatusCodes.Status400BadRequest;
			return View("BadRequest", new ErrorPageViewModel
			{
				Title = "Page Range Unavailable",
				Message = "Choose an earlier page in Administrator Event Oversight.",
				ReturnUrl = Url.Action(nameof(Events), "Admin"),
				ReturnLabel = "Return to Administrator Event Oversight"
			});
		}

		var events = await query
			.Include(item => item.Category)
			.Include(item => item.Organizer)
			.OrderBy(item => item.StartDateTime)
			.ThenBy(item => item.Id)
			.Skip((int)offset)
			.Take(selectedPageSize)
			.ToListAsync();

		return View(new AdminEventsViewModel
		{
			Events = events,
			TotalCount = totalCount,
			CurrentPage = currentPage,
			PageSize = selectedPageSize,
			TotalPages = totalPages,
			PageSizeOptions = AllowedPageSizes
		});
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CancelEvent(int id, int? page, int? pageSize)
	{
		var returnPage = Math.Max(1, page.GetValueOrDefault(1));
		var returnPageSize = pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value)
			? pageSize.Value : DefaultPageSize;
		var returnRoute = new { page = returnPage, pageSize = returnPageSize };

		await using var transaction = await _context.Database.BeginTransactionAsync();
		var eventItem = await EventWriteLock.LoadAsync(_context, id);

		if (eventItem is null)
		{
			await transaction.RollbackAsync();
			return NotFound();
		}

		if (eventItem.Status != EventStatus.Published)
		{
			await transaction.RollbackAsync();
			TempData["ErrorMessage"] = "Only a Published Event can be cancelled.";
			return RedirectToAction(nameof(Events), returnRoute);
		}

		if (eventItem.StartDateTime <= _timeProvider.GetLocalNow().DateTime)
		{
			await transaction.RollbackAsync();
			TempData["ErrorMessage"] = "A Published Event can be cancelled only before it starts.";
			return RedirectToAction(nameof(Events), returnRoute);
		}

		eventItem.Status = EventStatus.Cancelled;
		await _context.SaveChangesAsync();
		await transaction.CommitAsync();
		TempData["StatusMessage"] = $"{eventItem.Title} was cancelled by an Administrator.";
		return RedirectToAction(nameof(Events), returnRoute);
	}
}