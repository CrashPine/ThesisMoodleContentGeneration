namespace Backend.DAL.Entities;

public class Test
{
    public Guid Id { get; set; }
    public string RawGeneratedText { get; set; } = string.Empty; // Обычный текст генерации
    public string MoodleXmlContent { get; set; } = string.Empty; // Сгенерированный XML
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
    public User? User { get; set; }
}