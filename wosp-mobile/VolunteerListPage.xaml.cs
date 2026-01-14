using System.Text.Json;

namespace wosp_mobile;

public partial class VolunteerListPage : ContentPage
{
    private readonly HttpClient httpClient = new HttpClient();

    public VolunteerListPage()
    {
        InitializeComponent();
        LoadVolunteers();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadVolunteers();
    }

    private async void LoadVolunteers()
    {
        try
        {
            var token = Preferences.Get("auth_token", "");
            var baseUrl = Preferences.Get("base_url", "");
            var expiresAtStr = Preferences.Get("token_expires_at", "");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(baseUrl))
            {
                await DisplayAlertAsync("Błąd", "Brak tokenu lub URL. Zaloguj się ponownie.", "OK");
                await Shell.Current.GoToAsync("///LoginPage");
                return;
            }

            // Sprawdź czy token nie wygasł
            if (!string.IsNullOrEmpty(expiresAtStr))
            {
                if (DateTime.TryParse(expiresAtStr, out var expiresAt))
                {
                    if (expiresAt < DateTime.Now)
                    {
                        await DisplayAlertAsync("Sesja wygasła", "Zaloguj się ponownie.", "OK");
                        await Shell.Current.GoToAsync("///LoginPage");
                        return;
                    }
                }
            }

            httpClient.DefaultRequestHeaders.Remove("x-api-token");
            httpClient.DefaultRequestHeaders.Add("x-api-token", token);
            var response = await httpClient.GetAsync($"{baseUrl}/api/rozliczenia/listawolontariuszy");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Błąd", $"Błąd pobierania danych: {response.StatusCode}", "OK");
                return;
            }

            var result = JsonSerializer.Deserialize<ApiResponse<List<Volunteer>>>(responseBody);
            if (result?.success == true)
            {
                VolunteersCollectionView.ItemsSource = result.wolontariusze;
            }
            else
            {
                await DisplayAlertAsync("Błąd", "Nie udało się pobrać listy wolontariuszy.", "OK");
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

    private async void OnRozliczClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is int volunteerId)
        {
            await Shell.Current.GoToAsync($"///SettlementPage?volunteerId={volunteerId}");
        }
    }
}

public class Volunteer
{
    public int id { get; set; }
    public string numerID { get; set; } = string.Empty;
    public string imie { get; set; } = string.Empty;
    public string nazwisko { get; set; } = string.Empty;
    public int zaznaczony { get; set; } = 0;
}

public class ApiResponse<T>
{
    public bool success { get; set; }
    public T wolontariusze { get; set; } = default!;
}

public class IntToTextDecorationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int zaznaczony && zaznaczony == 1)
        {
            return TextDecorations.Underline;
        }
        return TextDecorations.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
