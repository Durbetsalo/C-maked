using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentApi.Data.DTOs.Group;
using StudentApi.Data.Services;

namespace StudentRazorPages.Pages.Groups;

public class Index : PageModel
{
    private readonly IGroupService _groupService;

    public Index(IGroupService groupService)
    {
        _groupService = groupService;
    }

    [BindProperty(SupportsGet = true)]
    [StringLength(200, ErrorMessage = "Поисковый запрос не должен превышать 200 символов")]
    public string? SearchTerm { get; set; }

    [TempData]
    public string? DeleteErrorMessage { get; set; }

    public IEnumerable<GroupGetDto> Groups { get; set; } = [];

    public string TotalCountMessage => $"Всего групп в базе: {Groups.Count()}";

    public async Task<IActionResult> OnGetAsync()
    {
        if (!ModelState.IsValid)
        {
            Groups = [];
            return Page();
        }

        Groups = await _groupService.GetAllAsync(SearchTerm);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var success = await _groupService.DeleteAsync(id);
        if (!success)
        {
            DeleteErrorMessage = "Не удалось удалить группу, возможно она уже была удалена.";
        }

        return RedirectToPage();
    }
}