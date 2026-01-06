namespace FinalProject.Models;

public class FocusSession
{
    public DateTime DateUtc { get; set; }   // gün bilgisi 
    public int TotalMinutes { get; set; }        // o gün eklenen focus dakikası
}