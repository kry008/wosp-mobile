using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace wosp_mobile;

public partial class LoginPage : ContentPage
{
	private bool isCameraActive = false;
	private readonly HttpClient httpClient;
	private CameraBarcodeReaderView? barcodeReaderView;
	private bool isProcessingBarcode = false;
	private Image? originalImage;

	public LoginPage()
	{
		InitializeComponent();
		httpClient = new HttpClient();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		
		// Zapisz referencję do oryginalnego obrazu
		if (originalImage == null)
		{
			originalImage = CameraImage;
		}
		
		// Ukryj kamerę na komputerach (Windows, Mac), na razie błędy posiada, ukryj całkowicie
		if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
		{
			var frame = (Frame)CameraImage.Parent;
			frame.IsVisible = false;
			CameraButton.IsVisible = false;
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		
		// Wyłącz kamerę i zwolnij zasoby przy opuszczaniu strony
		if (isCameraActive)
		{
			DisposeCameraResources();
		}
	}

	private void DisposeCameraResources()
	{
		try
		{
			if (barcodeReaderView != null)
			{
				barcodeReaderView.BarcodesDetected -= OnBarcodesDetected;
				
				// Przywróć obraz zastępczy
				var frame = (Frame)barcodeReaderView.Parent;
				if (frame != null && originalImage != null)
				{
					frame.Content = new Image 
					{ 
						Source = originalImage.Source,
						Aspect = Aspect.AspectFill
					};
				}
				
				barcodeReaderView = null;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Błąd podczas czyszczenia zasobów kamery: {ex.Message}");
		}
		
		isCameraActive = false;
		isProcessingBarcode = false;
	}

	private async void OnCameraButtonClicked(object sender, EventArgs e)
	{
		if (!isCameraActive)
		{
			// Włącz kamerę i skanowanie QR
			isCameraActive = true;
			isProcessingBarcode = false;
			CameraButton.Text = "Wyłącz kamerę";
			
			try
			{
				// Sprawdź uprawnienia kamery
				var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
				if (status != PermissionStatus.Granted)
				{
					status = await Permissions.RequestAsync<Permissions.Camera>();
					if (status != PermissionStatus.Granted)
					{
						await DisplayAlertAsync("Brak uprawnień", "Aplikacja potrzebuje dostępu do kamery aby skanować kody QR", "OK");
						isCameraActive = false;
						CameraButton.Text = "Włącz kamerę";
						return;
					}
				}

				// Utwórz i skonfiguruj czytnik kodów kreskowych
				barcodeReaderView = new CameraBarcodeReaderView
				{
					Options = new BarcodeReaderOptions
					{
						Formats = BarcodeFormats.OneDimensional | BarcodeFormats.TwoDimensional,
						AutoRotate = true,
						Multiple = false
					}
				};

				barcodeReaderView.BarcodesDetected += OnBarcodesDetected;

				// Zamień zawartość ramki na czytnik
				var frame = (Frame)CameraImage.Parent;
				frame.Content = barcodeReaderView;
			}
			catch (Exception ex)
			{
				await DisplayAlertAsync("Błąd", $"Nie udało się uruchomić kamery: {ex.Message}", "OK");
				DisposeCameraResources();
				CameraButton.Text = "Włącz kamerę";
			}
		}
		else
		{
			// Wyłącz kamerę
			DisposeCameraResources();
			CameraButton.Text = "Włącz kamerę";
		}
	}

	private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
	{
		// Zabezpieczenie przed wielokrotnym przetwarzaniem
		if (isProcessingBarcode)
			return;

		// Pobierz pierwszy wykryty kod
		var barcode = e.Results.FirstOrDefault();
		if (barcode == null || string.IsNullOrWhiteSpace(barcode.Value))
			return;

		// Ustaw flagę przetwarzania
		isProcessingBarcode = true;

		// Parsuj kod QR na głównym wątku
		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			try
			{
				await ParseQRCode(barcode.Value);
				
				// Wyłącz kamerę po udanym skanowaniu
				DisposeCameraResources();
				CameraButton.Text = "Włącz kamerę";
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Błąd podczas przetwarzania kodu QR: {ex.Message}");
				// Pozwól na ponowne skanowanie przy błędzie
				isProcessingBarcode = false;
			}
		});
	}

	private async Task ParseQRCode(string qrData)
	{
		try
		{
			var data = JsonSerializer.Deserialize<QRCodeData>(qrData);
			if (data != null)
			{
				UrlEntry.Text = data.Url;
				LoginEntry.Text = data.User;
			}
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Błąd", $"Nieprawidłowy format kodu QR: {ex.Message}", "OK");
		}
	}

