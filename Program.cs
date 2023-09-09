using _132.init;

namespace _132
{
    public class Program
    {

        async Task Main()
        {
            Init init = new();
            await init.RunAsync();
        }
    }
}