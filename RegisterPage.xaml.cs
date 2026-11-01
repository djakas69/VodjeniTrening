using VodjeniTrening.Services;

namespace VodjeniTrening;

public partial class RegisterPage : ContentPage
{
    private readonly IAuthenticationService _authenticationService;

    public RegisterPage(IAuthenticationService authenticationService)
    {
        InitializeComponent();
        _authenticationService = authenticationService;
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        var userName = UserNameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmation = ConfirmPasswordEntry.Text ?? string.Empty;

        if (userName.Length < 3)
        {
            ShowError("Korisničko ime mora imati najmanje 3 znaka.");
            return;
        }
        if (password.Length < 8)
        {
            ShowError("Lozinka mora imati najmanje 8 znakova.");
            return;
        }
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            ShowError("Unete lozinke se ne podudaraju.");
            return;
        }

        SetBusy(true);
        try
        {
            await _authenticationService.RegisterAsync(userName, password);
            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;

            var loginPage = Navigation.NavigationStack.OfType<LoginPage>().LastOrDefault();
            loginPage?.ShowAccountCreated(userName);
            await Navigation.PopAsync();
        }
        catch (AuthenticationException exception)
        {
            ShowError(exception.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

    private void SetBusy(bool isBusy)
    {
        RegisterButton.IsEnabled = !isBusy;
        UserNameEntry.IsEnabled = !isBusy;
        PasswordEntry.IsEnabled = !isBusy;
        ConfirmPasswordEntry.IsEnabled = !isBusy;
        BusyIndicator.IsVisible = isBusy;
        BusyIndicator.IsRunning = isBusy;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}

