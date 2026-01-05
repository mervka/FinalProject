using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using FinalProject.Models;
using FinalProject.Services;
using SkiaSharp.Extended.UI.Controls;
using Microsoft.Maui.ApplicationModel;


namespace FinalProject.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private const int StatMax = 100;
    private const int StatMin = 0;
    private const int StatDecayAmount = 1;
    private const int OfflineDecayChunk = 10;
    private static readonly TimeSpan StatDecayInterval = TimeSpan.FromMinutes(10); //DAKİKAYI SUNUMDAN ÖNCE DEĞİŞTİR!!!
    private static readonly TimeSpan OfflineDecayInterval = TimeSpan.FromHours(2);
    
    // -------------------------
    // 1) Services + State
    // -------------------------
    private readonly DataService _dataService = new();

    private Pet _pet = new();
    private bool _isInitialized;
    private CancellationTokenSource? _statDecayCts;


    private CancellationTokenSource? _cts;
    private TimeSpan _remainingTime = TimeSpan.Zero;
    private bool _isSessionRunning;
    private bool _isBreakSession;
    private int _selectedMinutes = 25;
    private bool _isDurationPickerVisible;
    private bool _isShopVisible;
    //private bool _isCalendarVisible; //takvim sonra eklenecek
    private bool _isPurchaseToastVisible;
    private string _purchaseToastTitle = string.Empty;
    private string _purchaseToastDetail = string.Empty;
    

    // -------------------------
    // 2) Commands (UI bunlari cagirir)
    // -------------------------
    public ICommand ChangeAnimationCommand { get; }
    public ICommand SelectDurationCommand { get; }
    public ICommand StartSessionCommand { get; }
    public ICommand CancelSessionCommand { get; }
    public ICommand ShowShopCommand { get; }
    public ICommand HideShopCommand { get; }
    public ICommand BuyItemCommand { get; }
    //public ICommand ShowCalendarCommand { get; }
    //public ICommand HideCalendarCommand { get; }
    
    public bool IsDurationPickerVisible
    {
        get => _isDurationPickerVisible;
        set
        {
            if (_isDurationPickerVisible == value) return; _isDurationPickerVisible = value; OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsBottomBarVisible));
        }
    }
    
    public bool IsShopVisible
    {
        get => _isShopVisible;
        set
        {
            if (_isShopVisible == value) return;
            _isShopVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBottomBarVisible));
        }
    }
   
   // public bool IsCalendarVisible
  //  {
   //     get => _isCalendarVisible;
    //    set
      //  {
        //    if (_isCalendarVisible == value) return;
          //  _isCalendarVisible = value;
            //OnPropertyChanged();
            //OnPropertyChanged(nameof(IsBottomBarVisible));
       // }
   // }
    

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
            IsShopVisible = false;
            //IsCalendarVisible = false;
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
        
        ShowShopCommand = new Command(() =>
        {
            if (IsSessionRunning) return;
            IsShopVisible = true;
        });

        HideShopCommand = new Command(() =>
        {
            IsShopVisible = false;
        });

        BuyItemCommand = new Command<ShopItem>(async item =>
        {
            if (item == null) return;
            await PurchaseItemAsync(item);
        });
        
       // ShowCalendarCommand = new Command(() =>
        //{
          //  if (IsSessionRunning) return;
            //IsShopVisible = false;
            //IsDurationPickerVisible = false;
            //IsCalendarVisible = true;
        //});

        //HideCalendarCommand = new Command(() =>
        //{
          //  IsCalendarVisible = false;
        //});

        //HideCalendarCommand = new Command(() =>
        //{
          //  IsCalendarVisible = false;
       // });
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
    
    public bool IsPurchaseToastVisible
    {
        get => _isPurchaseToastVisible;
        private set
        {
            if (_isPurchaseToastVisible == value) return;
            _isPurchaseToastVisible = value;
            OnPropertyChanged();
        }
    }

    public string PurchaseToastTitle
    {
        get => _purchaseToastTitle;
        private set
        {
            if (_purchaseToastTitle == value) return;
            _purchaseToastTitle = value;
            OnPropertyChanged();
        }
    }

    public string PurchaseToastDetail
    {
        get => _purchaseToastDetail;
        private set
        {
            if (_purchaseToastDetail == value) return;
            _purchaseToastDetail = value;
            OnPropertyChanged();
        }
    }
    
    public int Hunger => Pet.Hunger;
    public int Happiness => Pet.Happiness;
    public int Health => Pet.Health;

    public double HungerProgress => Hunger / (double)StatMax;
    public double HappinessProgress => Happiness / (double)StatMax;
    public double HealthProgress => Health / (double)StatMax;
    
    public ObservableCollection<ShopItem> ShopItems { get; } = new();
    public ObservableCollection<ShopCategory> ShopCategories { get; } = new();
    public ObservableCollection<ShopItem> VisibleShopItems { get; } = new();
   // public ObservableCollection<DailyFocusSummary> FocusSummaries { get; } = new();


    private ShopCategory? _selectedShopCategory;
    public ShopCategory? SelectedShopCategory
    {
        get => _selectedShopCategory;
        set
        {
            if (_selectedShopCategory == value) return;
            _selectedShopCategory = value;
            OnPropertyChanged();
            UpdateVisibleShopItems();
        }
    }
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
    public bool IsBottomBarVisible => !IsSessionRunning && !IsDurationPickerVisible && !IsShopVisible;
    //public bool IsBottomBarVisible => !IsSessionRunning && !IsDurationPickerVisible;


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
            ApplyOfflineStatDecay();
            EnsureShopCatalog();
            //UpdateFocusSummaries(); 
            StartStatDecayLoop();

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel.InitializeAsync] ERROR: {ex}");
            _isInitialized = false;
            throw;
        }
    }

    // -------------------------
    // 5) Public Actions (App buradan cagirsin)
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
        var coins = Math.Max(0, minutes);
        
        //int coins = minutes switch
        //{

            //10 => 20,
            //20 => 30,
            //30 => 40,
            //40 => 50,
            //50 => 60,
            //60 => 80,
            //_ => 0
        //};

        Pet.PatiCoins += coins;
        Pet.TotalFocusMinutes += minutes;
        Pet.FocusSessions.Add(new FocusSession
        {
            DurationMinutes = minutes,
            CoinsEarned = coins,
            StartTime = DateTime.Now,
            EndTime = DateTime.Now,
            IsCompleted = true
        });

        OnPropertyChanged(nameof(PatiCoinsText));
        //UpdateFocusSummaries();
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
    
      private void EnsureShopCatalog()
    {
        if (ShopItems.Count > 0)
        {
            return;
        }

        ShopCategories.Add(new ShopCategory { Id = "food", Name = "Mama" });
        ShopCategories.Add(new ShopCategory { Id = "toy", Name = "Oyuncak" });
        ShopCategories.Add(new ShopCategory { Id = "health", Name = "Sağlık" });
        ShopCategories.Add(new ShopCategory { Id = "furniture", Name = "Ev Eşyası" });

        ShopItems.Add(new ShopItem
        {
            Id = "food.basic",
            Name = "Tavuklu Mama",
            Description = "Tokluk +10",
            Price = 10,
            Icon = "🍗",
            Type = ItemType.Food,
            CategoryId = "food",
            PlaceInRoom = false,
            HungerEffect = 10
        });

        ShopItems.Add(new ShopItem
        {
            Id = "food.beef",
            Name = "Biftekli Mama",
            Description = "Tokluk +12",
            Price = 14,
            Icon = "🥩",
            Type = ItemType.Food,
            CategoryId = "food",
            PlaceInRoom = false,
            HungerEffect = 12
        });

        ShopItems.Add(new ShopItem
        {
            Id = "food.salmon",
            Name = "Somonlu Mama",
            Description = "Tokluk +14",
            Price = 16,
            Icon = "🐟",
            Type = ItemType.Food,
            CategoryId = "food",
            PlaceInRoom = false,
            HungerEffect = 14
        });

        ShopItems.Add(new ShopItem
        {
            Id = "food.treats",
            Name = "Ödül Maması",
            Description = "Tokluk +6",
            Price = 8,
            Icon = "🍪",
            Type = ItemType.Food,
            CategoryId = "food",
            PlaceInRoom = false,
            HungerEffect = 6
        });

        ShopItems.Add(new ShopItem
        {
            Id = "toy.scratch",
            Name = "Tırmalama Tahtası",
            Description = "Mutluluk +10",
            Price = 18,
            Icon = "🪵",
            Type = ItemType.Toy,
            CategoryId = "toy",
            PlaceInRoom = true,
            HappinessEffect = 10
        });

        ShopItems.Add(new ShopItem
        {
            Id = "toy.ball",
            Name = "Top",
            Description = "Mutluluk +8",
            Price = 12,
            Icon = "🎾",
            Type = ItemType.Toy,
            CategoryId = "toy",
            PlaceInRoom = true,
            HappinessEffect = 8
        });

        ShopItems.Add(new ShopItem
        {
            Id = "toy.rope",
            Name = "İp Oyuncağı",
            Description = "Mutluluk +7",
            Price = 11,
            Icon = "🧶",
            Type = ItemType.Toy,
            CategoryId = "toy",
            PlaceInRoom = true,
            HappinessEffect = 7
        });

        ShopItems.Add(new ShopItem
        {
            Id = "health.parasite",
            Name = "Parazit Aşısı",
            Description = "Sağlık +12",
            Price = 16,
            Icon = "💉",
            Type = ItemType.Health,
            CategoryId = "health",
            PlaceInRoom = false,
            HealthEffect = 12
        });

        ShopItems.Add(new ShopItem
        {
            Id = "health.rabies",
            Name = "Kuduz Aşısı",
            Description = "Sağlık +14",
            Price = 18,
            Icon = "🩺",
            Type = ItemType.Health,
            CategoryId = "health",
            PlaceInRoom = false,
            HealthEffect = 14
        });

        ShopItems.Add(new ShopItem
        {
            Id = "health.combo",
            Name = "Karma Aşı",
            Description = "Sağlık +16",
            Price = 20,
            Icon = "🧪",
            Type = ItemType.Health,
            CategoryId = "health",
            PlaceInRoom = false,
            HealthEffect = 16
        });

        ShopItems.Add(new ShopItem
        {
            Id = "health.neuter",
            Name = "Kısırlaştırma",
            Description = "Sağlık +20",
            Price = 25,
            Icon = "🏥",
            Type = ItemType.Health,
            CategoryId = "health",
            PlaceInRoom = false,
            HealthEffect = 20
        });

        ShopItems.Add(new ShopItem
        {
            Id = "furniture.bed",
            Name = "Yatak",
            Description = "Odaya yerleşir",
            Price = 20,
            Icon = "🛏️",
            Type = ItemType.Furniture,
            CategoryId = "furniture",
            PlaceInRoom = true
        });

        ShopItems.Add(new ShopItem
        {
            Id = "furniture.bowl",
            Name = "Mama Kabı",
            Description = "Odaya yerleşir",
            Price = 14,
            Icon = "🥣",
            Type = ItemType.Furniture,
            CategoryId = "furniture",
            PlaceInRoom = true
        });

        ShopItems.Add(new ShopItem
        {
            Id = "furniture.chair",
            Name = "Kedi Koltuğu",
            Description = "Odaya yerleşir",
            Price = 22,
            Icon = "🪑",
            Type = ItemType.Furniture,
            CategoryId = "furniture",
            PlaceInRoom = true
        });

        ShopItems.Add(new ShopItem
        {
            Id = "furniture.rug",
            Name = "Halı",
            Description = "Odaya yerleşir",
            Price = 18,
            Icon = "🧺",
            Type = ItemType.Furniture,
            CategoryId = "furniture",
            PlaceInRoom = true
        });

        SelectedShopCategory = ShopCategories.FirstOrDefault();
        UpdateVisibleShopItems();
    }
      
    private void UpdateCategorySelection()
    {
        foreach (var category in ShopCategories)
        {
            category.IsSelected = category == _selectedShopCategory;
        }
    }


    private void UpdateVisibleShopItems()
    {
        VisibleShopItems.Clear();

        if (SelectedShopCategory == null)
        {
            return;
        }

        foreach (var item in ShopItems.Where(item => item.CategoryId == SelectedShopCategory.Id))
        {
            VisibleShopItems.Add(item);
        }
    }
    
   // private void UpdateFocusSummaries()
    //{
     //   FocusSummaries.Clear();

      //  var summaries = Pet.FocusSessions
        //    .GroupBy(session => session.StartTime.Date)
          //  .OrderByDescending(group => group.Key)
            //.Select(group => new DailyFocusSummary
           // {
             //   Date = group.Key,
               // TotalMinutes = group.Sum(item => item.DurationMinutes)
           // });

        //foreach (var summary in summaries)
        //{
          //  FocusSummaries.Add(summary);
        //}
    //}

    private async Task PurchaseItemAsync(ShopItem item)
    {
        if (Pet.PatiCoins < item.Price)
        {
            return;
        }

        Pet.PatiCoins -= item.Price;
        Pet.OwnedItemIds.Add(item.Id);

        if (item.PlaceInRoom)
        {
            var placement = new RoomItem
            {
                ItemId = item.Id,
                Asset = item.Icon,
                X = 0.2 + (Pet.RoomItems.Count % 3) * 0.2,
                Y = 0.75
            };
            Pet.RoomItems.Add(placement);
        }

        var didChange = false;
        var petHunger = Pet.Hunger;
        var petHappiness = Pet.Happiness;
        var petHealth = Pet.Health;

        didChange |= AdjustStat(ref petHunger, item.HungerEffect);
        didChange |= AdjustStat(ref petHappiness, item.HappinessEffect);
        didChange |= AdjustStat(ref petHealth, item.HealthEffect);

        if (didChange)
        {
            Pet.Hunger = petHunger;
            Pet.Happiness = petHappiness;
            Pet.Health = petHealth;
        }

        OnPropertyChanged(nameof(PatiCoinsText));
        if (didChange)
        {
            OnPetStatsChanged();
        }

        await SavePetAsync();
    }
    
    private async Task ShowPurchaseToastAsync(ShopItem item)
    {
        var statMessage = item.Type switch
        {
            ItemType.Food when item.HungerEffect > 0 => $"Tokluk +{item.HungerEffect}",
            ItemType.Toy when item.HappinessEffect > 0 => $"Mutluluk +{item.HappinessEffect}",
            ItemType.Health when item.HealthEffect > 0 => $"Sağlık +{item.HealthEffect}",
            _ => item.PlaceInRoom ? "Odaya yerleştirildi" : "Satın alındı"
        };

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PurchaseToastTitle = $"✅ Satın alındı: {item.Name}";
            PurchaseToastDetail = $"💚 {statMessage}";
            IsPurchaseToastVisible = true;
        });

        await Task.Delay(2200);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            IsPurchaseToastVisible = false;
        });
    }

    private void StartStatDecayLoop()
    {
        if (_statDecayCts != null)
        {
            return;
        }

        _statDecayCts = new CancellationTokenSource();
        var token = _statDecayCts.Token;

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(StatDecayInterval);

            while (await timer.WaitForNextTickAsync(token))
            {
                Pet.LastStatUpdateUtc = DateTime.UtcNow;
                var didChange = false;
                var petHunger = Pet.Hunger;
                var petHappiness = Pet.Happiness;
                var petHealth = Pet.Health;

                didChange |= AdjustStat(ref petHunger, -StatDecayAmount);
                didChange |= AdjustStat(ref petHappiness, -StatDecayAmount);
                didChange |= AdjustStat(ref petHealth, -StatDecayAmount);

                if (!didChange)
                {
                    continue;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Pet.Hunger = petHunger;
                    Pet.Happiness = petHappiness;
                    Pet.Health = petHealth;
                    OnPetStatsChanged();
                });

                await SavePetAsync();
            }
        }, token);
    }
    
    private void ApplyOfflineStatDecay()
    {
        if (Pet.LastStatUpdateUtc == default)
        {
            Pet.LastStatUpdateUtc = DateTime.UtcNow;
            return;
        }

        var elapsed = DateTime.UtcNow - Pet.LastStatUpdateUtc;
        if (elapsed <= TimeSpan.Zero)
        {
            Pet.LastStatUpdateUtc = DateTime.UtcNow;
            return;
        }

        var minuteDrops = (int)Math.Floor(elapsed.TotalMinutes);
        var chunkDrops = (int)Math.Floor(elapsed.TotalHours / OfflineDecayInterval.TotalHours);
        var totalDrop = minuteDrops * StatDecayAmount + chunkDrops * OfflineDecayChunk;

        if (totalDrop <= 0)
        {
            Pet.LastStatUpdateUtc = DateTime.UtcNow;
            return;
        }

        var didChange = false;
        var petHunger = Pet.Hunger;
        var petHappiness = Pet.Happiness;
        var petHealth = Pet.Health;

        didChange |= AdjustStat(ref petHunger, -totalDrop);
        didChange |= AdjustStat(ref petHappiness, -totalDrop);
        didChange |= AdjustStat(ref petHealth, -totalDrop);

        if (didChange)
        {
            Pet.Hunger = petHunger;
            Pet.Happiness = petHappiness;
            Pet.Health = petHealth;
            OnPetStatsChanged();
        }

        Pet.LastStatUpdateUtc = DateTime.UtcNow;
        _ = SavePetAsync();
    }

    private static int ClampStat(int value)
    {
        return Math.Clamp(value, StatMin, StatMax);
    }

    private static bool AdjustStat(ref int stat, int delta)
    {
        var updated = ClampStat(stat + delta);
        if (updated == stat)
        {
            return false;
        }

        stat = updated;
        return true;
    }

    private void OnPetStatsChanged()
    {
        OnPropertyChanged(nameof(Hunger));
        OnPropertyChanged(nameof(Happiness));
        OnPropertyChanged(nameof(Health));
        OnPropertyChanged(nameof(HungerProgress));
        OnPropertyChanged(nameof(HappinessProgress));
        OnPropertyChanged(nameof(HealthProgress));
    }


    private void OnPetUiChanged()
    {
        OnPropertyChanged(nameof(PatiCoinsText));
        OnPropertyChanged(nameof(CurrentAnimation));
        OnPropertyChanged(nameof(LottieSource));
        OnPetStatsChanged();

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
