using System.ComponentModel;
using System.Runtime.CompilerServices;
using FinalProject.Models;
using FinalProject.Services;

namespace FinalProject.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DataService _dataService;
    private Pet _pet;

    public MainViewModel()
    {
        _dataService = new DataService();
        _pet = new Pet();
        
        LoadPetData();
    }

    
    public Pet Pet
    {
        get => _pet;
        set
        {
            _pet = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PatiCoinsText));
            OnPropertyChanged(nameof(CurrentAnimation));
        }
    }

    
    public string PatiCoinsText => $"🐾 {Pet.PatiCoins} Pati";
    
    
    public string CurrentAnimation => Pet.CurrentAnimation;

    
    private async void LoadPetData()
    {
        Pet = await _dataService.LoadPetAsync();
    }

    
    public async Task SavePetAsync()
    {
        await _dataService.SavePetAsync(Pet);
    }

    
    public void ChangeAnimation(string animationFile)
    {
        Pet.CurrentAnimation = animationFile;
        OnPropertyChanged(nameof(CurrentAnimation));
        _ = SavePetAsync(); 
    }

    
    public async Task AddCoinsAsync(int minutes)
    {
        int coins = minutes switch
        {
            10 => 20,
            20 => 30,
            30 => 40,
            40 => 50,
            50 => 60,
            60 => 80,
            _ => 0
        };

        Pet.PatiCoins += coins;
        Pet.TotalFocusMinutes += minutes;
        
        OnPropertyChanged(nameof(PatiCoinsText));
        
        await SavePetAsync();
    }

    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}