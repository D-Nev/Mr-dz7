using System;
using System.Globalization;
using System.Threading.Tasks;

static class HoroscopeModule
{
    public static async Task RunAsync()
    {
        Console.WriteLine();
        Console.WriteLine("Гороскоп на сьогодні (за датою, без API)");
        Console.Write("Введіть дату (напр. 2025-10-19) або Enter для сьогодні: ");
        var s = Console.ReadLine();

        DateTime date;
        if (string.IsNullOrWhiteSpace(s))
        {
            date = DateTime.Today;
        }
        else
        {
            if (!DateTime.TryParse(s, new CultureInfo("uk-UA"), DateTimeStyles.AssumeLocal, out date) &&
                !DateTime.TryParse(s, new CultureInfo("ru-RU"), DateTimeStyles.AssumeLocal, out date) &&
                !DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
            {
                Console.WriteLine("Невірний формат дати.");
                return;
            }
        }

        int seed = date.Year * 10000 + date.Month * 100 + date.Day;
        var rng = new Random(seed);

        var moods = new[]
        {
            "енергійний", "спокійний", "натхненний", "рішучий",
            "мрійливий", "урівноважений", "впевнений", "дослідницький"
        };
        var advices = new[]
        {
            "Сконцентруйтеся на одному завданні.",
            "Приділіть час здоров'ю та сну.",
            "Варто довести справу до кінця.",
            "Плануйте день — це дасть перевагу.",
            "Будьте відкриті до нових ідей.",
            "Уникайте поспішних рішень.",
            "Попросіть поради в надійної людини.",
            "Невелика прогулянка покращить настрій."
        };
        var colors = new[]
        {
            "синій", "зелений", "жовтий", "помаранчевий",
            "чорний", "білий", "фіолетовий", "червоний"
        };
        var focuses = new[]
        {
            "навчання", "кар'єра", "здоров'я", "відносини",
            "фінанси", "творчість", "саморозвиток", "відпочинок"
        };

        string mood = moods[rng.Next(moods.Length)];
        string advice = advices[rng.Next(advices.Length)];
        string color = colors[rng.Next(colors.Length)];
        string focus = focuses[rng.Next(focuses.Length)];
        int luckyNumber = rng.Next(1, 100);
        string luckyTime = $"{rng.Next(6, 22)}:00";

        Console.WriteLine();
        Console.WriteLine($"Дата: {date:yyyy-MM-dd}");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"Сьогоднішній настрій: {mood}");
        Console.WriteLine($"Порада дня: {advice}");
        Console.WriteLine($"Сфера фокусу: {focus}");
        Console.WriteLine($"Щасливий колір: {color}");
        Console.WriteLine($"Щасливе число: {luckyNumber}");
        Console.WriteLine($"Щасливий час: {luckyTime}");
        Console.WriteLine();
        Console.WriteLine("Готово.");

        await Task.CompletedTask;
    }
}
