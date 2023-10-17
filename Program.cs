using _132.init;

namespace _132
{
    public class Program
    {
        static async Task Main()
        {
            Init init = new();
            await init.RunAsync();
        }
    }
}