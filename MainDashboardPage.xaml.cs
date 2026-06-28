using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace DaerahRawanBanjir;

public partial class MainDashboardPage : ContentPage
{
    private readonly GeminiNlpService _geminiService = new();
    private readonly BmkgDummyRealtimeService _realtimeService = new();

    private IDispatcherTimer? _monitoringTimer;

    private readonly Label _labelUserInfo;
    private readonly Label _labelRainfallValue;
    private readonly Label _labelRainfallCategory;
    private readonly Label _labelElevationValue;
    private readonly Label _labelRiskPercent;
    private readonly Label _labelStatus;
    private readonly Label _labelLastUpdate;
    private readonly Label _labelRecommendation;

    private readonly ProgressBar _rainProgressBar;
    private readonly ProgressBar _categoryProgressBar;
    private readonly ProgressBar _elevationProgressBar;
    private readonly ProgressBar _riskProgressBar;

    private readonly CollectionView _monitoringCollectionView;
    private readonly CollectionView _chatCollectionView;

    private readonly Entry _entryChatMessage;

    private readonly Button _buttonSendChat;
    private readonly Button _buttonHistory;
    private readonly Button _buttonSendToMasyarakat;
    private readonly Button _buttonStartStopMonitoring;

    public ObservableCollection<MonitoringLogItem> MonitoringItems { get; } = new();
    public ObservableCollection<ChatMessage> ChatMessages { get; } = new();

    private double _lastCurahHujan = 0;
    private double _lastElevasi = 0;
    private string _lastKategoriHujan = "-";
    private string _lastStatus = "Belum Dianalisis";
    private bool _isMonitoringRunning = true;
    private bool _governmentMessagesLoaded = false;

