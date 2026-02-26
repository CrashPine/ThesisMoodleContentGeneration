namespace Backend.DAL.Entities;

public class TextSource
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty; // Промежуточный текст OCR
    public Guid UserId { get; set; }
    public User? User { get; set; }
}