using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RestSharp;

static class WeatherModule
{
    private class GeoResp { public List<GeoItem>? Results { get; set; } }
    private class GeoItem
    {
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Country { get; set; }
        public string? Timezone { get; set; }
    }

    private class ForecastResp
    {
        public string? Timezone { get; set; }
        public CurrentBlock? Current { get; set; }
        public DailyBlock? Daily { get; set; }
    }
    private class CurrentBlock
    {
        [JsonPropertyName("time")] public string? Time { get; set; }
        [JsonPropertyName("temperature_2m")] public double Temperature2m { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public int RelativeHumidity2m { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double WindSpeed10m { get; set; }
        [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
    }
    private class DailyBlock
    {
        public List<string>? Time { get; set; }
        [JsonPropertyName("weather_code")] public List<int>? WeatherCode { get; set; }
        [JsonPropertyName("temperature_2m_max")] public List<double>? TempMax { get; set; }
        [JsonPropertyName("temperature_2m_min")] public List<double>? TempMin { get; set; }
        [JsonPropertyName("relative_humidity_2m_max")] public List<int>? HumMax { get; set; }
        [JsonPropertyName("relative_humidity_2m_min")] public List<int>? HumMin { get; set; }
        [JsonPropertyName("wind_speed_10m_max")] public List<double>? WindMax { get; set; }
    }

    private class MarineResp { public MarineDaily? Daily { get; set; } }
    private class MarineDaily
    {
        public List<string>? Time { get; set; }
        [JsonPropertyName("sea_surface_temperature_max")] public List<double>? SstMax { get; set; }
        [JsonPropertyName("sea_surface_temperature_min")] public List<double>? SstMin { get; set; }
    }

    public static async Task RunAsync()
    {
        Console.WriteLine();
        Console.WriteLine("Погода сьогодні + завтра");
        Console.Write("Введіть місто: ");
        var city = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(city))
        {
            Console.WriteLine("Місто не введено");
            return;
        }

        try
        {
            var jsonOpt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var geoClient = new RestClient("https://geocoding-api.open-meteo.com");
            var geoReq = new RestRequest("v1/search", Method.Get)
                .AddQueryParameter("name", city)
                .AddQueryParameter("count", "1")
                .AddQueryParameter("language", "uk");
            var geoResp = await geoClient.ExecuteAsync(geoReq);

            if (!geoResp.IsSuccessful || string.IsNullOrWhiteSpace(geoResp.Content))
            {
                Console.WriteLine("Error");
                return;
            }

            var geo = JsonSerializer.Deserialize<GeoResp>(geoResp.Content!, jsonOpt);
            var place = geo?.Results?.FirstOrDefault();
            if (place == null)
            {
                Console.WriteLine("Місто не знайдено");
                return;
            }

            double lat = place.Latitude;
            double lon = place.Longitude;

            var wxClient = new RestClient("https://api.open-meteo.com");
            var wxReq = new RestRequest("v1/forecast", Method.Get)
                .AddQueryParameter("latitude", lat.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddQueryParameter("longitude", lon.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddQueryParameter("current", "temperature_2m,relative_humidity_2m,wind_speed_10m,weather_code")
                .AddQueryParameter("daily", "weather_code,temperature_2m_max,temperature_2m_min,relative_humidity_2m_max,relative_humidity_2m_min,wind_speed_10m_max")
                .AddQueryParameter("forecast_days", "2")
                .AddQueryParameter("timezone", "auto");
            var wxResp = await wxClient.ExecuteAsync(wxReq);

            if (!wxResp.IsSuccessful || string.IsNullOrWhiteSpace(wxResp.Content))
            {
                Console.WriteLine("Error");
                return;
            }

            var wx = JsonSerializer.Deserialize<ForecastResp>(wxResp.Content!, jsonOpt);
            if (wx?.Current == null || wx.Daily?.Time == null || wx.Daily.WeatherCode == null)
            {
                Console.WriteLine("Неповні дані прогнозу");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"{place.Name}, {place.Country}  (lat {lat:0.###}, lon {lon:0.###})   ТЗ: {wx.Timezone}");
            Console.WriteLine(new string('-', 60));

            // СЬОГОДНІ
            var todayCond = CodeToCondition(wx.Daily.WeatherCode.ElementAtOrDefault(0));
            var todayHumMin = wx.Daily.HumMin?.ElementAtOrDefault(0);
            var todayHumMax = wx.Daily.HumMax?.ElementAtOrDefault(0);
            var todayWindMax = wx.Daily.WindMax?.ElementAtOrDefault(0);
            var tMax = wx.Daily.TempMax?.ElementAtOrDefault(0);
            var tMin = wx.Daily.TempMin?.ElementAtOrDefault(0);

            Console.WriteLine("СЬОГОДНІ:");
            Console.WriteLine($"  Умови: {CodeToCondition(wx.Current.WeatherCode)} (добовий код: {todayCond})");
            Console.WriteLine($"  Темп зараз: {wx.Current.Temperature2m:0.#} °C  (мін/макс доби: {tMin:0.#} / {tMax:0.#} °C)");
            Console.WriteLine($"  Вологість зараз: {wx.Current.RelativeHumidity2m}%  (мін/макс доби: {todayHumMin}% / {todayHumMax}%)");
            Console.WriteLine($"  Вітер зараз: {wx.Current.WindSpeed10m:0.#} м/с  (макс за добу: {todayWindMax:0.#} м/с)");
            Console.WriteLine();

            // ЗАВТРА
            var tMax2 = wx.Daily.TempMax?.ElementAtOrDefault(1);
            var tMin2 = wx.Daily.TempMin?.ElementAtOrDefault(1);
            var humMin2 = wx.Daily.HumMin?.ElementAtOrDefault(1);
            var humMax2 = wx.Daily.HumMax?.ElementAtOrDefault(1);
            var windMax2 = wx.Daily.WindMax?.ElementAtOrDefault(1);
            var tomorrowCond = CodeToCondition(wx.Daily.WeatherCode.ElementAtOrDefault(1));

            Console.WriteLine("ЗАВТРА:");
            Console.WriteLine($"  Умови: {tomorrowCond}");
            Console.WriteLine($"  Темп: мін/макс {tMin2:0.#} / {tMax2:0.#} °C");
            Console.WriteLine($"  Вологість: {humMin2}% / {humMax2}%");
            Console.WriteLine($"  Вітер: до {windMax2:0.#} м/с");
            Console.WriteLine();

            var seaLat = 46.49;
            var seaLon = 30.73;
            var marineClient = new RestClient("https://marine-api.open-meteo.com");
            var marineReq = new RestRequest("v1/marine", Method.Get)
                .AddQueryParameter("latitude", seaLat.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddQueryParameter("longitude", seaLon.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AddQueryParameter("daily", "sea_surface_temperature_max,sea_surface_temperature_min")
                .AddQueryParameter("forecast_days", "2")
                .AddQueryParameter("timezone", "Europe/Kyiv");
            var marineResp = await marineClient.ExecuteAsync(marineReq);

            if (marineResp.IsSuccessful && !string.IsNullOrWhiteSpace(marineResp.Content))
            {
                var marine = JsonSerializer.Deserialize<MarineResp>(marineResp.Content!, jsonOpt);
                var sstTodayMin = marine?.Daily?.SstMin?.ElementAtOrDefault(0);
                var sstTodayMax = marine?.Daily?.SstMax?.ElementAtOrDefault(0);
                var sstTomorrowMin = marine?.Daily?.SstMin?.ElementAtOrDefault(1);
                var sstTomorrowMax = marine?.Daily?.SstMax?.ElementAtOrDefault(1);

                Console.WriteLine("Температура води в Чорному морі (район Одеси):");
                if (sstTodayMin.HasValue && sstTodayMax.HasValue)
                    Console.WriteLine($"  Сьогодні: {sstTodayMin:0.#}…{sstTodayMax:0.#} °C");
                if (sstTomorrowMin.HasValue && sstTomorrowMax.HasValue)
                    Console.WriteLine($"  Завтра:   {sstTomorrowMin:0.#}…{sstTomorrowMax:0.#} °C");
            }
            else
            {
                Console.WriteLine("API ERROR");
            }

            Console.WriteLine();
            Console.WriteLine("Готово.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Сталася помилка: {ex.Message}");
        }
    }

    private static string CodeToCondition(int code) => code switch
    {
        0 => "сонячно",
        1 or 2 or 3 => "хмарно",
        45 or 48 => "туман",
        51 or 53 or 55 or 56 or 57 => "мряка",
        61 or 63 or 65 or 80 or 81 or 82 => "дощ",
        66 or 67 => "крижаній дощ",
        71 or 73 or 75 or 77 or 85 or 86 => "сніг",
        95 or 96 or 99 => "гроза",
        _ => "невідомо"
    };
}