    public MainDashboardPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);

        Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Color.FromArgb("#0E3B68"), 0.0f),
                new GradientStop(Color.FromArgb("#0B2E58"), 0.45f),
                new GradientStop(Color.FromArgb("#071B36"), 1.0f)
            }
        };

        _labelUserInfo = new Label
        {
            Text = $"User: {UserSession.RegisteredUsername} ({UserSession.Role})",
            TextColor = Color.FromArgb("#39F1D0"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        };

        _buttonHistory = new Button
        {
            Text = "History",
            BackgroundColor = Color.FromArgb("#31B86B"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 4,
            WidthRequest = 95,
            HeightRequest = 42,
            IsVisible = UserSession.IsPemerintah
        };
        _buttonHistory.Clicked += OnHistoryClicked;

        Button buttonLogout = new Button
        {
            Text = "Logout",
            BackgroundColor = Color.FromArgb("#EF4444"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 4,
            WidthRequest = 95,
            HeightRequest = 42
        };
        buttonLogout.Clicked += OnLogoutClicked;

        _buttonStartStopMonitoring = new Button
        {
            Text = "Pause",
            BackgroundColor = Color.FromArgb("#FFD868"),
            TextColor = Color.FromArgb("#071B36"),
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 4,
            WidthRequest = 95,
            HeightRequest = 42
        };
        _buttonStartStopMonitoring.Clicked += OnStartStopMonitoringClicked;

        _labelRainfallValue = CreateBigValueLabel("0.00 mm");
        _labelRainfallCategory = CreateBigValueLabel("-");
        _labelElevationValue = CreateBigValueLabel("0.00 m");

        _labelRiskPercent = new Label
        {
            Text = "--%",
            TextColor = Color.FromArgb("#FFD868"),
            FontSize = 36,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };

        _labelStatus = new Label
        {
            Text = "Menunggu Data",
            TextColor = Color.FromArgb("#FFD868"),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };

        _labelLastUpdate = new Label
        {
            Text = "Update terakhir: -",
            TextColor = Color.FromArgb("#8DD9FF"),
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center
        };

        _labelRecommendation = new Label
        {
            Text = "Rekomendasi akan muncul setelah data monitoring berjalan.",
            TextColor = Color.FromArgb("#D8F3FF"),
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _rainProgressBar = CreateProgressBar();
        _categoryProgressBar = CreateProgressBar();
        _elevationProgressBar = CreateProgressBar();
        _riskProgressBar = CreateProgressBar();

        _monitoringCollectionView = new CollectionView
        {
            BackgroundColor = Color.FromArgb("#0B2E58"),
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                Label timeLabel = new Label
                {
                    TextColor = Color.FromArgb("#39F1D0"),
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold
                };
                timeLabel.SetBinding(Label.TextProperty, nameof(MonitoringLogItem.TimeText));

                Label dataLabel = new Label
                {
                    TextColor = Color.FromArgb("#E6FAFF"),
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                dataLabel.SetBinding(Label.TextProperty, nameof(MonitoringLogItem.DataText));

                Label statusLabel = new Label
                {
                    TextColor = Color.FromArgb("#FFD868"),
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold
                };
                statusLabel.SetBinding(Label.TextProperty, nameof(MonitoringLogItem.StatusText));

                return new Frame
                {
                    Margin = new Thickness(4, 4, 4, 6),
                    Padding = 8,
                    CornerRadius = 6,
                    HasShadow = false,
                    BackgroundColor = Color.FromArgb("#164C82"),
                    BorderColor = Color.FromArgb("#2A78B4"),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 3,
                        Children =
                        {
                            timeLabel,
                            dataLabel,
                            statusLabel
                        }
                    }
                };
            })
        };
        _monitoringCollectionView.ItemsSource = MonitoringItems;

        _chatCollectionView = new CollectionView
        {
            BackgroundColor = Color.FromArgb("#0B2E58"),
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                Label senderLabel = new Label
                {
                    TextColor = Color.FromArgb("#39F1D0"),
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold
                };
                senderLabel.SetBinding(Label.TextProperty, nameof(ChatMessage.Sender));

                Label textLabel = new Label
                {
                    TextColor = Color.FromArgb("#E6FAFF"),
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                textLabel.SetBinding(Label.TextProperty, nameof(ChatMessage.Text));

                return new Frame
                {
                    Margin = new Thickness(4, 4, 4, 6),
                    Padding = 8,
                    CornerRadius = 6,
                    HasShadow = false,
                    BackgroundColor = Color.FromArgb("#164C82"),
                    BorderColor = Color.FromArgb("#2A78B4"),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            senderLabel,
                            textLabel
                        }
                    }
                };
            })
        };
        _chatCollectionView.ItemsSource = ChatMessages;

        _entryChatMessage = new Entry
        {
            Placeholder = UserSession.IsPemerintah
                ? "Tanya Gemini atau tulis pesan untuk masyarakat..."
                : "Tanya risiko banjir berdasarkan data monitoring...",
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#87BFE8"),
            BackgroundColor = Color.FromArgb("#0B2E58")
        };
        _entryChatMessage.Completed += OnSendChatClicked;

        _buttonSendChat = new Button
        {
            Text = "Kirim",
            BackgroundColor = Color.FromArgb("#238DF3"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 4,
            WidthRequest = 70
        };
        _buttonSendChat.Clicked += OnSendChatClicked;

        _buttonSendToMasyarakat = new Button
        {
            Text = "Kirim Pesan ke Masyarakat",
            BackgroundColor = Color.FromArgb("#31B86B"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 4,
            HeightRequest = 42,
            IsVisible = UserSession.IsPemerintah
        };
        _buttonSendToMasyarakat.Clicked += OnSendToMasyarakatClicked;

        Content = CreateDashboardLayout(buttonLogout);

        ChatMessages.Add(new ChatMessage
        {
            Sender = "Gemini",
            Text = "Halo, saya asisten NLP untuk sistem monitoring real-time risiko banjir. Data curah hujan, kategori hujan, dan elevasi tanah akan diperbarui otomatis menggunakan dummy dinamis berbasis kategori BMKG."
        });

        if (UserSession.IsPemerintah)
        {
            ChatMessages.Add(new ChatMessage
            {
                Sender = "Sistem",
                Text = "Anda masuk sebagai User Pemerintah. Anda dapat memantau data real-time, melihat history akses masyarakat, bertanya kepada Gemini, dan mengirim pesan kepada User Masyarakat."
            });
        }
        else
        {
            ChatMessages.Add(new ChatMessage
            {
                Sender = "Sistem",
                Text = "Anda masuk sebagai User Masyarakat. Anda dapat memantau kondisi banjir real-time dan menerima pesan dari User Pemerintah."
            });
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        StartMonitoring();

        if (UserSession.IsMasyarakat && !_governmentMessagesLoaded)
        {
            _governmentMessagesLoaded = true;
            await LoadGovernmentMessagesForMasyarakatAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        StopMonitoring();
    }

    private View CreateDashboardLayout(Button buttonLogout)
    {
        Grid mainGrid = new Grid
        {
            Padding = 10,
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(2.1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }
            }
        };

        VerticalStackLayout leftLayout = new VerticalStackLayout
        {
            Spacing = 10
        };

        Grid headerGrid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        VerticalStackLayout titleLayout = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label
                {
                    Text = "SISTEM MONITORING REAL-TIME RISIKO RAWAN BANJIR",
                    TextColor = Color.FromArgb("#E6FAFF"),
                    FontSize = 23,
                    FontAttributes = FontAttributes.Bold
                },

                new Label
                {
                    Text = "Monitoring Real-time berbasis parameter curah hujan, data hujan BMKG, dan elevasi tanah daerah",
                    TextColor = Color.FromArgb("#8DD9FF"),
                    FontSize = 13
                },

                _labelUserInfo
            }
        };

        headerGrid.Add(titleLayout, 0, 0);
        headerGrid.Add(_buttonStartStopMonitoring, 1, 0);
        headerGrid.Add(_buttonHistory, 2, 0);
        headerGrid.Add(buttonLogout, 3, 0);

        leftLayout.Children.Add(CreateFrame(headerGrid));

        Grid parameterGrid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        parameterGrid.Add(
            CreateParameterCard(
                "TINGKAT CURAH HUJAN",
                _labelRainfallValue,
                "mm / 24 jam",
                Color.FromArgb("#2E9BFF")
            ),
            0,
            0
        );

        parameterGrid.Add(
            CreateParameterCard(
                "KATEGORI HUJAN",
                _labelRainfallCategory,
                "BMKG",
                Color.FromArgb("#FFD868")
            ),
            1,
            0
        );

        parameterGrid.Add(
            CreateParameterCard(
                "ELEVASI TANAH",
                _labelElevationValue,
                "meter",
                Color.FromArgb("#FF9B64")
            ),
            2,
            0
        );

        leftLayout.Children.Add(parameterGrid);

        Grid riskGrid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) }
            }
        };

        riskGrid.Add(CreateFrame(new VerticalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Frame
                {
                    WidthRequest = 150,
                    HeightRequest = 150,
                    CornerRadius = 75,
                    Padding = 0,
                    HasShadow = false,
                    BackgroundColor = Colors.Transparent,
                    BorderColor = Color.FromArgb("#FFD868"),
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        Children =
                        {
                            _labelRiskPercent,
                            new Label
                            {
                                Text = "Risk Score",
                                TextColor = Color.FromArgb("#E6FAFF"),
                                FontSize = 14,
                                HorizontalTextAlignment = TextAlignment.Center
                            }
                        }
                    }
                },

                _labelStatus,
                _labelLastUpdate
            }
        }), 0, 0);

        riskGrid.Add(CreateFrame(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = "Indikator Monitoring",
                    TextColor = Color.FromArgb("#D8F3FF"),
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold
                },

                CreateSmallLabel("Tingkat Curah Hujan"),
                _rainProgressBar,

                CreateSmallLabel("Risiko Berdasarkan Kategori Hujan"),
                _categoryProgressBar,

                CreateSmallLabel("Risiko Elevasi Tanah Rendah"),
                _elevationProgressBar,

                CreateSmallLabel("Total Risiko Banjir"),
                _riskProgressBar,

                new Label
                {
                    Text = "Rekomendasi Sistem",
                    TextColor = Color.FromArgb("#D8F3FF"),
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(0, 8, 0, 0)
                },

                _labelRecommendation
            }
        }), 1, 0);

        leftLayout.Children.Add(riskGrid);

        Frame logFrame = CreateFrame(new Grid
        {
            RowSpacing = 8,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            Children =
            {
                new Label
                {
                    Text = "Log Monitoring Real-Time",
                    TextColor = Color.FromArgb("#D8F3FF"),
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold
                }
            }
        }, 14);

        Grid logGrid = (Grid)logFrame.Content;
        logGrid.Add(_monitoringCollectionView, 0, 1);
        logFrame.HeightRequest = 250;

        leftLayout.Children.Add(logFrame);

        VerticalStackLayout rightLayout = new VerticalStackLayout
        {
            Spacing = 10
        };

        Grid chatInputGrid = new Grid
        {
            ColumnSpacing = 6,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        chatInputGrid.Add(_entryChatMessage, 0, 0);
        chatInputGrid.Add(_buttonSendChat, 1, 0);

        Grid chatGrid = new Grid
        {
            RowSpacing = 8,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        chatGrid.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = "Chat Box AI Assistant Gemini",
                    TextColor = Color.FromArgb("#D8F3FF"),
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold
                },

                new Label
                {
                    Text = UserSession.IsPemerintah
                        ? "Chat dengan Gemini atau kirim pesan kepada User Masyarakat"
                        : "Tanya Gemini berdasarkan data monitoring dan pesan dari pemerintah",
                    TextColor = Color.FromArgb("#8DD9FF"),
                    FontSize = 11,
                    LineBreakMode = LineBreakMode.WordWrap
                }
            }
        }, 0, 0);

        chatGrid.Add(_chatCollectionView, 0, 1);
        chatGrid.Add(chatInputGrid, 0, 2);
        chatGrid.Add(_buttonSendToMasyarakat, 0, 3);

        Frame chatFrame = CreateFrame(chatGrid, 14);
        chatFrame.HeightRequest = 620;

        rightLayout.Children.Add(chatFrame);

        mainGrid.Add(leftLayout, 0, 0);
        mainGrid.Add(rightLayout, 1, 0);

        return new ScrollView
        {
            Content = mainGrid
        };
    }

    private View CreateParameterCard(
        string title,
        Label valueLabel,
        string subtitle,
        Color accentColor)
    {
        return CreateFrame(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = title,
                    TextColor = Color.FromArgb("#8DD9FF"),
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                },

                valueLabel,

                new Label
                {
                    Text = subtitle,
                    TextColor = accentColor,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
        }, 16);
    }

    private Label CreateBigValueLabel(string text)
    {
        return new Label
        {
            Text = text,
            TextColor = Color.FromArgb("#E6FAFF"),
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private ProgressBar CreateProgressBar()
    {
        return new ProgressBar
        {
            Progress = 0,
            ProgressColor = Color.FromArgb("#45BDF8"),
            BackgroundColor = Color.FromArgb("#0B2E58")
        };
    }

    private Label CreateSmallLabel(string text)
    {
        return new Label
        {
            Text = text,
            TextColor = Color.FromArgb("#D8F3FF"),
            FontSize = 12
        };
    }

    private Frame CreateFrame(View content, int padding = 16)
    {
        return new Frame
        {
            Padding = padding,
            CornerRadius = 0,
            HasShadow = false,
            BackgroundColor = Color.FromArgb("#123E70"),
            BorderColor = Color.FromArgb("#1F6FA8"),
            Content = content
        };
    }

    private void StartMonitoring()
    {
        if (_monitoringTimer != null)
        {
            if (_isMonitoringRunning)
                _monitoringTimer.Start();

            return;
        }

        _monitoringTimer = Dispatcher.CreateTimer();
        _monitoringTimer.Interval = TimeSpan.FromSeconds(5);
        _monitoringTimer.Tick += async (sender, e) =>
        {
            await UpdateRealtimeMonitoringAsync();
        };

        _monitoringTimer.Start();
        _isMonitoringRunning = true;
        _buttonStartStopMonitoring.Text = "Pause";

        _ = UpdateRealtimeMonitoringAsync();
    }

    private void StopMonitoring()
    {
        _monitoringTimer?.Stop();
    }

    private async Task UpdateRealtimeMonitoringAsync()
    {
        if (!_isMonitoringRunning)
            return;

        RealtimeFloodData data = _realtimeService.GenerateNextData();

        FuzzyFloodResult result =
            FuzzyFloodService.ProcessMonitoring(
                data.RainfallMmPerDay,
                data.RainfallCategory,
                data.ElevationMeter
            );

        _lastCurahHujan = data.RainfallMmPerDay;
        _lastElevasi = data.ElevationMeter;
        _lastKategoriHujan = data.RainfallCategoryText;
        _lastStatus = result.StatusText;

        _labelRainfallValue.Text = $"{data.RainfallMmPerDay:F2} mm";
        _labelRainfallCategory.Text = data.RainfallCategoryText;
        _labelElevationValue.Text = $"{data.ElevationMeter:F2} m";
        _labelRiskPercent.Text = $"{result.RiskScore:F0}%";
        _labelStatus.Text = result.StatusText;
        _labelLastUpdate.Text = $"Update terakhir: {data.UpdatedAt:HH:mm:ss}";
        _labelRecommendation.Text = result.Recommendation;

        Color statusColor = GetStatusColor(result.StatusCode);

        _labelRiskPercent.TextColor = statusColor;
        _labelStatus.TextColor = statusColor;
        _riskProgressBar.ProgressColor = statusColor;

        double categoryRisk =
            RainfallCategoryHelper.GetCategoryRiskValue(data.RainfallCategory) / 100.0;

        await Task.WhenAll(
            _rainProgressBar.ProgressTo(result.RainHigh, 350, Easing.CubicOut),
            _categoryProgressBar.ProgressTo(categoryRisk, 350, Easing.CubicOut),
            _elevationProgressBar.ProgressTo(result.ElevationLow, 350, Easing.CubicOut),
            _riskProgressBar.ProgressTo(Math.Clamp(result.RiskScore / 100.0, 0, 1), 350, Easing.CubicOut)
        );

        MonitoringItems.Insert(0, new MonitoringLogItem
        {
            TimeText = data.UpdatedAt.ToString("HH:mm:ss"),
            DataText = $"Curah hujan: {data.RainfallMmPerDay:F2} mm/24 jam | Kategori: {data.RainfallCategoryText} | Elevasi: {data.ElevationMeter:F2} m",
            StatusText = $"Status: {result.StatusText} ({result.RiskScore:F0}%)"
        });

        while (MonitoringItems.Count > 12)
        {
            MonitoringItems.RemoveAt(MonitoringItems.Count - 1);
        }
    }

    private Color GetStatusColor(string statusCode)
    {
        if (statusCode == "BAHAYA")
        {
            return Color.FromArgb("#FF806E");
        }

        if (statusCode == "WASPADA")
        {
            return Color.FromArgb("#FFD868");
        }

        return Color.FromArgb("#39F1D0");
    }

    private async void OnStartStopMonitoringClicked(object? sender, EventArgs e)
    {
        _isMonitoringRunning = !_isMonitoringRunning;

        if (_isMonitoringRunning)
        {
            _buttonStartStopMonitoring.Text = "Pause";
            _buttonStartStopMonitoring.BackgroundColor = Color.FromArgb("#FFD868");
            _buttonStartStopMonitoring.TextColor = Color.FromArgb("#071B36");
            _monitoringTimer?.Start();

            await UpdateRealtimeMonitoringAsync();
        }
        else
        {
            _buttonStartStopMonitoring.Text = "Start";
            _buttonStartStopMonitoring.BackgroundColor = Color.FromArgb("#31B86B");
            _buttonStartStopMonitoring.TextColor = Colors.White;
            _monitoringTimer?.Stop();
        }
    }

    private async void OnSendChatClicked(object? sender, EventArgs e)
    {
        string question = _entryChatMessage.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(question))
        {
            return;
        }

        _entryChatMessage.Text = string.Empty;

        ChatMessage userMessage = new ChatMessage
        {
            Sender = "Anda",
            Text = question
        };

        ChatMessages.Add(userMessage);

        ChatMessage loadingMessage = new ChatMessage
        {
            Sender = "Gemini",
            Text = "Sedang menganalisis data monitoring real-time..."
        };

        ChatMessages.Add(loadingMessage);

        _buttonSendChat.IsEnabled = false;
        _entryChatMessage.IsEnabled = false;
        _buttonSendToMasyarakat.IsEnabled = false;

        await ScrollChatToEnd(loadingMessage);

        string enhancedQuestion =
            $"Data monitoring saat ini:\n" +
            $"- Curah hujan: {_lastCurahHujan:F2} mm/24 jam\n" +
            $"- Kategori hujan: {_lastKategoriHujan}\n" +
            $"- Elevasi tanah: {_lastElevasi:F2} meter\n" +
            $"- Status risiko banjir: {_lastStatus}\n\n" +
            $"Pertanyaan user: {question}";

        string answer;

        try
        {
            answer = await _geminiService.AskAsync(
                enhancedQuestion,
                _lastCurahHujan,
                _lastElevasi,
                _lastStatus
            );
        }
        catch (Exception ex)
        {
            answer = $"Terjadi kesalahan saat menghubungi Gemini: {ex.Message}";
        }

        ChatMessages.Remove(loadingMessage);

        ChatMessage answerMessage = new ChatMessage
        {
            Sender = "Gemini",
            Text = answer
        };

        ChatMessages.Add(answerMessage);

        _buttonSendChat.IsEnabled = true;
        _entryChatMessage.IsEnabled = true;
        _buttonSendToMasyarakat.IsEnabled = true;

        await ScrollChatToEnd(answerMessage);
    }

    private async void OnSendToMasyarakatClicked(object? sender, EventArgs e)
    {
        if (!UserSession.IsPemerintah)
        {
            await DisplayAlert(
                "Akses Ditolak",
                "Hanya User Pemerintah yang dapat mengirim pesan kepada User Masyarakat.",
                "OK"
            );

            return;
        }

        string messageText = _entryChatMessage.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(messageText))
        {
            await DisplayAlert(
                "Pesan Kosong",
                "Silakan tulis pesan terlebih dahulu di chat box.",
                "OK"
            );

            return;
        }

        string finalMessage =
            $"{messageText}\n\n" +
            $"Kondisi monitoring saat pesan dikirim:\n" +
            $"- Curah hujan: {_lastCurahHujan:F2} mm/24 jam\n" +
            $"- Kategori hujan: {_lastKategoriHujan}\n" +
            $"- Elevasi tanah: {_lastElevasi:F2} meter\n" +
            $"- Status risiko: {_lastStatus}";

        bool confirm = await DisplayAlert(
            "Kirim Pesan",
            "Kirim pesan ini kepada semua User Masyarakat?",
            "Kirim",
            "Batal"
        );

        if (!confirm)
        {
            return;
        }

        await AppDatabase.SimpanPesanPemerintahAsync(finalMessage);

        ChatMessages.Add(new ChatMessage
        {
            Sender = $"Pemerintah - {UserSession.RegisteredUsername}",
            Text = finalMessage
        });

        ChatMessages.Add(new ChatMessage
        {
            Sender = "Sistem",
            Text = "Pesan berhasil dikirim. Pesan akan tampil pada Chat Box AI Assistant Gemini milik User Masyarakat."
        });

        _entryChatMessage.Text = string.Empty;

        await ScrollChatToEnd(ChatMessages.Last());
    }

    private async Task LoadGovernmentMessagesForMasyarakatAsync()
    {
        List<GovernmentMessageModel> messages =
            await AppDatabase.AmbilPesanPemerintahAsync();

        if (messages.Count == 0)
        {
            return;
        }

        ChatMessages.Add(new ChatMessage
        {
            Sender = "Sistem",
            Text = "Pesan terbaru dari User Pemerintah:"
        });

        foreach (GovernmentMessageModel message in messages.OrderBy(m => m.SentAt))
        {
            ChatMessages.Add(new ChatMessage
            {
                Sender = $"Pemerintah - {message.SenderUsername}",
                Text = $"{message.MessageText}\n\nDikirim: {message.SentAt:dd/MM/yyyy HH:mm}"
            });
        }

        await ScrollChatToEnd(ChatMessages.Last());
    }

    private async Task ScrollChatToEnd(ChatMessage message)
    {
        await Task.Delay(100);

        _chatCollectionView.ScrollTo(
            message,
            position: ScrollToPosition.End,
            animate: true
        );
    }

    private async void OnHistoryClicked(object? sender, EventArgs e)
    {
        if (!UserSession.IsPemerintah)
        {
            await DisplayAlert(
                "Akses Ditolak",
                "History akses hanya dapat dilihat oleh User Pemerintah.",
                "OK"
            );

            return;
        }

        await Navigation.PushAsync(new AccessHistoryPage());
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        bool konfirmasi = await DisplayAlert(
            "Konfirmasi Logout",
            "Apakah Anda yakin ingin keluar dari dashboard?",
            "Ya",
            "Batal"
        );

        if (konfirmasi)
        {
            StopMonitoring();
            UserSession.ClearSession();
            Application.Current!.MainPage = new NavigationPage(new MainPage());
        }
    }
}

public class MonitoringLogItem
{
    public string TimeText { get; set; } = string.Empty;

    public string DataText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;
}