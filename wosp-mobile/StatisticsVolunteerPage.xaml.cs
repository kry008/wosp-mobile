using System.Text.Json;

namespace wosp_mobile;

public partial class StatisticsVolunteerPage : ContentPage
{
    private readonly HttpClient httpClient = new HttpClient();

    public StatisticsVolunteerPage()
    {
        InitializeComponent();
        LoadVolunteerStats();
    }

    private async void LoadVolunteerStats()
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
            var response = await httpClient.GetAsync($"{baseUrl}/api/statystyki/wolontariusz");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Błąd", $"Błąd pobierania danych: {response.StatusCode}", "OK");
                return;
            }

            var result = JsonSerializer.Deserialize<VolunteerStatsResponse>(responseBody);
            if (result?.success == true)
            {
                VolunteerStatsCollectionView.ItemsSource = result.wolontariusze;
            }
            else
            {
                await DisplayAlertAsync("Błąd", "Nie udało się pobrać statystyk wolontariuszy.", "OK");
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

public class VolunteerStat
{
    public int id { get; set; }
    public string numerID { get; set; } = string.Empty;
    public string imie { get; set; } = string.Empty;
    public string nazwisko { get; set; } = string.Empty;
    public double suma { get; set; }
}

public class VolunteerStatsResponse
{
    public bool success { get; set; }
    public List<VolunteerStat> wolontariusze { get; set; } = new List<VolunteerStat>();
}