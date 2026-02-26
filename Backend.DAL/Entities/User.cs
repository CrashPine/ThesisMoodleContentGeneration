namespace Backend.DAL.Entities;

public class User
{
    public Guid Id { get; set; }
    public string MoodleUserId { get; set; } = string.Empty; // ID с фронта (Moodle)
    public List<TextSource> TextSources { get; set; } = new();
    public List<Test> Tests { get; set; } = new();
}