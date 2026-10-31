using VodjeniTrening.ViewModels;

namespace VodjeniTrening;

public partial class MainPage : ContentPage
{
    private readonly WorkoutViewModel _viewModel;

    public MainPage(WorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Suspend();
        base.OnDisappearing();
    }
}
