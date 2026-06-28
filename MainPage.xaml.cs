using Microsoft.Maui.Controls;

namespace DaerahRawanBanjir;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = EntryUser.Text?.Trim() ?? string.Empty;
        string password = EntryPass.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert(
                "Peringatan",
                "Username dan password tidak boleh kosong.",
                "OK"
            );

            return;
        }

        UserModel? user = await AppDatabase.LoginAsync(username, password);

        if (user == null)
        {
            await DisplayAlert(
                "Login Gagal",
                "Username atau password salah.",
                "OK"
            );

            return;
        }

        UserSession.LoadUser(user);

        await Navigation.PushAsync(new FaceVerificationPage());
    }

    private async void OnGoToRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}