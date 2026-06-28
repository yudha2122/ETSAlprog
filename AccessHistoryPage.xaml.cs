using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace DaerahRawanBanjir;

public partial class AccessHistoryPage : ContentPage
{
    private readonly CollectionView _historyCollectionView;
    private readonly Label _labelEmpty;

    public ObservableCollection<AccessHistoryItem> HistoryItems { get; } = new();

    public AccessHistoryPage()
    {
        Title = "History Akses Masyarakat";
        BackgroundColor = Color.FromArgb("#0B2E58");

        _historyCollectionView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            BackgroundColor = Color.FromArgb("#123E70"),
            ItemTemplate = new DataTemplate(() =>
            {
                Label usernameLabel = new Label
                {
                    TextColor = Color.FromArgb("#39F1D0"),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold
                };
                usernameLabel.SetBinding(Label.TextProperty, nameof(AccessHistoryItem.Username));

                Label timeLabel = new Label
                {
                    TextColor = Color.FromArgb("#D8F3FF"),
                    FontSize = 13
                };
                timeLabel.SetBinding(Label.TextProperty, nameof(AccessHistoryItem.AccessTimeText));

                Label activityLabel = new Label
                {
                    TextColor = Color.FromArgb("#8DD9FF"),
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                activityLabel.SetBinding(Label.TextProperty, nameof(AccessHistoryItem.Activity));

                return new Frame
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = 12,
                    CornerRadius = 8,
                    HasShadow = false,
                    BackgroundColor = Color.FromArgb("#0B2E58"),
                    BorderColor = Color.FromArgb("#2A78B4"),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children =
                        {
                            usernameLabel,
                            timeLabel,
                            activityLabel
                        }
                    }
                };
            })
        };

        _historyCollectionView.ItemsSource = HistoryItems;

        _labelEmpty = new Label
        {
            Text = "Belum ada User Masyarakat yang mengakses sistem.",
            TextColor = Color.FromArgb("#8DD9FF"),
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            IsVisible = false
        };

        Button buttonRefresh = new Button
        {
            Text = "Refresh",
            BackgroundColor = Color.FromArgb("#238DF3"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 46
        };
        buttonRefresh.Clicked += OnRefreshClicked;

        Button buttonBack = new Button
        {
            Text = "Kembali",
            BackgroundColor = Color.FromArgb("#EF4444"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 46
        };
        buttonBack.Clicked += OnBackClicked;

        Grid contentGrid = new Grid
        {
            Padding = 20,
            RowSpacing = 12,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        VerticalStackLayout titleLayout = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label
                {
                    Text = "HISTORY AKSES USER MASYARAKAT",
                    TextColor = Colors.White,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                },

                new Label
                {
                    Text = "Data ini hanya dapat dilihat oleh User Pemerintah.",
                    TextColor = Color.FromArgb("#8DD9FF"),
                    FontSize = 13,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
        };

        Grid historyGrid = new Grid();

        historyGrid.Add(_historyCollectionView);
        historyGrid.Add(_labelEmpty);

        Frame historyFrame = new Frame
        {
            Padding = 12,
            CornerRadius = 10,
            HasShadow = false,
            BackgroundColor = Color.FromArgb("#123E70"),
            BorderColor = Color.FromArgb("#1F6FA8"),
            Content = historyGrid
        };

        Grid buttonGrid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        buttonGrid.Add(buttonRefresh, 0, 0);
        buttonGrid.Add(buttonBack, 1, 0);

        contentGrid.Add(titleLayout, 0, 0);
        contentGrid.Add(historyFrame, 0, 1);
        contentGrid.Add(buttonGrid, 0, 2);

        Content = contentGrid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        if (!UserSession.IsPemerintah)
        {
            await DisplayAlert(
                "Akses Ditolak",
                "Halaman history hanya dapat dibuka oleh User Pemerintah.",
                "OK"
            );

            await Navigation.PopAsync();
            return;
        }

        List<AccessHistoryModel> histories =
            await AppDatabase.AmbilRiwayatAksesMasyarakatAsync();

        HistoryItems.Clear();

        foreach (AccessHistoryModel history in histories)
        {
            HistoryItems.Add(new AccessHistoryItem
            {
                Username = history.Username,
                AccessTimeText = history.AccessedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                Activity = history.Activity
            });
        }

        _labelEmpty.IsVisible = HistoryItems.Count == 0;
        _historyCollectionView.IsVisible = HistoryItems.Count > 0;
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadHistoryAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

public class AccessHistoryItem
{
    public string Username { get; set; } = string.Empty;
    public string AccessTimeText { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
}