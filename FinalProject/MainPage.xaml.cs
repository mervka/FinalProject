using FinalProject.ViewModels;

namespace FinalProject;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();
        
        _viewModel = new MainViewModel();
        BindingContext = _viewModel;
    }
    
    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        // App odağı kaybedince -> cancel gibi davran
        _viewModel.CancelSession();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Window != null)
            Window.Deactivated += OnWindowDeactivated;

        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage.OnAppearing] ERROR: {ex}");
            await DisplayAlert("Error", "Data could not be loaded.", "OK");
        }
    }
    
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is FinalProject.ViewModels.MainViewModel vm)
            vm.CancelSession();
    }





}

