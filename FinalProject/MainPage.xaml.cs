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
}