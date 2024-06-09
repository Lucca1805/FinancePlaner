using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class CurrencyExchangeService
{
    private readonly HttpClient _client;
    private const string BaseUrl = "https://v6.exchangerate-api.com/v6/b4c68532d1d274149951189f/";

    public CurrencyExchangeService()
    {
        _client = new HttpClient();
    }

    public async Task<decimal> GetExchangeRate(string baseCurrency, string targetCurrency)
    {
        string url = $"{BaseUrl}pair/{baseCurrency}/{targetCurrency}";
        Console.WriteLine(url);
        try
        {
            HttpResponseMessage response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            var exchangeRates = JsonSerializer.Deserialize<ExchangeRateResponse>(responseBody);

            if (exchangeRates != null && exchangeRates.result == "success")
            {
                return exchangeRates.conversion_rate;
            }
            else
            {
                throw new Exception($"Exchange rate not found. {url}");
            }
        }
        catch (HttpRequestException e)
        {
            throw new Exception($"Error retrieving exchange rate: {e.Message}");
        }
    }


    public async Task<decimal> ConvertCurrency(decimal amount, string baseCurrency, string targetCurrency)
    {
        try
        {
            decimal exchangeRate = await GetExchangeRate(baseCurrency, targetCurrency);
            return amount * exchangeRate;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error converting currency: {ex.Message}");
        }
    }
}

public class ExchangeRateResponse
{
    public string result { get; set; }
    public string documentation { get; set; }
    public string terms_of_use { get; set; }
    public long time_last_update_unix { get; set; }
    public string time_last_update_utc { get; set; } // Change the type to string
    public long time_next_update_unix { get; set; }
    public string time_next_update_utc { get; set; } // Change the type to string
    public string base_code { get; set; }
    public string target_code { get; set; }
    public decimal conversion_rate { get; set; }
}
