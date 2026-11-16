using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class AdminUsersController : Controller
{
	private readonly UserManager<ApplicationUser> _userManager;

	public AdminUsersController(UserManager<ApplicationUser> userManager)
	{
		_userManager = userManager;
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		var users = await _userManager.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync();
		var organizers = await _userManager.GetUsersInRoleAsync(RoleNames.Organizer);
		var organizerIds = organizers.Select(user => user.Id).ToHashSet();

		var model = users.Select(user => new AdminUserViewModel
		{
			Id = user.Id,
			DisplayName = user.DisplayName,
			Email = user.Email ?? string.Empty,
			IsOrganizer = organizerIds.Contains(user.Id)
		}).ToList();

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> GrantOrganizer(string id)
	{
		var user = await _userManager.FindByIdAsync(id);
		if (user is null) return NotFound();

		if (await _userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			TempData["ErrorMessage"] = "This user already has Organizer access.";
			return RedirectToAction(nameof(Index));
		}

		var result = await _userManager.AddToRoleAsync(user, RoleNames.Organizer);
		if (!result.Succeeded)
		{
			TempData["ErrorMessage"] = "Organizer access could not be granted.";
			return RedirectToAction(nameof(Index));
		}

		TempData["StatusMessage"] = $"Organizer access was granted to {user.DisplayName}.";
		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> RemoveOrganizer(string id)
	{
		var user = await _userManager.FindByIdAsync(id);
		if (user is null) return NotFound();

		if (!await _userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			TempData["ErrorMessage"] = "This user does not currently have Organizer access.";
			return RedirectToAction(nameof(Index));
		}

		var result = await _userManager.RemoveFromRoleAsync(user, RoleNames.Organizer);
		if (!result.Succeeded)
		{
			TempData["ErrorMessage"] = "Organizer access could not be removed.";
			return RedirectToAction(nameof(Index));
		}

		TempData["StatusMessage"] = $"Organizer access was removed from {user.DisplayName}.";
		return RedirectToAction(nameof(Index));
	}
}