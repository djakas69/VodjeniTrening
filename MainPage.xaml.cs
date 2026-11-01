using VodjeniTrening.ViewModels;
using VodjeniTrening.Services;

namespace VodjeniTrening;

public partial class MainPage : ContentPage
{
    private readonly WorkoutViewModel _viewModel;
    private readonly IWorkoutService _workoutService;
    private bool _feedbackDialogOpen;

    public MainPage(WorkoutViewModel viewModel, IWorkoutService workoutService)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _workoutService = workoutService;
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

            if (comment is null)
                return;
            if (string.IsNullOrWhiteSpace(comment))
            {
                await DisplayAlertAsync("Komentar", "Unesite komentar ili izaberite Otkaži.", "U redu");
                return;
            }

            await _workoutService.SubmitFeedbackAsync(comment);
            _viewModel.MarkFeedbackSubmitted();
            await DisplayAlertAsync("Hvala", "Komentar je uspešno poslat.", "U redu");
        }
        catch (WorkoutDataException exception)
        {
            await DisplayAlertAsync("Komentar nije poslat", exception.Message, "U redu");
        }
        finally
        {
            _feedbackDialogOpen = false;
        }
    }
}
