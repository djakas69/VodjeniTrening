using VodjeniTrening.ViewModels;
using VodjeniTrening.Services;

namespace VodjeniTrening;

public partial class MainPage : ContentPage
{
    private readonly WorkoutViewModel _viewModel;
    private readonly IWorkoutService _workoutService;
    private readonly HistoryPage _historyPage;
    private bool _feedbackDialogOpen;

    public MainPage(
        WorkoutViewModel viewModel,
        IWorkoutService workoutService,
        HistoryPage historyPage)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _workoutService = workoutService;
        _historyPage = historyPage;
        _viewModel.FeedbackRequested += OnFeedbackRequested;
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

    private async void OnFeedbackRequested(object? sender, EventArgs e)
    {
        if (_feedbackDialogOpen)
            return;

        _feedbackDialogOpen = true;
        try
        {
            var comment = await DisplayPromptAsync(
                "Kraj treninga",
                "Upišite zapažanje o današnjem treningu.",
                accept: "Pošalji",
                cancel: "Otkaži",
                placeholder: "Kako je prošao trening?",
                maxLength: 2000,
                keyboard: Keyboard.Text);

            await _workoutService.SubmitWorkoutResultAsync(
                string.IsNullOrWhiteSpace(comment) ? null : comment,
                _viewModel.CompletedDurationSeconds);
            _viewModel.MarkFeedbackSubmitted();
            if (!string.IsNullOrWhiteSpace(comment))
                await DisplayAlertAsync("Hvala", "Komentar je uspešno poslat.", "U redu");
        }
        catch (WorkoutDataException exception)
        {
            await DisplayAlertAsync("Trening nije sačuvan", exception.Message, "U redu");
        }
        finally
        {
            _feedbackDialogOpen = false;
        }
    }

    private async void OnHistoryClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_historyPage);
}