	private async void OnLoginButtonClicked(object sender, EventArgs e)
	{
		// Walidacja pól
		if (string.IsNullOrWhiteSpace(UrlEntry.Text))
		{
			await ShowError("Wprowadź adres pomocnika");
			return;
		}

		if (string.IsNullOrWhiteSpace(LoginEntry.Text))
		{
			await ShowError("Wprowadź login");
			return;
		}

		if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
		{
			await ShowError("Wprowadź hasło");
			return;
		}

		// Wyłącz przycisk podczas logowania
		LoginButton.IsEnabled = false;
		StatusLabel.Text = "Łączenie...";
		StatusLabel.TextColor = Colors.Gray;
		StatusLabel.IsVisible = true;

		try
		{
			var baseUrl = UrlEntry.Text.TrimEnd('/');
			System.Diagnostics.Debug.WriteLine($"Próba połączenia z: {baseUrl}");
			
			// Sprawdź czy URL/api jest dostępny
			var apiCheckResponse = await httpClient.GetAsync($"{baseUrl}/api");
			System.Diagnostics.Debug.WriteLine($"Status odpowiedzi API: {apiCheckResponse.StatusCode}");
			
			if (apiCheckResponse.StatusCode != System.Net.HttpStatusCode.OK && 
			    apiCheckResponse.StatusCode != System.Net.HttpStatusCode.Created)
			{
				await ShowError($"Nie można połączyć się z serwerem. Status: {apiCheckResponse.StatusCode}");
				return;
			}

			// Połączono z serwerem pomyślnie
			StatusLabel.Text = "Połączono z serwerem pomyślnie";
			StatusLabel.TextColor = Colors.Green;
			await Task.Delay(500);

			// Zaloguj użytkownika
			var loginData = new
			{
				login = LoginEntry.Text,
				haslo = PasswordEntry.Text
			};

			var jsonContent = JsonSerializer.Serialize(loginData);
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			var loginResponse = await httpClient.PostAsync($"{baseUrl}/api/login", content);
			var responseBody = await loginResponse.Content.ReadAsStringAsync();

			if (!loginResponse.IsSuccessStatusCode)
			{
				await DisplayAlertAsync("Błąd", $"Logowanie nieudane: {loginResponse.StatusCode}", "OK");
				return;
			}

			var loginResult = JsonSerializer.Deserialize<LoginResponse>(responseBody);

			if (loginResult == null || !loginResult.success)
			{
				await DisplayAlertAsync("Błąd", "Logowanie nieudane. Sprawdź dane i spróbuj ponownie.", "OK");
				return;
			}
            //Pokaż token
            //await DisplayAlertAsync("Zalogowano", $"Token: {loginResult.token}", "OK");
			// Zapisz token w Preferences
			Preferences.Set("auth_token", loginResult.token);
			Preferences.Set("token_expires_at", loginResult.expiresAt);
			Preferences.Set("base_url", baseUrl);
			Preferences.Set("username", LoginEntry.Text);

			// Sukces
			StatusLabel.Text = "Zalogowano pomyślnie!";
			StatusLabel.TextColor = Colors.Green;

			await Task.Delay(500);
			// Przejdź do głównej strony aplikacji
			await Shell.Current.GoToAsync("///MainPage");
            
		}
		catch (HttpRequestException ex)
		{
			System.Diagnostics.Debug.WriteLine($"HttpRequestException: {ex.Message}");
			await DisplayAlertAsync("Błąd", $"Błąd połączenia: {ex.Message}", "OK");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
			await DisplayAlertAsync("Błąd", $"Błąd: {ex.Message}", "OK");
		}
		finally
		{
			LoginButton.IsEnabled = true;
		}
	}

	private async Task ShowError(string message)
	{
		StatusLabel.Text = message;
		StatusLabel.TextColor = Colors.Red;
		StatusLabel.IsVisible = true;
		await Task.Delay(3000);
		StatusLabel.IsVisible = false;
	}

	// Klasy pomocnicze
	private class QRCodeData
	{
		[JsonPropertyName("url")]
		public string Url { get; set; } = string.Empty;
		
		[JsonPropertyName("user")]
		public string User { get; set; } = string.Empty;
	}

	private class LoginResponse
	{
		public bool success { get; set; }
		public string token { get; set; } = string.Empty;
		public string expiresAt { get; set; } = string.Empty;
	}
}
