using Microsoft.Extensions.Configuration;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using _132.SlashCommands;
using Microsoft.Extensions.DependencyInjection;

namespace _132.init
{
    internal class Init
    {

        private IConfiguration _config;

        public async Task RunAsync()
        {
            if (!File.Exists("config.json")) CreateDefaultConfig();

            _config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("config.json")
            .Build();


            DiscordClient _client = new(new DiscordConfiguration
            {
                Token = _config["Token"],
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
            });

            if (string.IsNullOrWhiteSpace(_config["Token"]))
            {
                Console.WriteLine("Token is missing in config.json. Please provide a token through the terminal.");
                return;
            }

            var slash = _client.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider()
            });

            slash.RegisterCommands<Empty>();
            slash.RegisterCommands<Empty>(botConfig.GuildId);
            slash.RegisterCommands<MusicSL>(botConfig.GuildId);

            _client.Ready += Client_Ready;

            await _client.ConnectAsync();

            _client.UseVoiceNext();
            await Task.Delay(-1);
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            Console.WriteLine($"Logged in as: {sender.CurrentUser.Username}");
            return Task.CompletedTask;
        }

        private void CreateDefaultConfig()
        {
            Console.WriteLine("Write your token");
            var token = Console.ReadLine();
            var defaultConfig = new
            {
                Token = token.ToString()
            };

            File.WriteAllText("config.json", 
                Newtonsoft.Json.JsonConvert.SerializeObject(defaultConfig, 
                Newtonsoft.Json.Formatting.Indented));
            Console.WriteLine("config.json created");
        }
    }
}
