using Microsoft.Maui.Controls;
using SkiaSharp;

namespace DaerahRawanBanjir;

public partial class FaceVerificationPage : ContentPage
{
    private const double MinimumSimilarity = 65.0;

    public FaceVerificationPage()
    {
        InitializeComponent();

        string source = string.IsNullOrWhiteSpace(UserSession.RegisteredFaceSource)
            ? "belum diketahui"
            : UserSession.RegisteredFaceSource;

        LabelRegisteredInfo.Text = $"Data wajah referensi: {source}";
    }

    private async void OnVerifyWithCameraClicked(object sender, EventArgs e)
    {
        try
        {
            if (UserSession.RegisteredFace == null)
            {
                await DisplayAlert(
                    "Data Wajah Tidak Ada",
                    "Data wajah referensi belum tersedia. Silakan registrasi ulang.",
                    "OK"
                );

                return;
            }

            if (!Microsoft.Maui.Media.MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert(
                    "Kamera Tidak Tersedia",
                    "Perangkat ini tidak mendukung kamera.",
                    "OK"
                );

                return;
            }

            LabelVerificationResult.Text = "Status: membuka kamera...";
            LabelVerificationResult.TextColor = Color.FromArgb("#FFD868");

            Microsoft.Maui.Storage.FileResult? photo =
                await Microsoft.Maui.Media.MediaPicker.Default.CapturePhotoAsync(
                    new Microsoft.Maui.Media.MediaPickerOptions
                    {
                        Title = "Ambil Foto Verifikasi Wajah"
                    }
                );

            if (photo == null)
            {
                LabelVerificationResult.Text = "Status: verifikasi dibatalkan.";
                LabelVerificationResult.TextColor = Color.FromArgb("#FFD868");
                return;
            }

            LabelVerificationResult.Text = "Status: memproses wajah dengan OpenCV...";
            LabelVerificationResult.TextColor = Color.FromArgb("#FFD868");

            SKBitmap? currentFace = await FaceService.CreateTemplateFromFileAsync(photo);

            if (currentFace == null)
            {
                LabelVerificationResult.Text = "Status: wajah tidak terdeteksi.";
                LabelVerificationResult.TextColor = Color.FromArgb("#FF806E");

                await DisplayAlert(
                    "Wajah Tidak Terdeteksi",
                    "OpenCV tidak berhasil mendeteksi wajah pada foto. Pastikan wajah menghadap kamera, pencahayaan cukup, dan tidak terlalu jauh.",
                    "OK"
                );

                return;
            }

            double similarity = FaceService.CalculateSimilarity(
                UserSession.RegisteredFace,
                currentFace
            );

            currentFace.Dispose();

            if (similarity >= MinimumSimilarity)
            {
                LabelVerificationResult.Text = $"Status: wajah cocok ({similarity:F1}%).";
                LabelVerificationResult.TextColor = Color.FromArgb("#39F1D0");

                if (UserSession.IsMasyarakat)
                {
                    await AppDatabase.SimpanRiwayatAksesMasyarakatAsync();
                }

                await DisplayAlert(
                    "Verifikasi Berhasil",
                    $"Wajah berhasil diverifikasi.\nTingkat kemiripan: {similarity:F1}%",
                    "OK"
                );

                Application.Current!.MainPage =
                    new NavigationPage(new MainDashboardPage());
            }
            else
            {
                LabelVerificationResult.Text = $"Status: wajah tidak cocok ({similarity:F1}%).";
                LabelVerificationResult.TextColor = Color.FromArgb("#FF806E");

                await DisplayAlert(
                    "Verifikasi Gagal",
                    $"Wajah tidak cocok dengan data registrasi.\nTingkat kemiripan: {similarity:F1}%",
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            LabelVerificationResult.Text = "Status: terjadi kesalahan.";
            LabelVerificationResult.TextColor = Color.FromArgb("#FF806E");

            await DisplayAlert(
                "Error",
                $"Terjadi kesalahan saat verifikasi wajah: {ex.Message}",
                "OK"
            );
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}