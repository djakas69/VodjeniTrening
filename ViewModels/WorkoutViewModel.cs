using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VodjeniTrening.Models;
using VodjeniTrening.Services;

namespace VodjeniTrening.ViewModels;

public sealed class WorkoutViewModel : INotifyPropertyChanged
{
    private enum SessionState { Loading, Ready, Exercise, Rest, Completed, Error }
    private enum PendingTransition { None, NextExercise, NextRound, NextBlock }

    private readonly IWorkoutService _workoutService;
    private readonly SemaphoreSlim _transitionGate = new(1, 1);
    private readonly AsyncCommand _primaryCommand;
    private readonly AsyncCommand _pauseCommand;
    private readonly AsyncCommand _skipCommand;
    private readonly AsyncCommand _restartCommand;
    private Workout? _workout;
    private CancellationTokenSource? _countdownCancellation;
    private Task? _countdownTask;
    private SessionState _state = SessionState.Loading;
    private PendingTransition _pendingTransition;
    private int _blockIndex;
    private int _roundIndex;
    private int _exerciseIndex;
    private int _remainingSeconds;
    private int _remainingRepetitions;
    private bool _initialized;
    private bool _isPaused;
    private bool _countdownNeedsResume;
    private bool _feedbackSubmitted;
    private string _workoutName = "Učitavanje treninga…";
    private string _blockName = "";
    private string _roundProgress = "";
    private string _exerciseName = "";
    private string _exerciseProgress = "";
    private string _nextExerciseText = "";
    private string _mainValue = "—";
    private string _statusText = "Učitavanje";
    private string _errorMessage = "";

