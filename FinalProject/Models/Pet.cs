using System.Collections.Generic;

namespace FinalProject.Models;

public class Pet
{
    public string Name { get; set; } = "Cat";
    public int Hunger { get; set; } = 100;
    public int Happiness { get; set; } = 100;
    public int Health { get; set; } = 100;
    public DateTime LastStatUpdateUtc { get; set; } = default;
    public int PatiCoins { get; set; } = 0;
    public int TotalFocusMinutes { get; set; } = 0;
    public string CurrentAnimation { get; set; } = "standing_cat.json";
    public List<string> OwnedItemIds { get; set; } = new();
    //public List<RoomItem> RoomItems { get; set; } = new();
    public List<FocusSession> FocusSessions { get; set; } = new(); //Focus history kayitlari
    
    
    public int Level => TotalFocusMinutes switch
    {
        < 500 => 1,
        < 2000 => 2,
        _ => 3
    };
}
