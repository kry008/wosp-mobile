namespace wosp_mobile;

	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

		private async void OnVolunteerListClicked(object? sender, EventArgs e)
		{
			await Shell.Current.GoToAsync("///VolunteerListPage");
		}

		private async void OnStatisticsSummaryClicked(object? sender, EventArgs e)
		{
			await Shell.Current.GoToAsync("///StatisticsSummaryPage");
		}

		private async void OnStatisticsCountingClicked(object? sender, EventArgs e)
		{
			await Shell.Current.GoToAsync("///StatisticsCountingPage");
		}

		private async void OnStatisticsVolunteerClicked(object? sender, EventArgs e)
		{
			await Shell.Current.GoToAsync("///StatisticsVolunteerPage");
		}

		private async void OnLogoutClicked(object? sender, EventArgs e)
		{
			try
			{
				var token = Preferences.Get("auth_token", "");
				var baseUrl = Preferences.Get("base_url", "");

				if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(baseUrl))
				{
					var httpClient = new HttpClient();
					httpClient.DefaultRequestHeaders.Add("x-api-token", token);
					await httpClient.PostAsync($"{baseUrl}/api/logout", null);
				}

				// Wyczyść preferences
				Preferences.Remove("auth_token");
				Preferences.Remove("token_expires_at");
				Preferences.Remove("base_url");
				Preferences.Remove("username");

				await Shell.Current.GoToAsync("///LoginPage");
			}
			catch
			{
				// Nawet jeśli logout się nie powiedzie, przejdź do login
				await Shell.Current.GoToAsync("///LoginPage");
			}
		}
	}
