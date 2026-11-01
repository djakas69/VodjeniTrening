using VodjeniTrening.Services;

namespace VodjeniTrening;

public partial class LoginPage : ContentPage
{
    private readonly IAuthenticationService _authenticationService;
    private readonly MainPage _mainPage;
    private readonly RegisterPage _registerPage;

    public LoginPage(
        IAuthenticationService authenticationService,
        MainPage mainPage,
        RegisterPage registerPage)
    {
        InitializeComponent();
        _authenticationService = authenticationService;
        _mainPage = mainPage;
        _registerPage = registerPage;
    }

    internal void ShowAccountCreated(string userName)
    {
        UserNameEntry.Text = userName;
        PasswordEntry.Text = string.Empty;
        ShowMessage("Nalog je kreiran. Sada se možete ulogovati.", isError: false);
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var userName = UserNameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            ShowMessage("Unesite korisničko ime i lozinku.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            await _authenticationService.LoginAsync(userName, password);
            PasswordEntry.Text = string.Empty;
            await Navigation.PushAsync(_mainPage);
        }
        catch (AuthenticationException exception)
        {
            ShowMessage(exception.Message, isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        MessageLabel.IsVisible = false;
        await Navigation.PushAsync(_registerPage);
    }

    private void SetBusy(bool isBusy)
    {
        LoginButton.IsEnabled = !isBusy;
        UserNameEntry.IsEnabled = !isBusy;
        PasswordEntry.IsEnabled = !isBusy;
        BusyIndicator.IsVisible = isBusy;
        BusyIndicator.IsRunning = isBusy;
    }

    private void ShowMessage(string message, bool isError)
    {
        MessageLabel.Text = message;
        MessageLabel.TextColor = isError
            ? Color.FromArgb("#B91C1C")
            : Color.FromArgb("#15803D");
        MessageLabel.IsVisible = true;
    }
}

