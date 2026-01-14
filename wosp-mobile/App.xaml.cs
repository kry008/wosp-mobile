using Microsoft.Extensions.DependencyInjection;

namespace wosp_mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	protected override async void OnStart()
	{
		base.OnStart();

		var authToken = Preferences.Get("auth_token", string.Empty);

		if (!string.IsNullOrEmpty(authToken))
		{
			try
			{
				using var client = new HttpClient();
				client.DefaultRequestHeaders.Add("x-api-token", authToken);

				var baseUrl = Preferences.Get("base_url", string.Empty);
				var response = await client.GetAsync($"{baseUrl}/api/rozliczenia/listawolontariuszy");

				if (response.IsSuccessStatusCode)
				{
					await Shell.Current.GoToAsync("///MainPage");
					return;
				}
				else
				{
					//wyczyść dane logowania
					Preferences.Remove("auth_token");
					Preferences.Remove("token_expires_at");
					Preferences.Remove("base_url");
					Preferences.Remove("username");
				}
			}
			catch
			{
				//wyczyść dane logowania, gdy błąd połączenia
				Preferences.Remove("auth_token");
				Preferences.Remove("token_expires_at");
				Preferences.Remove("base_url");
				Preferences.Remove("username");
			}
		}

		await Shell.Current.GoToAsync("///LoginPage");
	}

}