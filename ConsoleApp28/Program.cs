using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine(" Оберіть режим ");
        Console.WriteLine("1 - Курси валют (USD, EUR, GBP)");
        Console.WriteLine("2 - Погода (сьогодні + завтра для міста)");
        Console.WriteLine("3 - Гороскоп на сьогодні");
        Console.Write("Ваш вибір: ");
        var mode = Console.ReadLine();

        switch (mode)
        {
            case "2":
                await WeatherModule.RunAsync();
                break;
            case "3":
                await HoroscopeModule.RunAsync();
                break;
            default:
                await FxModule.RunAsync();
                break;
        }
    }
}
