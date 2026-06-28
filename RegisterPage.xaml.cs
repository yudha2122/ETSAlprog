using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace DaerahRawanBanjir;

public partial class RegisterPage : ContentPage
{
    private readonly Entry _entryNewUser;
    private readonly Entry _entryNewPass;
    private readonly Picker _pickerRole;
    private readonly Label _labelFaceStatus;

    private SKBitmap? _capturedFace;
    private string _capturedFaceSource = string.Empty;

    public RegisterPage()
    {
        Title = "Registrasi";
        BackgroundColor = Color.FromArgb("#0B2E58");

        _entryNewUser = new Entry
        {
            Placeholder = "Buat Username",
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#87BFE8"),
            BackgroundColor = Color.FromArgb("#0B2E58")
        };

        _entryNewPass = new Entry
        {
            Placeholder = "Buat Password",
            IsPassword = true,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#87BFE8"),
            BackgroundColor = Color.FromArgb("#0B2E58")
        };

        _pickerRole = new Picker
        {
            Title = "Pilih jenis user",
            TextColor = Colors.White,
            TitleColor = Color.FromArgb("#87BFE8"),
            BackgroundColor = Color.FromArgb("#0B2E58"),
            ItemsSource = new List<string>
            {
                UserRoles.Pemerintah,
                UserRoles.Masyarakat
            },
            SelectedItem = UserRoles.Masyarakat
        };

        _labelFaceStatus = new Label
        {
            Text = "Belum ada data wajah. Silakan ambil foto wajah menggunakan kamera.",
            FontSize = 13,
            TextColor = Color.FromArgb("#8DD9FF")
        };

        Button buttonCaptureFace = new Button
        {
            Text = "📷 Ambil Foto Wajah dari Kamera",
            BackgroundColor = Color.FromArgb("#31B86B"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 50
        };
        buttonCaptureFace.Clicked += OnCaptureFaceClicked;

        Button buttonRegister = new Button
        {
            Text = "Simpan & Daftarkan",
            BackgroundColor = Color.FromArgb("#238DF3"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 50
        };
        buttonRegister.Clicked += OnRegisterClicked;

        Button buttonBack = new Button
        {
            Text = "Kembali ke Login",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#8DD9FF")
        };
        buttonBack.Clicked += OnBackToLoginClicked;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(30),
                Spacing = 16,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = "REGISTRASI AKUN",
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = Colors.White
                    },

                    new Label
                    {
                        Text = "Buat akun untuk masuk ke sistem analisis daerah risiko rawan banjir",
                        FontSize = 13,
                        HorizontalTextAlignment = TextAlignment.Center,
                        TextColor = Color.FromArgb("#8DD9FF")
                    },

                    new Frame
                    {
                        Padding = 18,
                        CornerRadius = 12,
                        HasShadow = false,
                        BackgroundColor = Color.FromArgb("#123E70"),
                        BorderColor = Color.FromArgb("#1F6FA8"),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 14,
                            Children =
                            {
                                _entryNewUser,
                                _entryNewPass,

                                new Label
                                {
                                    Text = "Jenis User",
                                    FontAttributes = FontAttributes.Bold,
                                    Margin = new Thickness(0, 8, 0, 0),
                                    TextColor = Color.FromArgb("#D8F3FF")
                                },

                                _pickerRole,

                                new Label
                                {
                                    Text = "Data Wajah",
                                    FontAttributes = FontAttributes.Bold,
                                    Margin = new Thickness(0, 8, 0, 0),
                                    TextColor = Color.FromArgb("#D8F3FF")
                                },

                                _labelFaceStatus,
                                buttonCaptureFace,
                                buttonRegister,
                                buttonBack
                            }
                        }
                    }
                }
            }
        };
    }

    private async void OnCaptureFaceClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!Microsoft.Maui.Media.MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert(
                    "Kamera Tidak Tersedia",
                    "Perangkat ini tidak mendukung penggunaan kamera.",
                    "OK"
                );

                return;
            }

            Microsoft.Maui.Storage.FileResult? photo =
                await Microsoft.Maui.Media.MediaPicker.Default.CapturePhotoAsync(
                    new Microsoft.Maui.Media.MediaPickerOptions
                    {
                        Title = "Ambil Foto Wajah"
                    }
                );

            await SaveFaceTemplateAsync(photo, "kamera");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Kamera Gagal Dibuka",
                $"Kamera tidak bisa digunakan. Detail: {ex.Message}",
                "OK"
            );
        }
    }

    private async Task SaveFaceTemplateAsync(
        Microsoft.Maui.Storage.FileResult? file,
        string source)
    {
        SKBitmap? template = await FaceService.CreateTemplateFromFileAsync(file);

        if (template == null)
        {
            await DisplayAlert(
                "Wajah Tidak Terdeteksi",
                "OpenCV tidak berhasil mendeteksi wajah pada foto. Pastikan wajah menghadap kamera, pencahayaan cukup, dan tidak terlalu jauh.",
                "OK"
            );

            return;
        }

        _capturedFace?.Dispose();
        _capturedFace = template;
        _capturedFaceSource = source;

        _labelFaceStatus.Text = $"Data wajah berhasil diambil dari {source}.";
        _labelFaceStatus.TextColor = Colors.LightGreen;

        await DisplayAlert(
            "Berhasil",
            $"Foto wajah dari {source} berhasil disimpan sebagai template.",
            "OK"
        );
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        string username = _entryNewUser.Text?.Trim() ?? string.Empty;
        string password = _entryNewPass.Text ?? string.Empty;
        string role = _pickerRole.SelectedItem?.ToString() ?? string.Empty;

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

        if (!UserRoles.IsValid(role))
        {
            await DisplayAlert(
                "Jenis User Belum Dipilih",
                "Silakan pilih User Pemerintah atau User Masyarakat.",
                "OK"
            );

            return;
        }

        if (_capturedFace == null)
        {
            await DisplayAlert(
                "Data Wajah Belum Ada",
                "Silakan ambil foto wajah terlebih dahulu.",
                "OK"
            );

            return;
        }

        bool usernameSudahAda = await AppDatabase.UsernameSudahAdaAsync(username);

        if (usernameSudahAda)
        {
            await DisplayAlert(
                "Username Sudah Terdaftar",
                "Silakan gunakan username lain.",
                "OK"
            );

            return;
        }

        byte[] faceBytes = ConvertBitmapToBytes(_capturedFace);

        UserModel user = new UserModel
        {
            Username = username,
            Password = password,
            Role = role,
            FaceTemplate = faceBytes,
            FaceSource = _capturedFaceSource,
            CreatedAt = DateTime.Now
        };

        await AppDatabase.SimpanUserAsync(user);

        await DisplayAlert(
            "Sukses",
            $"Akun {role} berhasil dibuat dan data wajah berhasil disimpan.",
            "OK"
        );

        await Navigation.PopAsync();
    }

    private byte[] ConvertBitmapToBytes(SKBitmap bitmap)
    {
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private async void OnBackToLoginClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}