using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentApi.Data.DTOs.Group;
using StudentApi.Data.Services;

namespace StudentRazorPages.Pages.Groups;

public class Create : PageModel
{
    private readonly IGroupService _groupService;
    private readonly IStudyProgramService _studyProgramService;

    public Create(IGroupService groupService, IStudyProgramService studyProgramService)
    {
        _groupService = groupService;
        _studyProgramService = studyProgramService;
    }

    [BindProperty]
    public GroupCreateDto Input { get; set; } = new();

    public IEnumerable<SelectListItem> StudyProgramList { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadStudyProgramsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadStudyProgramsAsync();
            return Page();
        }

        await _groupService.CreateAsync(Input);
        return RedirectToPage("./Index");
    }

    private async Task LoadStudyProgramsAsync()
    {
        var programs = await _studyProgramService.GetAllAsync(null);
        StudyProgramList = programs.Select(x => new SelectListItem(x.Name, x.Id.ToString()));
    }
}