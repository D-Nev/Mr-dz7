using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RestSharp;

class Program
{
    private class RatesResponse
    {
        public bool? Success { get; set; }
        public string Base { get; set; } = "";
        public string Date { get; set; } = "";
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }

    private class ErApiResponse
    {
        public string Result { get; set; } = "";                     
        [JsonPropertyName("base_code")] public string BaseCode { get; set; } = "";
        [JsonPropertyName("time_last_update_utc")] public string TimeLastUpdateUtc { get; set; } = "";
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine(" Курси валют ");
        Console.Write("Введіть базову валюту: ");
        var input = Console.ReadLine();
        var baseCurrency = string.IsNullOrWhiteSpace(input) ? "UAH" : input.Trim().ToUpperInvariant();

        if (!Regex.IsMatch(baseCurrency, @"^[A-Z]{3}$"))
        {
            Console.WriteLine("Err");
            baseCurrency = "UAH";
        }

        var requested = new[] { "USD", "EUR", "GBP" };
        var symbols = requested.Where(s => !s.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (symbols.Length == 0)
        {
            Console.WriteLine("Немає що запитувати.");
            return;
        }

        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var hostClient = new RestClient("https://api.exchangerate.host");
            var hostReq = new RestRequest("latest", Method.Get)
                .AddQueryParameter("base", baseCurrency)
                .AddQueryParameter("symbols", string.Join(",", symbols));

            var hostResp = await hostClient.ExecuteAsync(hostReq);

            bool hostOk = hostResp.IsSuccessful && !string.IsNullOrWhiteSpace(hostResp.Content);
            if (hostOk)
            {
                var data = JsonSerializer.Deserialize<RatesResponse>(hostResp.Content!, jsonOptions);
                if (data?.Rates != null && data.Rates.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine($"База: {data.Base}   Дата: {data.Date}");
                    Console.WriteLine(new string('-', 40));
                    PrintRates(baseCurrency, symbols, data.Rates);
                    Console.WriteLine("Готово.");
                    return;
                }
            }

            Console.WriteLine("API");
            var erClient = new RestClient("https://open.er-api.com");
            var erReq = new RestRequest($"v6/latest/{baseCurrency}", Method.Get);
            var erResp = await erClient.ExecuteAsync(erReq);

            if (!erResp.IsSuccessful || string.IsNullOrWhiteSpace(erResp.Content))
            {
                Console.WriteLine($"API ERROR: {(int)erResp.StatusCode} {erResp.StatusDescription}");
                return;
            }

            var er = JsonSerializer.Deserialize<ErApiResponse>(erResp.Content!, jsonOptions);
            if (er == null || !string.Equals(er.Result, "success", StringComparison.OrdinalIgnoreCase) || er.Rates == null)
            {
                Console.WriteLine("API ERROR");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"База: {er.BaseCode}   Оновлено: {er.TimeLastUpdateUtc}");
            Console.WriteLine(new string('-', 40));
            PrintRates(baseCurrency, symbols, er.Rates);
            Console.WriteLine("Готово.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void PrintRates(string baseCurrency, string[] symbols, Dictionary<string, decimal> rates)
    {
        foreach (var sym in symbols)
        {
            if (!rates.TryGetValue(sym, out var rate))
            {
                Console.WriteLine($"{sym}: немає даних.");
                Console.WriteLine();
                continue;
            }

            string direct = $"1 {baseCurrency} = {rate:0.########} {sym}";
            string inverse = rate > 0m
                ? $"1 {sym} = {(1m / rate):0.####} {baseCurrency}"
                : "Зворотній курс недоступний (rate = 0)";

            Console.WriteLine(direct);
            Console.WriteLine(inverse);
            Console.WriteLine();
        }
    }
}
