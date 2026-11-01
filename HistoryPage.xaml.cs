using System.Collections.ObjectModel;
using VodjeniTrening.Models;
using VodjeniTrening.Services;

namespace VodjeniTrening;

public partial class HistoryPage : ContentPage
{
    private readonly IWorkoutService _workoutService;
    private readonly ObservableCollection<WorkoutHistoryItem> _history = [];
    private bool _isLoading;

    public HistoryPage(IWorkoutService workoutService)
    {
        InitializeComponent();
        _workoutService = workoutService;
        HistoryCollection.ItemsSource = _history;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e) => await LoadHistoryAsync();

    private async Task LoadHistoryAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        ErrorLabel.IsVisible = false;
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        try
        {
            var entries = await _workoutService.LoadHistoryAsync();
            _history.Clear();
            foreach (var entry in entries)
                _history.Add(entry);
        }
        catch (WorkoutDataException exception)
        {
            ErrorLabel.Text = exception.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
            _isLoading = false;
        }
    }
}
