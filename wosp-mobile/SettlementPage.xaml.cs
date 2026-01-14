using System.Text.Json;
using Microsoft.Maui.Controls.Shapes;

namespace wosp_mobile;

[QueryProperty(nameof(VolunteerId), "volunteerId")]
public partial class SettlementPage : ContentPage
{
    private readonly HttpClient httpClient = new HttpClient();
    private int volunteerId;
    private List<CountingPerson> allCounters = new List<CountingPerson>();
    private List<CountingPerson> selectedCounters = new List<CountingPerson>();
    private List<CountingPerson> filteredCounters = new List<CountingPerson>();
    private bool hasTerminal = false;

    public int VolunteerId
    {
        get => volunteerId;
        set
        {
            volunteerId = value;
            ClearForm();
            LoadVolunteerDetails();
        }
    }

    public SettlementPage()
    {
        InitializeComponent();
        SetupTextChangedEvents();
        LoadCountingPersons();
    }

    private void SetupTextChangedEvents()
    {
        KwotaZTerminalaEntry.TextChanged += OnEntryTextChanged;
        M1grEntry.TextChanged += OnEntryTextChanged;
        M2grEntry.TextChanged += OnEntryTextChanged;
        M5grEntry.TextChanged += OnEntryTextChanged;
        M10grEntry.TextChanged += OnEntryTextChanged;
        M20grEntry.TextChanged += OnEntryTextChanged;
        M50grEntry.TextChanged += OnEntryTextChanged;
        M1zlEntry.TextChanged += OnEntryTextChanged;
        M2zlEntry.TextChanged += OnEntryTextChanged;
        M5zlEntry.TextChanged += OnEntryTextChanged;
        B10zlEntry.TextChanged += OnEntryTextChanged;
        B20zlEntry.TextChanged += OnEntryTextChanged;
        B50zlEntry.TextChanged += OnEntryTextChanged;
        B100zlEntry.TextChanged += OnEntryTextChanged;
        B200zlEntry.TextChanged += OnEntryTextChanged;
        B500zlEntry.TextChanged += OnEntryTextChanged;
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        CalculateSummary();
    }

    private async void LoadCountingPersons()
    {
        try
        {
            var token = Preferences.Get("auth_token", "");
            var baseUrl = Preferences.Get("base_url", "");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(baseUrl))
                return;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-token", token);
            var response = await client.GetAsync($"{baseUrl}/api/users/liczacy");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CountingPersonsResponse>(responseBody);
                if (result?.success == true)
                {
                    allCounters = result.liczacy.Select(c => new CountingPerson
                    {
                        id = c.id,
                        imie = c.imie,
                        nazwisko = c.nazwisko,
                        DisplayName = $"{c.imie} {c.nazwisko}"
                    }).ToList();

                    // Automatycznie dodaj zalogowanego użytkownika
                    var myCounter = allCounters.FirstOrDefault(c => c.id == result.mojeId);
                    if (myCounter != null && !selectedCounters.Contains(myCounter))
                    {
                        selectedCounters.Add(myCounter);
                        UpdateSelectedCountersUI();
                    }

                    filteredCounters = allCounters.ToList();
                    AvailableCountersCollectionView.ItemsSource = filteredCounters;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Błąd", $"Nie udało się pobrać listy liczących: {ex.Message}", "OK");
        }
    }

    private async void LoadVolunteerDetails()
    {
        try
        {
            var token = Preferences.Get("auth_token", "");
            var baseUrl = Preferences.Get("base_url", "");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(baseUrl))
            {
                await DisplayAlertAsync("Błąd", "Brak tokenu lub URL. Zaloguj się ponownie.", "OK");
                await Shell.Current.GoToAsync("///LoginPage");
                return;
            }

            httpClient.DefaultRequestHeaders.Remove("x-api-token");
            httpClient.DefaultRequestHeaders.Add("x-api-token", token);
            var response = await httpClient.GetAsync($"{baseUrl}/api/rozliczenia/wolontariusz/{volunteerId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Błąd", $"Błąd pobierania danych: {response.StatusCode}", "OK");
                return;
            }

