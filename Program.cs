using NAudio;
using System.Data.SQLite;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.VoiceNext.EventArgs;
using NAudio.Wave;
using System.Linq;
using System.IO;
using System.Text;
using System.Data;
using DSharpPlus.EventArgs;
using Microsoft.VisualBasic;
using System.Reflection.PortableExecutable;
using System.Collections.Specialized;
using System.Threading.Channels;
using System.Threading;
using System.Collections.Generic;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using _132.PlayerController;
using Newtonsoft.Json;
using DSharpPlus.CommandsNext;
using YoutubeDLSharp.Options;
using YoutubeDLSharp;

namespace _132
{
    class Config
    {
        public string? BotToken { get; set; }
        public ulong GuildId { get; set; }

    }

    public class Program
    {
        private static StringBuilder stringBuilder = new StringBuilder();

        private static DiscordClient discord;

        private static SpotifyClientConfig config = SpotifyClientConfig
          .CreateDefault()
          .WithAuthenticator(new ClientCredentialsAuthenticator("1eae436156fa4655b98928e9321bd845", "8bfd61271ae24d0398cfcfc61c647bee"));

        private static SpotifyClient spotify = new(config);

        private static List<string> tracks = new();
        internal static Config botConfig
        {
            get
            {
                try
                {
                    string tempConfig = File.ReadAllText("config.json");
                    Config? _botConfig = JsonConvert.DeserializeObject<Config>(tempConfig);
                    if (_botConfig != null) return _botConfig;
                    else
                    {
                        Console.WriteLine("Config not founded");
                        Environment.Exit(1);
                        return null;
                    }
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("File not founded");
                    Environment.Exit(1);
                    return null;
                }
            }
        }

        static DiscordClient Inicialization()
        {

            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = botConfig.BotToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
            });
            return discord;
        }

        static async Task Main()
        {

            Inicialization();

            var slash = discord.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider()
            });

            slash.RegisterCommands<Empty>();
            slash.RegisterCommands<Empty>(botConfig.GuildId);
            slash.RegisterCommands<MusicSL>(botConfig.GuildId);


            discord.MessageCreated += async (s, e) =>
            {
                if (e.Message.Content.ToLower().StartsWith("пупа"))
                    await e.Message.RespondAsync("лупа!");
            };

            await discord.ConnectAsync();
            discord.UseVoiceNext();

            await Task.Delay(-1);
        }


        public class Empty : ApplicationCommandModule { }


        public class MusicSL : ApplicationCommandModule
        {
            [SlashCommand("help", "find a bug?")]
            public static async Task HelpCommand(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var builder = new DiscordWebhookBuilder().WithContent("If you find a bug in the bot, you can contact its developers in telegram:\r\n@belugach4n\r\n@Sadfaded");
                await ctx.EditResponseAsync(builder);
                
            }


                [SlashCommand("join", "join to a channel")]
            public static async Task JoinCommand(InteractionContext ctx)
            {
                DiscordChannel channel;
                try
                {
                    channel = ctx.Member.VoiceState.Channel;
                }
                catch (NullReferenceException)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You must be in a voice channel to use this command."));
                    return;
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Joining {channel.Name}"));
                var vnext = ctx.Client.GetVoiceNext();
                await vnext.ConnectAsync(channel);
            }


            [SlashCommand("play", "playing radio or song")]
            public static async Task PlayMusicCommand(InteractionContext ctx, [Option("url", "write youtube url")] string url = null)
            {
                try
                {
                    if(string.IsNullOrEmpty(url))
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                        var builder = new DiscordWebhookBuilder().WithContent("Write a link");
                        await ctx.EditResponseAsync(builder);
                        return;
                    }

                    else
                    {
                        var fullPath = url;
                        await PlayerControl.PlayMusic(ctx, fullPath);
                    }

                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    var builder = new DiscordWebhookBuilder().WithContent("incorrect song's index, please try again");
                    await ctx.EditResponseAsync(builder);
                }
                
                
            }

            [SlashCommand("leave", "leave voice channel")]
            public static async Task LeaveCommand(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var vnext = ctx.Client.GetVoiceNext();
                var connection = vnext.GetConnection(ctx.Guild);
                connection.Disconnect();
                var builder = new DiscordWebhookBuilder().WithContent("voice channel leaved");
                await ctx.EditResponseAsync(builder);
            }


            [SlashCommand("Pause", "Turn off sound")]
            public static async Task PauseCommand(InteractionContext ctx)
            {
                var builder = new DiscordWebhookBuilder();
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var vnext = ctx.Client.GetVoiceNext();
                var connection = vnext.GetConnection(ctx.Guild);
                if (connection == null)
                {
                    builder = new DiscordWebhookBuilder().WithContent("Nothing playing here");
                    await ctx.EditResponseAsync(builder);
                    return;
                }

                PlayerControl.PauseMusic(connection);

                builder = new DiscordWebhookBuilder().WithContent("Done!");
                await ctx.EditResponseAsync(builder);
            }

            [SlashCommand("Resume", "Turn on sound")]
            public static async Task ResumeCommand(InteractionContext ctx)
            {
                var builder = new DiscordWebhookBuilder();
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var vnext = ctx.Client.GetVoiceNext();
                var connection = vnext.GetConnection(ctx.Guild);
                if (connection == null)
                {
                    builder = new DiscordWebhookBuilder().WithContent("Nothing playing here");
                    await ctx.EditResponseAsync(builder);
                    return;
                }

                PlayerControl.ResumeMusic(connection);

                builder = new DiscordWebhookBuilder().WithContent("Done!");
                await ctx.EditResponseAsync(builder);

            }

            [SlashCommand("stop", "stop radio or song")]
            public async Task StopCommand(InteractionContext ctx)
            {
                var builder = new DiscordWebhookBuilder();
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var vnext = ctx.Client.GetVoiceNext();
                var connection = vnext.GetConnection(ctx.Guild);
                if (connection == null)
                {
                    builder = new DiscordWebhookBuilder().WithContent("Nothing playing here");
                    await ctx.EditResponseAsync(builder);
                    return;
                }

                var transmit = connection.GetTransmitSink();

                transmit.Dispose();
                builder = new DiscordWebhookBuilder().WithContent("stopped");
                await ctx.EditResponseAsync(builder);

            }
        }
    }
}