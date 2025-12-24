namespace FinalProject.Models;

public class Pet
{
    public string Name { get; set; } = "Cat";
    public int Hunger { get; set; } = 100;
    public int Happiness { get; set; } = 100;
    public int Health { get; set; } = 100;
    public int PatiCoins { get; set; } = 0;
    public int TotalFocusMinutes { get; set; } = 0;
    public string CurrentAnimation { get; set; } = "hi_cat.json";
    
    public int Level => TotalFocusMinutes switch
    {
        < 500 => 1,
        < 2000 => 2,
        _ => 3
    };
}
