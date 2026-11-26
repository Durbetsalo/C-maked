using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentApi.Data.DTOs.Group;
using StudentApi.Data.Services;

namespace StudentRazorPages.Pages.Groups;

public class Update : PageModel
{
    private readonly IGroupService _groupService;
    private readonly IStudyProgramService _studyProgramService;

    public Update(IGroupService groupService, IStudyProgramService studyProgramService)
    {
        _groupService = groupService;
        _studyProgramService = studyProgramService;
    }

    [BindProperty]
    public GroupUpdateDto Input { get; set; } = new();

    public IEnumerable<SelectListItem> StudyProgramList { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var group = await _groupService.GetByIdForUpdateAsync(id);
        if (group is null)
        {
            return NotFound();
        }

        await LoadStudyProgramsAsync();
        Input = group;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return await PageWithStudyProgramsAsync();
        }

        var updated = await _groupService.UpdateAsync(id, Input);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Не удалось обновить группу, возможно она была удалена.");
            return await PageWithStudyProgramsAsync();
        }

        return RedirectToPage("./Index");
    }

    private async Task<IActionResult> PageWithStudyProgramsAsync()
    {
        await LoadStudyProgramsAsync();
        return Page();
    }

    private async Task LoadStudyProgramsAsync()
    {
        var programs = await _studyProgramService.GetAllAsync(null);
        StudyProgramList = programs.Select(x => new SelectListItem(x.Name, x.Id.ToString()));
    }
}