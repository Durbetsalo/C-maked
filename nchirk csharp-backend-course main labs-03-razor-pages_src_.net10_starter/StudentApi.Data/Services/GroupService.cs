using Microsoft.EntityFrameworkCore;
using StudentApi.Data.Data;
using StudentApi.Data.DTOs.Group;
using StudentApi.Data.DTOs.Student;
using StudentApi.Data.Entities;

namespace StudentApi.Data.Services;

public class GroupService : IGroupService
{
    private readonly AppDbContext context;

    public GroupService(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<GroupGetDto>> GetAllAsync(string? search)
    {
        var groupsQuery = context.Groups
            .Include(g => g.Students)
            .Include(g => g.StudyProgram)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            groupsQuery = groupsQuery.Where(g => g.Name.ToLower().Contains(search.ToLower()));
        }

        return await groupsQuery.Select(g => new GroupGetDto
        {
            Id = g.Id,
            Name = g.Name,
            StudyProgramName = g.StudyProgram == null ? null : g.StudyProgram.Name,
            Students = g.Students.Select(s => new StudentShortDto
            {
                Id = s.Id,
                Name = s.Name
            }).ToList()
        }).ToListAsync();
    }

    public async Task<GroupGetDto?> GetByIdAsync(int id)
    {
        return await context.Groups
            .Include(g => g.Students)
            .Include(g => g.StudyProgram)
            .Select(g => new GroupGetDto
            {
                Id = g.Id,
                Name = g.Name,
                StudyProgramName = g.StudyProgram == null ? null : g.StudyProgram.Name,
                Students = g.Students.Select(s => new StudentShortDto
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList()
            })
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<GroupUpdateDto?> GetByIdForUpdateAsync(int id)
    {
        return await context.Groups
            .Where(g => g.Id == id)
            .Select(g => new GroupUpdateDto
            {
                Name = g.Name,
                StudyProgramId = g.StudyProgramId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GroupGetDto> CreateAsync(GroupCreateDto dto)
    {
        var newGroup = new Group
        {
            Name = dto.Name,
            StudyProgramId = dto.StudyProgramId
        };

        await context.Groups.AddAsync(newGroup);
        await context.SaveChangesAsync();

        var result = await GetByIdAsync(newGroup.Id);
        return result!;
    }

    public async Task<bool> UpdateAsync(int id, GroupUpdateDto dto)
    {
        var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == id);
        if (group == null) return false;

        group.Name = dto.Name;
        group.StudyProgramId = dto.StudyProgramId;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == id);
        if (group == null) return false;

        context.Groups.Remove(group);
        await context.SaveChangesAsync();
        return true;
    }
}