namespace FinalProject.Models;

public class FocusSession
{
    public int DurationMinutes { get; set; }
    public int CoinsEarned { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsCompleted { get; set; } 
}