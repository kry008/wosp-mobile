using System.Text.Json;

namespace wosp_mobile;

public partial class StatisticsCountingPage : ContentPage
{
    private readonly HttpClient httpClient = new HttpClient();

    public StatisticsCountingPage()
    {
        InitializeComponent();
        LoadCountingStats();
    }

    private async void LoadCountingStats()
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

            httpClient.DefaultRequestHeaders.Add("x-api-token", token);
            var response = await httpClient.GetAsync($"{baseUrl}/api/statystyki/liczacy");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Błąd", $"Błąd pobierania danych: {response.StatusCode}", "OK");
                return;
            }

            var result = JsonSerializer.Deserialize<CountingStatsResponse>(responseBody);
            if (result?.success == true)
            {
                CountingStatsCollectionView.ItemsSource = result.liczacy;
            }
            else
            {
                await DisplayAlertAsync("Błąd", "Nie udało się pobrać statystyk liczących.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Błąd", $"Błąd: {ex.Message}", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///MainPage");
    }
}

public class CountingStat
{
    public int id { get; set; }
    public string imie { get; set; } = string.Empty;
    public string nazwisko { get; set; } = string.Empty;
    public int liczbaRozliczen { get; set; }
    public double suma { get; set; }
}

public class CountingStatsResponse
{
    public bool success { get; set; }
    public List<CountingStat> liczacy { get; set; } = new List<CountingStat>();
}