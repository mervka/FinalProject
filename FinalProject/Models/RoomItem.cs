namespace FinalProject.Models;


public class RoomItem
{
    public string ItemId { get; set; } = "";
    public string Asset { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Scale { get; set; } = 1;
}