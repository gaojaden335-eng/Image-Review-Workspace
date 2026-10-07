using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Guest"; // "Admin", "User", or "Guest"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Project> CreatedProjects { get; set; } = new List<Project>();
    public ICollection<SelectionRecord> SelectionRecords { get; set; } = new List<SelectionRecord>();
    public ICollection<UserProgress> UserProgresses { get; set; } = new List<UserProgress>();
}

