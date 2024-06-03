using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinanceLiquidityManager.Handler.Currency
{
    public class CurrencyConverter
    {
        private readonly HttpClient _client;

        public CurrencyConverter()
        {
            _client = new HttpClient();
        }

        public async Task<decimal> ConvertCurrency(string fromCurrency, string toCurrency, double amount)
        {
            try
            {
                var url = $"https://openexchangerates.org/api/convert/{amount}/{fromCurrency}/{toCurrency}?app_id=bc13bdaca280473aac6b48c03dfa035f&prettyprint=false";
                var response = await _client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var json = JsonSerializer.Deserialize<ExchangeRateResponse>(responseBody, options);
                    decimal convertedAmount = Convert.ToDecimal(json.amount);
                    return convertedAmount;
                }
                else
                {
                    throw new HttpRequestException("Failed to retrieve exchange rate data");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred: {ex.Message}");
            }
        }

        private class ExchangeRateResponse
        {
            public double amount { get; set; }
        }
    }
}
