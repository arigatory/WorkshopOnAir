namespace WorkshopOnAir;

public static class Program
{
    public static async Task Main()
    {
        var httpClient = new HttpClient();

        string data = await httpClient.GetStringAsync("http://msudotnet.ru/");
        if (data.Length > 0)
        {
            Console.WriteLine(data.Substring(0,500));
        }
    }

}