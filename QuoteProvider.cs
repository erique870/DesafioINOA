using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace StockQuoteAlert;

// Faz requisições da cotação atual do ativo B3 informado, a API de cotação escolhida foi a brapi (https://brapi.dev/)
public class QuoteProvider
{
    private readonly HttpClient _httpClient;
    private readonly string? _token;

    public QuoteProvider(HttpClient httpClient, string? token)
    {
        _httpClient = httpClient;
        _token = token;    
    }

    // Retorna o preço atual (regularMarketPrice) do ativo informado
    public async Task<decimal> GetCurrentPriceAsync(string ticker)
    {
        string url = $"https://brapi.dev/api/quote/{Uri.EscapeDataString(ticker)}";
        
        // Suporte a uso de tokens customizados
        if(!string.IsNullOrWhiteSpace(_token))
        {
            url += $"?token={Uri.EscapeDataString(_token)}";
        }

        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BrapiQuoteResponse>();

        var result = payload?.Results?.FirstOrDefault();

        if ((result is null) || (result.RegularMarketPrice is null))
        {
            throw new InvalidOperationException(
                $"Não foi possível obter a cotação de \"{ticker}\". Verifique se o código do ativo foi digitado corretamente."
            );
        }

        return result.RegularMarketPrice.Value;
    }

    private class BrapiQuoteResponse
    {
        [JsonPropertyName("results")]
        public List<BrapiQuoteResult>? Results { get; set; }   
    }

    private class BrapiQuoteResult
    {
        [JsonPropertyName("regularMarketPrice")]
        public decimal? RegularMarketPrice { get; set; }
    }
}