    public WorkoutViewModel(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
        _primaryCommand = new AsyncCommand(HandlePrimaryAsync, CanUsePrimary);
        _pauseCommand = new AsyncCommand(TogglePauseAsync, () => IsPauseEnabled);
        _skipCommand = new AsyncCommand(SkipAsync, () => IsSkipEnabled);
        _restartCommand = new AsyncCommand(RestartAsync, () => _workout is not null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? FeedbackRequested;

    public string WorkoutName { get => _workoutName; private set => SetField(ref _workoutName, value); }
    public string BlockName { get => _blockName; private set => SetField(ref _blockName, value); }
    public string RoundProgress { get => _roundProgress; private set => SetField(ref _roundProgress, value); }
    public string ExerciseName { get => _exerciseName; private set => SetField(ref _exerciseName, value); }
    public string ExerciseProgress { get => _exerciseProgress; private set => SetField(ref _exerciseProgress, value); }
    public string NextExerciseText { get => _nextExerciseText; private set => SetField(ref _nextExerciseText, value); }
    public string MainValue { get => _mainValue; private set => SetField(ref _mainValue, value); }
    public string StatusText { get => _statusText; private set => SetField(ref _statusText, value); }
    public string ErrorMessage { get => _errorMessage; private set { if (SetField(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string PrimaryButtonText => _state switch
    {
        SessionState.Ready => "Start",
        SessionState.Exercise => "Vežba u toku",
        SessionState.Rest => "Preskoči pauzu",
        SessionState.Completed => _feedbackSubmitted ? "Komentar je poslat" : "Kraj treninga",
        SessionState.Error => "Pokušaj ponovo",
        _ => "Sačekajte"
    };
    public string PauseButtonText => _isPaused ? "Nastavi" : "Pauziraj";
    public bool IsPauseEnabled =>
        (_state == SessionState.Rest && _remainingSeconds > 0) ||
        (_state == SessionState.Exercise && (_remainingSeconds > 0 || _remainingRepetitions > 0));
    public bool IsSkipEnabled => _state is SessionState.Exercise or SessionState.Rest;
    public ICommand PrimaryCommand => _primaryCommand;
    public ICommand PauseCommand => _pauseCommand;
    public ICommand SkipCommand => _skipCommand;
    public ICommand RestartCommand => _restartCommand;

    private WorkoutBlock? CurrentBlock => _workout?.Blocks.ElementAtOrDefault(_blockIndex);
    private Exercise? CurrentExercise => CurrentBlock?.Exercises.ElementAtOrDefault(_exerciseIndex);

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        await LoadWorkoutAsync();
    }

    private async Task LoadWorkoutAsync()
    {
        SetState(SessionState.Loading);
        WorkoutName = "Učitavanje treninga…";
        StatusText = "Povezivanje sa serverom";
        MainValue = "—";
        ErrorMessage = "";

        try
        {
            _workout = await _workoutService.LoadAsync();
            WorkoutName = _workout.Name;
            ResetToReady();
        }
        catch (WorkoutDataException exception)
        {
            SetState(SessionState.Error);
            WorkoutName = "Vođeni trening";
            StatusText = "Greška pri učitavanju";
            ErrorMessage = exception.Message;
            MainValue = "!";
        }
    }

    public void Suspend()
    {
        if (!IsPauseEnabled || _isPaused)
            return;

        _isPaused = true;
        _countdownNeedsResume = true;
        CancelCountdown();
        NotifyControls();
    }

    private bool CanUsePrimary() => _state switch
    {
        SessionState.Ready or SessionState.Rest or SessionState.Error => true,
        SessionState.Completed => !_feedbackSubmitted,
        _ => false
    };

    private Task HandlePrimaryAsync() => ExecuteSerializedAsync(async () =>
    {
        switch (_state)
        {
            case SessionState.Ready:
                await StartExerciseAsync();
                break;
            case SessionState.Exercise:
                await FinishExerciseAsync();
                break;
            case SessionState.Rest:
                await FinishRestAsync();
                break;
            case SessionState.Completed:
                FeedbackRequested?.Invoke(this, EventArgs.Empty);
                break;
            case SessionState.Error:
                await LoadWorkoutAsync();
                break;
        }
    });

    private Task SkipAsync() => ExecuteSerializedAsync(async () =>
    {
        if (_state == SessionState.Exercise)
            await FinishExerciseAsync();
        else if (_state == SessionState.Rest)
            await FinishRestAsync();
    });

    private Task RestartAsync() => ExecuteSerializedAsync(RestartCoreAsync);

    private async Task TogglePauseAsync()
    {
        if (!IsPauseEnabled)
            return;

        _isPaused = !_isPaused;
        if (!_isPaused && _countdownNeedsResume)
        {
            _countdownNeedsResume = false;
            if (_state == SessionState.Exercise && CurrentExercise?.IsTimed == false)
                StartRepetitionCountdown(_remainingRepetitions, CurrentExercise.RepetitionDurationSeconds);
            else
                StartCountdown(_remainingSeconds);
        }

        NotifyControls();
        await Task.CompletedTask;
    }

    private async Task ExecuteSerializedAsync(Func<Task> action)
    {
        if (!await _transitionGate.WaitAsync(0))
            return;

        try
        {
            await action();
        }
        finally
        {
            _transitionGate.Release();
        }
    }

    private void ResetToReady()
    {
        CancelCountdown();
        _blockIndex = 0;
        _roundIndex = 0;
        _exerciseIndex = 0;
        _pendingTransition = PendingTransition.None;
        _isPaused = false;
        _countdownNeedsResume = false;
        _feedbackSubmitted = false;
        _remainingSeconds = 0;
        _remainingRepetitions = 0;
        ErrorMessage = "";
        UpdatePositionText();
        MainValue = "—";
        StatusText = "Spremno";
        SetState(SessionState.Ready);
    }

    private Task RestartCoreAsync()
    {
        ResetToReady();
        return Task.CompletedTask;
    }

    private Task StartExerciseAsync()
    {
        CancelCountdown();
        _isPaused = false;
        _countdownNeedsResume = false;
        _pendingTransition = PendingTransition.None;
        UpdatePositionText();
        StatusText = "Vežba";
        SetState(SessionState.Exercise);

        var exercise = CurrentExercise!;
        if (exercise.IsTimed)
        {
            _remainingRepetitions = 0;
            MainValue = FormatTime(exercise.DurationSeconds);
            StartCountdown(exercise.DurationSeconds);
        }
        else
        {
            _remainingSeconds = 0;
            _remainingRepetitions = exercise.RepetitionCount;
            MainValue = $"{exercise.RepetitionCount}×";
            StartRepetitionCountdown(exercise.RepetitionCount, exercise.RepetitionDurationSeconds);
        }

        return Task.CompletedTask;
    }

    private async Task FinishExerciseAsync()
    {
        var block = CurrentBlock!;
        var exercise = CurrentExercise!;
        CancelCountdown();

        int restSeconds;
        if (_exerciseIndex + 1 < block.Exercises.Count)
        {
            _pendingTransition = PendingTransition.NextExercise;
            restSeconds = exercise.RestAfterExerciseSeconds;
            StatusText = "Pauza između vežbi";
        }
        else if (_roundIndex + 1 < block.RoundCount)
        {
            _pendingTransition = PendingTransition.NextRound;
            restSeconds = block.RestBetweenRoundsSeconds;
            StatusText = "Pauza između krugova";
        }
        else if (_blockIndex + 1 < _workout!.Blocks.Count)
        {
            _pendingTransition = PendingTransition.NextBlock;
            restSeconds = block.RestAfterBlockSeconds;
            StatusText = "Pauza između blokova";
        }
        else
        {
            CompleteWorkout();
            return;
        }

        if (restSeconds <= 0)
        {
            await ApplyPendingTransitionAsync();
            return;
        }

        ExerciseName = StatusText;
        ExerciseProgress = "";
        SetState(SessionState.Rest);
        MainValue = FormatTime(restSeconds);
        StartCountdown(restSeconds);
    }

    private async Task FinishRestAsync()
    {
        CancelCountdown();
        await ApplyPendingTransitionAsync();
    }

    private async Task ApplyPendingTransitionAsync()
    {
        switch (_pendingTransition)
        {
            case PendingTransition.NextExercise:
                _exerciseIndex++;
                break;
            case PendingTransition.NextRound:
                _roundIndex++;
                _exerciseIndex = 0;
                break;
            case PendingTransition.NextBlock:
                _blockIndex++;
                _roundIndex = 0;
                _exerciseIndex = 0;
                break;
            default:
                return;
        }

        _pendingTransition = PendingTransition.None;
        await StartExerciseAsync();
    }

    private void CompleteWorkout()
    {
        CancelCountdown();
        _remainingSeconds = 0;
        _remainingRepetitions = 0;
        _pendingTransition = PendingTransition.None;
        _isPaused = false;
        StatusText = "Trening završen";
        BlockName = "Bravo!";
        RoundProgress = "";
        ExerciseName = "Završili ste sve blokove";
        ExerciseProgress = "";
        NextExerciseText = "";
        MainValue = "✓";
        SetState(SessionState.Completed);
    }

    public void MarkFeedbackSubmitted()
    {
        _feedbackSubmitted = true;
        NotifyControls();
    }

    private void StartCountdown(int seconds)
    {
        CancelCountdown();
        _remainingSeconds = seconds;
        MainValue = FormatTime(seconds);
        _countdownCancellation = new CancellationTokenSource();
        _countdownTask = RunCountdownAsync(_countdownCancellation);
        NotifyControls();
    }

    private void StartRepetitionCountdown(int repetitions, int repetitionDurationSeconds)
    {
        CancelCountdown();
        _remainingSeconds = 0;
        _remainingRepetitions = repetitions;
        MainValue = $"{repetitions}×";
        _countdownCancellation = new CancellationTokenSource();
        _countdownTask = RunRepetitionCountdownAsync(
            repetitionDurationSeconds, _countdownCancellation);
        NotifyControls();
    }

    private async Task RunRepetitionCountdownAsync(
        int repetitionDurationSeconds,
        CancellationTokenSource countdownSource)
    {
        var cancellationToken = countdownSource.Token;

        try
        {
            while (_remainingRepetitions > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(repetitionDurationSeconds), cancellationToken);
                if (_isPaused)
                    continue;

                _remainingRepetitions--;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainValue = $"{_remainingRepetitions}×";
                    NotifyControls();
                });
            }

            cancellationToken.ThrowIfCancellationRequested();
            await CompleteCountdownAsync(countdownSource, SessionState.Exercise);
        }
        catch (OperationCanceledException)
        {
            // Expected when a step is skipped, paused, restarted or the page disappears.
        }
    }

    private async Task RunCountdownAsync(CancellationTokenSource countdownSource)
    {
        var cancellationToken = countdownSource.Token;
        var countdownState = _state;

        try
        {
            while (_remainingSeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                if (_isPaused)
                    continue;

                _remainingSeconds--;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainValue = FormatTime(_remainingSeconds);
                    NotifyControls();
                });
            }

            cancellationToken.ThrowIfCancellationRequested();
            await CompleteCountdownAsync(countdownSource, countdownState);
        }
        catch (OperationCanceledException)
        {
            // Expected when a step is skipped, paused, restarted or the page disappears.
        }
    }

