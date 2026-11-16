using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class AdminCategoriesController : Controller
{
	private readonly ApplicationDbContext _context;

	public AdminCategoriesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		var categories = await _context.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync();
		return View(categories);
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View(new CategoryInputModel());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(CategoryInputModel model)
	{
		await ValidateUniqueNameAsync(model);
		if (!ModelState.IsValid) return View(model);

		var category = new Category { Name = model.Name.Trim() };
		_context.Categories.Add(category);

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException exception) when (IsUniqueCategoryNameViolation(exception))
		{
			_context.Entry(category).State = EntityState.Detached;
			ModelState.AddModelError(nameof(CategoryInputModel.Name), "A Category with this name already exists.");
			return View(model);
		}

		TempData["StatusMessage"] = $"{category.Name} was created.";
		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(category => category.Id == id);
		if (category is null) return NotFound();
		return View(new CategoryInputModel { Name = category.Name });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, CategoryInputModel model)
	{
		var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == id);
		if (category is null) return NotFound();

		await ValidateUniqueNameAsync(model, excludedCategoryId: category.Id);
		if (!ModelState.IsValid) return View(model);

		category.Name = model.Name.Trim();

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException exception) when (IsUniqueCategoryNameViolation(exception))
		{
			_context.Entry(category).State = EntityState.Detached;
			ModelState.AddModelError(nameof(CategoryInputModel.Name), "A Category with this name already exists.");
			return View(model);
		}

		TempData["StatusMessage"] = $"{category.Name} was updated.";
		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Delete(int id)
	{
		var model = await _context.Categories.AsNoTracking().Where(category => category.Id == id)
			.Select(category => new CategoryDeleteViewModel
			{
				Id = category.Id,
				Name = category.Name,
				IsInUse = category.Events.Any()
			}).SingleOrDefaultAsync();

		if (model is null) return NotFound();
		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteConfirmed(int id)
	{
		var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == id);
		if (category is null) return NotFound();

		var isInUse = await _context.Events.AnyAsync(eventItem => eventItem.CategoryId == category.Id);
		if (isInUse)
		{
			TempData["ErrorMessage"] = "This Category is in use and cannot be deleted.";
			return RedirectToAction(nameof(Index));
		}

		_context.Categories.Remove(category);

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException exception) when (IsEventCategoryReferenceViolation(exception))
		{
			_context.Entry(category).State = EntityState.Detached;
			TempData["ErrorMessage"] = "This Category is in use and cannot be deleted.";
			return RedirectToAction(nameof(Index));
		}

		TempData["StatusMessage"] = $"{category.Name} was deleted.";
		return RedirectToAction(nameof(Index));
	}

	private async Task ValidateUniqueNameAsync(CategoryInputModel model, int? excludedCategoryId = null)
	{
		if (string.IsNullOrWhiteSpace(model.Name)) return;
		var candidateName = model.Name.Trim();

		var duplicateExists = await _context.Categories.AsNoTracking()
			.AnyAsync(category => (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value) &&
				EF.Functions.Collate(category.Name, ApplicationDbContext.CategoryNameCollation) == candidateName);

		if (duplicateExists)
		{
			ModelState.AddModelError(nameof(CategoryInputModel.Name), "A Category with this name already exists.");
		}
	}

	private static bool IsUniqueCategoryNameViolation(DbUpdateException exception)
	{
		return exception.InnerException is SqlException error && (error.Number == 2601 || error.Number == 2627) &&
			error.Message.Contains("IX_Categories_Name", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsEventCategoryReferenceViolation(DbUpdateException exception)
	{
		return exception.InnerException is SqlException error && error.Number == 547 &&
			error.Message.Contains("FK_Events_Categories_CategoryId", StringComparison.OrdinalIgnoreCase);
	}
}