            var result = JsonSerializer.Deserialize<VolunteerDetailResponse>(responseBody);
            if (result?.success == true)
            {
                VolunteerInfoLabel.Text = $"{result.wolontariusz.numerID} - {result.wolontariusz.imie} {result.wolontariusz.nazwisko}";
                hasTerminal = result.hadTerminal;
                TerminalFrame.IsVisible = true; // Zawsze widoczna sekcja
                NoTerminalLabel.IsVisible = !hasTerminal;
                TerminalControls.IsVisible = hasTerminal;
                if (result.hadTerminal)
                {
                    TerminalSwitch.IsToggled = true;
                }
                CalculateSummary();
            }
            else
            {
                await DisplayAlertAsync("Błąd", "Nie udało się pobrać szczegółów wolontariusza.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Błąd", $"Błąd: {ex.Message}", "OK");
        }
    }

    private void OnTerminalToggled(object sender, ToggledEventArgs e)
    {
        // Jeśli przełącznik został wyłączony, czyścimy pole kwoty terminala, i przeliczamy podsumowanie
        if (!e.Value)
        {
            KwotaZTerminalaEntry.Text = string.Empty;
        }
        CalculateSummary();
    }

    private async void OnRozliczClicked(object sender, EventArgs e)
    {
        try
        {
            var token = Preferences.Get("auth_token", "");
            var baseUrl = Preferences.Get("base_url", "");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(baseUrl))
            {
                await DisplayAlertAsync("Błąd", "Brak tokenu lub URL. Zaloguj się ponownie.", "OK");
                await Shell.Current.GoToAsync("///LoginPage");
                return;
            }

            var settlementData = new
            {
                terminal = TerminalSwitch.IsToggled ? 1 : 0,
                kwotaZTerminala = double.TryParse(KwotaZTerminalaEntry.Text, out var kzt) ? kzt : 0,
                m1gr = int.TryParse(M1grEntry.Text, out var m1gr) ? m1gr : 0,
                m2gr = int.TryParse(M2grEntry.Text, out var m2gr) ? m2gr : 0,
                m5gr = int.TryParse(M5grEntry.Text, out var m5gr) ? m5gr : 0,
                m10gr = int.TryParse(M10grEntry.Text, out var m10gr) ? m10gr : 0,
                m20gr = int.TryParse(M20grEntry.Text, out var m20gr) ? m20gr : 0,
                m50gr = int.TryParse(M50grEntry.Text, out var m50gr) ? m50gr : 0,
                m1zl = int.TryParse(M1zlEntry.Text, out var m1zl) ? m1zl : 0,
                m2zl = int.TryParse(M2zlEntry.Text, out var m2zl) ? m2zl : 0,
                m5zl = int.TryParse(M5zlEntry.Text, out var m5zl) ? m5zl : 0,
                b10zl = int.TryParse(B10zlEntry.Text, out var b10zl) ? b10zl : 0,
                b20zl = int.TryParse(B20zlEntry.Text, out var b20zl) ? b20zl : 0,
                b50zl = int.TryParse(B50zlEntry.Text, out var b50zl) ? b50zl : 0,
                b100zl = int.TryParse(B100zlEntry.Text, out var b100zl) ? b100zl : 0,
                b200zl = int.TryParse(B200zlEntry.Text, out var b200zl) ? b200zl : 0,
                b500zl = int.TryParse(B500zlEntry.Text, out var b500zl) ? b500zl : 0,
                walutaObca = string.IsNullOrWhiteSpace(WalutaObcaEntry.Text) ? "BRAK" : WalutaObcaEntry.Text.Trim(),
                daryInne = string.IsNullOrWhiteSpace(DaryInneEntry.Text) ? "BRAK" : DaryInneEntry.Text.Trim(),
                uwagiLiczacych = string.IsNullOrWhiteSpace(UwagiLiczacychEditor.Text) ? "BRAK" : UwagiLiczacychEditor.Text.Trim(),
                uwagiWolontariusza = string.IsNullOrWhiteSpace(UwagiWolontariuszaEditor.Text) ? "BRAK" : UwagiWolontariuszaEditor.Text.Trim(),
                sala = string.IsNullOrWhiteSpace(SalaEntry.Text) ? "GŁÓWNA" : SalaEntry.Text.Trim(),
                liczacy = string.Join(",", selectedCounters.Select(c => c.id))
            };

            var jsonContent = JsonSerializer.Serialize(settlementData);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Remove("x-api-token");
            httpClient.DefaultRequestHeaders.Add("x-api-token", token);
            var response = await httpClient.PostAsync($"{baseUrl}/api/rozliczenia/wolontariusz/{volunteerId}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<SettlementResponse>(responseBody);
                if (result?.success == true)
                {
                    await DisplayAlertAsync("Sukces", "Wolontariusz został rozliczony.", "OK");
                    await Shell.Current.GoToAsync("///VolunteerListPage"); // Wróć do listy
                }
                else
                {
                    await DisplayAlertAsync("Błąd", result?.message ?? "Nieznany błąd.", "OK");
                }
            }
            else
            {
                await DisplayAlertAsync("Błąd", $"Błąd serwera: {response.StatusCode}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Błąd", $"Błąd: {ex.Message}", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///VolunteerListPage");
    }

    private void OnSearchCountersTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";
        if (string.IsNullOrWhiteSpace(searchText))
        {
            filteredCounters = allCounters.Where(c => !selectedCounters.Any(s => s.id == c.id)).ToList();
        }
        else
        {
            filteredCounters = allCounters
                .Where(c => !selectedCounters.Any(s => s.id == c.id) &&
                           (c.imie.ToLower().Contains(searchText) ||
                            c.nazwisko.ToLower().Contains(searchText)))
                .ToList();
        }
        AvailableCountersCollectionView.ItemsSource = filteredCounters;
    }

    private void OnCounterTapped(object sender, EventArgs e)
    {
        var frame = sender as Frame;
        var counter = frame?.BindingContext as CountingPerson;
        if (counter != null && !selectedCounters.Any(s => s.id == counter.id))
        {
            selectedCounters.Add(counter);
            UpdateSelectedCountersUI();
            OnSearchCountersTextChanged(SearchCountersEntry, new TextChangedEventArgs("", SearchCountersEntry.Text));
        }
    }

    private void UpdateSelectedCountersUI()
    {
        SelectedCountersLayout.Clear();
        foreach (var counter in selectedCounters)
        {
            var border = new Border
            {
                Padding = new Thickness(8, 4),
                Margin = new Thickness(2),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Color.FromArgb("#E3F2FD"),
                Stroke = Color.FromArgb("#2196F3"),
                StrokeThickness = 1
            };

            var stack = new HorizontalStackLayout { Spacing = 5 };
            var label = new Label
            {
                Text = counter.DisplayName,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Color.FromArgb("#2196F3"),
                FontAttributes = FontAttributes.Bold
            };
            var removeButton = new Button
            {
                Text = "×",
                FontSize = 20,
                Padding = new Thickness(5, 0),
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#2196F3")
            };
            removeButton.Clicked += (s, e) => OnRemoveCounterClicked(counter);

            stack.Add(label);
            stack.Add(removeButton);
            border.Content = stack;
            SelectedCountersLayout.Add(border);
        }
    }

    private void OnRemoveCounterClicked(CountingPerson counter)
    {
        selectedCounters.Remove(counter);
        UpdateSelectedCountersUI();
        OnSearchCountersTextChanged(SearchCountersEntry, new TextChangedEventArgs("", SearchCountersEntry.Text));
    }

    private void ClearForm()
    {
        // Czyść wszystkie pola formularza
        TerminalSwitch.IsToggled = false;
        KwotaZTerminalaEntry.Text = "";
        M1grEntry.Text = "";
        M2grEntry.Text = "";
        M5grEntry.Text = "";
        M10grEntry.Text = "";
        M20grEntry.Text = "";
        M50grEntry.Text = "";
        M1zlEntry.Text = "";
        M2zlEntry.Text = "";
        M5zlEntry.Text = "";
        B10zlEntry.Text = "";
        B20zlEntry.Text = "";
        B50zlEntry.Text = "";
        B100zlEntry.Text = "";
        B200zlEntry.Text = "";
        B500zlEntry.Text = "";
        WalutaObcaEntry.Text = "";
        DaryInneEntry.Text = "";
        SalaEntry.Text = "GŁÓWNA";
        UwagiLiczacychEditor.Text = "";
        UwagiWolontariuszaEditor.Text = "";
        SearchCountersEntry.Text = "";
        selectedCounters.Clear();
        UpdateSelectedCountersUI();
        CalculateSummary();
    }

    private void CalculateSummary()
    {
        double sumaRozliczenia = 0;

        // Monety grosze
        sumaRozliczenia += int.TryParse(M1grEntry.Text, out var v) ? v * 0.01 : 0;
        sumaRozliczenia += int.TryParse(M2grEntry.Text, out v) ? v * 0.02 : 0;
        sumaRozliczenia += int.TryParse(M5grEntry.Text, out v) ? v * 0.05 : 0;
        sumaRozliczenia += int.TryParse(M10grEntry.Text, out v) ? v * 0.10 : 0;
        sumaRozliczenia += int.TryParse(M20grEntry.Text, out v) ? v * 0.20 : 0;
        sumaRozliczenia += int.TryParse(M50grEntry.Text, out v) ? v * 0.50 : 0;

        // Monety złote
        sumaRozliczenia += int.TryParse(M1zlEntry.Text, out v) ? v * 1 : 0;
        sumaRozliczenia += int.TryParse(M2zlEntry.Text, out v) ? v * 2 : 0;
        sumaRozliczenia += int.TryParse(M5zlEntry.Text, out v) ? v * 5 : 0;

        // Banknoty
        sumaRozliczenia += int.TryParse(B10zlEntry.Text, out v) ? v * 10 : 0;
        sumaRozliczenia += int.TryParse(B20zlEntry.Text, out v) ? v * 20 : 0;
        sumaRozliczenia += int.TryParse(B50zlEntry.Text, out v) ? v * 50 : 0;
        sumaRozliczenia += int.TryParse(B100zlEntry.Text, out v) ? v * 100 : 0;
        sumaRozliczenia += int.TryParse(B200zlEntry.Text, out v) ? v * 200 : 0;
        sumaRozliczenia += int.TryParse(B500zlEntry.Text, out v) ? v * 500 : 0;

        double sumaTerminala = double.TryParse(KwotaZTerminalaEntry.Text, out var t) ? t : 0;
        double sumaTotal = sumaRozliczenia + sumaTerminala;

        if (!hasTerminal)
        {
            SummaryRozliczenieLabel.IsVisible = false;
            SummaryTerminalLabel.IsVisible = false;
            SummaryTotalLabel.Text = $"Suma: {sumaRozliczenia:F2} zł";
        }
        else
        {
            SummaryRozliczenieLabel.IsVisible = true;
            SummaryTerminalLabel.IsVisible = true;
            SummaryRozliczenieLabel.Text = $"Suma rozliczenia: {sumaRozliczenia:F2} zł";
            SummaryTerminalLabel.Text = $"Suma z terminala: {sumaTerminala:F2} zł";
            SummaryTotalLabel.Text = $"Suma: {sumaTotal:F2} zł";
        }
    }
}

public class VolunteerDetail
{
    public int id { get; set; }
    public string numerID { get; set; } = string.Empty;
    public string imie { get; set; } = string.Empty;
    public string nazwisko { get; set; } = string.Empty;
    // Inne właściwości jeśli potrzebne
}

public class VolunteerDetailResponse
{
    public bool success { get; set; }
    public VolunteerDetail wolontariusz { get; set; } = new VolunteerDetail();
    public bool hadTerminal { get; set; }
}

public class SettlementResponse
{
    public bool success { get; set; }
    public int rozliczenieID { get; set; }
    public string message { get; set; } = string.Empty;
}

public class CountingPerson
{
    public int id { get; set; }
    public string imie { get; set; } = string.Empty;
    public string nazwisko { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class CountingPersonsResponse
{
    public bool success { get; set; }
    public List<CountingPerson> liczacy { get; set; } = new List<CountingPerson>();
    public int mojeId { get; set; }
}