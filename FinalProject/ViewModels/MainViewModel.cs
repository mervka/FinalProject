using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FinalProject.Models;
using FinalProject.Services;
using SkiaSharp.Extended.UI.Controls;

namespace FinalProject.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    // -------------------------
    // 1) Services + State
    // -------------------------
    private readonly DataService _dataService = new();

    private Pet _pet = new();
    private bool _isInitialized;

    private CancellationTokenSource? _cts;
    private TimeSpan _remainingTime = TimeSpan.Zero;
    private bool _isSessionRunning;
    private bool _isBreakSession;
    private int _selectedMinutes = 25;
    private bool _isDurationPickerVisible;
   

    

    // -------------------------
    // 2) Commands (UI bunlari cagirir)
    // -------------------------
    public ICommand ChangeAnimationCommand { get; }
    public ICommand SelectDurationCommand { get; }
    public ICommand StartSessionCommand { get; }
    public ICommand CancelSessionCommand { get; }
    
    public bool IsDurationPickerVisible
    {
        get => _isDurationPickerVisible;
        set
        {
            if (_isDurationPickerVisible == value) return; _isDurationPickerVisible = value; OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsBottomBarVisible));
        }
    }

    public ICommand ShowDurationPickerCommand { get; }
    public ICommand HideDurationPickerCommand { get; }
    public ICommand StartFromPickerCommand { get; }

    public MainViewModel()
    {
        ChangeAnimationCommand = new Command<string>(file =>
        {
            if (!string.IsNullOrWhiteSpace(file))
                ChangeAnimation(file, persist: true); // kullanici secince kaydet
        });

        SelectDurationCommand = new Command<object>(param =>
        {
            if (IsSessionRunning) return;

            if (param is int i)
                SelectedMinutes = i;
            else if (param != null && int.TryParse(param.ToString(), out var parsed))
                SelectedMinutes = parsed;
        });
        
        ShowDurationPickerCommand = new Command(() =>
        {
            if (IsSessionRunning) return;
            IsDurationPickerVisible = true;
        });

        HideDurationPickerCommand = new Command(() =>
        {
            IsDurationPickerVisible = false;
        });

        StartFromPickerCommand = new Command(async () =>
        {
            IsDurationPickerVisible = false;
            if (!IsSessionRunning)
                await StartSessionAsync();
        });

        StartSessionCommand = new Command(async () =>
        {
            if (IsSessionRunning) return;
            await StartSessionAsync();
        });

        CancelSessionCommand = new Command(() =>
        {
            if (!IsSessionRunning) return;
            CancelSession();
        });
    }

    // -------------------------
    // 3) Bindable Properties (UI buraya bakar)
    // -------------------------
    public Pet Pet
    {
        get => _pet;
        private set
        {
            _pet = value;
            OnPropertyChanged();
            OnPetUiChanged();
        }
    }

    public string PatiCoinsText => $"🐾 {Pet.PatiCoins} Pati";
    public string CurrentAnimation => Pet.CurrentAnimation;

    // SKLottieView Source icin
    public SKLottieImageSource LottieSource =>
        new SKFileLottieImageSource { File = Pet.CurrentAnimation };

    public int SelectedMinutes
    {
        get => _selectedMinutes;
        set
        {
            if (_selectedMinutes == value) return;
            _selectedMinutes = value;
            OnPropertyChanged();
        }
    }

    public bool IsSessionRunning
    {
        get => _isSessionRunning;
        private set
        {
            if (_isSessionRunning == value) return;
            _isSessionRunning = value;
            OnPropertyChanged();
                
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanCancel));
            OnPropertyChanged(nameof(IsBottomBarVisible));
        }
    }

    public bool CanStart => !IsSessionRunning;
    public bool CanCancel => IsSessionRunning;
    
    //focus'da arkadaki buton gizlensin
    public bool IsBottomBarVisible => !IsSessionRunning && !IsDurationPickerVisible;


    public string RemainingTimeText =>
        $"{(int)_remainingTime.TotalMinutes:00}:{_remainingTime.Seconds:00}";

    public string SessionTypeText => _isBreakSession ? "Break" : "Focus";

    // -------------------------
    // 4) Lifecycle (sayfa acilinca veri yukle)
    // -------------------------
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            var loaded = await _dataService.LoadPetAsync();

            if (string.IsNullOrWhiteSpace(loaded.CurrentAnimation))
                loaded.CurrentAnimation = "standing_cat.json";

            Pet = loaded;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel.InitializeAsync] ERROR: {ex}");
            _isInitialized = false;
            throw;
        }
    }

    // -------------------------
    // 5) Public Actions (Page/App buradan cagirsin)
    // -------------------------
    public void CancelSession()
    {
        _cts?.Cancel();

        // UI'yi anında toparla (overlay hemen kapansın)
        IsDurationPickerVisible = false;

        _isBreakSession = false;
        OnPropertyChanged(nameof(SessionTypeText));

        _remainingTime = TimeSpan.Zero;
        OnPropertyChanged(nameof(RemainingTimeText));

        ChangeAnimation("standing_cat.json", persist: false);

        IsSessionRunning = false;
    }

    public void ChangeAnimation(string animationFile, bool persist)
    {
        Pet.CurrentAnimation = animationFile;

        // UI guncelle
        OnPropertyChanged(nameof(CurrentAnimation));
        OnPropertyChanged(nameof(LottieSource));

        // Sadece "kalici" degisimlerde kaydet (kullanici secimi gibi)
        if (persist)
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

    // -------------------------
    // 6) Timer Flow (Pomodoro)
    // -------------------------
    private async Task StartSessionAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        
        IsDurationPickerVisible = false;
        IsSessionRunning = true;

        try
        {
            // Focus
            _isBreakSession = false;
            OnPropertyChanged(nameof(SessionTypeText));

            ChangeAnimation("sleeping_loader_cat.json", persist: false);

            await RunCountdownAsync(TimeSpan.FromMinutes(SelectedMinutes), token);

            await AddCoinsAsync(SelectedMinutes);

            // Break (5 dk)
            _isBreakSession = true;
            OnPropertyChanged(nameof(SessionTypeText));

            ChangeAnimation("cat paw loading.json", persist: false);

            await RunCountdownAsync(TimeSpan.FromMinutes(5), token);
        }
        catch (OperationCanceledException)
        {
            // cancel olunca sadece toparla
            ChangeAnimation("standing_cat.json", persist: false);
        }
        finally
        {
            IsSessionRunning = false;

            _cts?.Dispose();
            _cts = null;

            _remainingTime = TimeSpan.Zero;
            OnPropertyChanged(nameof(RemainingTimeText));

            ChangeAnimation("standing_cat.json", persist: false);
        }
    }

    private async Task RunCountdownAsync(TimeSpan duration, CancellationToken token)
    {
        _remainingTime = duration;
        OnPropertyChanged(nameof(RemainingTimeText));

        while (_remainingTime > TimeSpan.Zero)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(1000, token);

            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
            OnPropertyChanged(nameof(RemainingTimeText));
        }
    }

    // -------------------------
    // 7) Persistence + Helpers
    // -------------------------
    private async Task SavePetAsync()
    {
        await _dataService.SavePetAsync(Pet);
    }

    private void OnPetUiChanged()
    {
        OnPropertyChanged(nameof(PatiCoinsText));
        OnPropertyChanged(nameof(CurrentAnimation));
        OnPropertyChanged(nameof(LottieSource));
    }

    // -------------------------
    // 8) INotifyPropertyChanged
    // -------------------------
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
}
