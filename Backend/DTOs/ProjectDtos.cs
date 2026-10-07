using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateProjectDto
{
    [Required(ErrorMessage = "项目名称是必需的")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}

public class UpdateProjectDto
{
    [Required(ErrorMessage = "项目名称是必需的")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int QueueCount { get; set; }
}

