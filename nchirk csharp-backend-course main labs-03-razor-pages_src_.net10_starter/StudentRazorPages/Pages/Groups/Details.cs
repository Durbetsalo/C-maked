using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentApi.Data.DTOs.Group;
using StudentApi.Data.Services;

namespace StudentRazorPages.Pages.Groups;

public class Details : PageModel
{
    private readonly IGroupService _groupService;

    public Details(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public GroupGetDto Group { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group is null)
        {
            return NotFound();
        }

        Group = group;
        return Page();
    }
}