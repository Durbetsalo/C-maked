using System.ComponentModel.DataAnnotations;

namespace StudentApi.Data.DTOs.Group;

public class GroupUpdateDto
{
    [Required(ErrorMessage = "Название группы обязательно")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Название должно содержать от 1 до 50 символов")]
    public string Name { get; set; } = string.Empty;

    public int? StudyProgramId { get; set; }
}