    private Task CompleteCountdownAsync(
        CancellationTokenSource countdownSource,
        SessionState countdownState) =>
        MainThread.InvokeOnMainThreadAsync(() => ExecuteSerializedAsync(async () =>
            {
                if (!ReferenceEquals(_countdownCancellation, countdownSource) || _state != countdownState)
                    return;

                if (countdownState == SessionState.Exercise)
                    await FinishExerciseAsync();
                else
                    await FinishRestAsync();
            }));

    private void CancelCountdown()
    {
        var cancellation = Interlocked.Exchange(ref _countdownCancellation, null);
        if (cancellation is null)
            return;

        cancellation.Cancel();
        cancellation.Dispose();
        _countdownTask = null;
    }

    private void UpdatePositionText()
    {
        var block = CurrentBlock!;
        var exercise = CurrentExercise!;
        BlockName = block.Name;
        RoundProgress = $"Krug {_roundIndex + 1} od {block.RoundCount}";
        ExerciseName = exercise.Name;
        ExerciseProgress = $"Vežba {_exerciseIndex + 1} od {block.Exercises.Count}";
        NextExerciseText = GetNextExerciseText();
    }

    private string GetNextExerciseText()
    {
        if (_workout is null || CurrentBlock is null)
            return "";

        Exercise? nextExercise = null;
        if (_exerciseIndex + 1 < CurrentBlock.Exercises.Count)
            nextExercise = CurrentBlock.Exercises[_exerciseIndex + 1];
        else if (_roundIndex + 1 < CurrentBlock.RoundCount)
            nextExercise = CurrentBlock.Exercises[0];
        else if (_blockIndex + 1 < _workout.Blocks.Count)
            nextExercise = _workout.Blocks[_blockIndex + 1].Exercises[0];

        return nextExercise is null ? "Poslednja vežba" : $"Sledeća vežba: {nextExercise.Name}";
    }

    private void SetState(SessionState state)
    {
        _state = state;
        NotifyControls();
    }

    private void NotifyControls()
    {
        OnPropertyChanged(nameof(PrimaryButtonText));
        OnPropertyChanged(nameof(PauseButtonText));
        OnPropertyChanged(nameof(IsPauseEnabled));
        OnPropertyChanged(nameof(IsSkipEnabled));
        _primaryCommand.RaiseCanExecuteChanged();
        _pauseCommand.RaiseCanExecuteChanged();
        _skipCommand.RaiseCanExecuteChanged();
        _restartCommand.RaiseCanExecuteChanged();
    }

    private static string FormatTime(int totalSeconds) =>
        $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
