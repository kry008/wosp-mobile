using System.Text.Json;

namespace wosp_mobile;

public partial class StatisticsSummaryPage : ContentPage
{
    private readonly HttpClient httpClient = new HttpClient();

    public StatisticsSummaryPage()
    {
        InitializeComponent();
        LoadSummary();
    }

    private async void LoadSummary()
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
            var response = await httpClient.GetAsync($"{baseUrl}/api/statystyki/podsumowanie");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Błąd", $"Błąd pobierania danych: {response.StatusCode}", "OK");
                return;
            }

            var result = JsonSerializer.Deserialize<SummaryResponse>(responseBody);
            if (result?.success == true)
            {
                SummaryLabel.Text = $"Suma całkowita: {result.summary.sumaCalkowita:F2} zł";
                TopVolunteersCollectionView.ItemsSource = result.topWolontariusze;
            }
            else
            {
                await DisplayAlertAsync("Błąd", "Nie udało się pobrać podsumowania.", "OK");
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

public class Summary
{
    public double sumaCalkowita { get; set; }
    // Dodaj inne właściwości jeśli potrzebne
}

public class TopVolunteer
{
    public string numerID { get; set; } = string.Empty;
    public string imie { get; set; } = string.Empty;
    public string nazwisko { get; set; } = string.Empty;
    public double suma { get; set; }
}

public class SummaryResponse
{
    public bool success { get; set; }
    public Summary summary { get; set; } = new Summary();
    public List<TopVolunteer> topWolontariusze { get; set; } = new List<TopVolunteer>();
}