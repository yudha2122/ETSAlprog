namespace DaerahRawanBanjir;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Paksa aplikasi membuka MainPage (Halaman Login) saat pertama kali dinyalakan
        MainPage = new NavigationPage(new MainPage());
    }
}