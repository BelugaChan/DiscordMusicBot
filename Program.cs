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
                var connection = await vnext.ConnectAsync(channel);
                connection.VoiceReceived += VoiceReceiveHandler;
            }


            [SlashCommand("play", "playing radio or song")]
            public static async Task PlayMusicCommand(InteractionContext ctx, [Option("number", "choose song's number")] double number = -1)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var builder = new DiscordWebhookBuilder();
                var fullPath = "";
                try
                {
                    if (number == -1)
                    {
                        fullPath = "https://pool.anison.fm:9000/AniSonFM(320)";
                    }
                    else
                    {
                        string path = ReadSqlite(number);
                        fullPath = Path.GetFullPath(path);
                    }
                    await PlayerControl.PlayMusic(ctx, fullPath);
                }
                catch (Exception)
                {
                    builder = new DiscordWebhookBuilder().WithContent("incorrect song's index, please try again");
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


            [SlashCommand("Turn_off", "Turn off sound")]
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

            [SlashCommand("Turn_on", "Turn on sound")]
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

            [SlashCommand("show", "show all songs")]
            public static async Task ShowCommand(InteractionContext ctx)
            {
                tracks.Clear();

                try
                {
                    tracks = ShowSongs();
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(BuildString(tracks)));
                }
                catch (Exception)
                {
                    await ctx.Channel.SendMessageAsync("Something went wrong :(");
                }
                
                tracks.Clear();
            }


            [SlashCommand("download", "download audio from youtube")]
            public static async Task DownloadCommand(InteractionContext ctx, [Option("search_link", "enter your link")] string link)
            {
                DiscordMessage message = await ctx.Channel.SendMessageAsync("The download may take some time");
                var builder = new DiscordWebhookBuilder();
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var ytdl = new YoutubeDL() { YoutubeDLPath = "yt-dlp.exe" };
                ytdl.FFmpegPath = "ffmpeg.exe";
                ytdl.OutputFolder = $"{Environment.CurrentDirectory}\\Music";
                try
                {
                    var res = await ytdl.RunAudioDownload(
                    link,
                    AudioConversionFormat.Mp3
                    );
                    string songName = res.Data[42..^4];
                    AddSqlite(songName, res.Data);
                    builder = new DiscordWebhookBuilder().WithContent("Your song has been downloaded and added to the database!");
                    await ctx.EditResponseAsync(builder);
                }
                catch (Exception)
                {
                    await ctx.Channel.DeleteMessageAsync(message);
                    builder = new DiscordWebhookBuilder().WithContent($"Your download link is incorrect.\r\nlink examples: https://www.youtube.com/watch?v=p77-glF--GA&t=7s\r\nor https://music.youtube.com/watch?v=ZmJ5oBdJTXQ");
                    await ctx.EditResponseAsync(builder);                    
                }                                                
                                            
            }


            [SlashCommandGroup("spotify", "slash-commands for spotify")]
            public class GroupContainer : ApplicationCommandModule
            {
                [SlashCommand("search", "search popular songs")]
                public static async Task SearchCommand(InteractionContext ctx, [Option("search_query", "enter the word")] string search)
                {
                    stringBuilder.Clear();
                    tracks.Clear();

                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    SearchRequest request = new(SearchRequest.Types.Track, search);
                    var searchResult = await spotify.Search.Item(request);
                    var e = searchResult.Tracks;
                    int counter = 1;
                    stringBuilder.Append($"Search results for: {search}\r\n");
                    foreach (var item in e.Items)
                    {
                        stringBuilder.Append($"{counter};   {item.Artists.First().Name};   {item.Name};{item.Id}\r\n");
                        tracks.Add($"{counter}; {item.Artists.First().Name}; {item.Name};{item.Id}");
                        counter++;
                    }
                    var builder = new DiscordWebhookBuilder().WithContent(stringBuilder.ToString());
                    await ctx.EditResponseAsync(builder);

                }


                [SlashCommand("features", "show song's features")]
                public static async Task FeaturesCommand(InteractionContext ctx, [Option("song_number", "song's number from search")] double number)
                {
                    try
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                        stringBuilder.Clear();


                        string g = "";
                        foreach (var item in tracks)
                        {
                            string[] a = item.Split(';');
                            if (number == int.Parse(a[0]))
                            {
                                g = a[3];
                            }
                        }
                        var track = await spotify.Tracks.GetAudioFeatures(g);

                        var builder = new DiscordWebhookBuilder().WithContent(stringBuilder.Append($"Results for track: {number}\r\nAcousticness: {track.Acousticness} " +
                            $"\r\nLoudness: {track.Loudness}\r\nInstrumentalness: {track.Instrumentalness}" +
                            $"\r\nDanceability: {track.Danceability}\r\nLiveness: {track.Liveness}" +
                            $"\r\nDurationMs: {track.DurationMs}\r\nEnergy: {track.Energy}" +
                            $"\r\nKey: {track.Key}\r\nSpeechiness: {track.Speechiness}" +
                            $"\r\nTempo: {track.Tempo}").ToString());
                        tracks.Clear();
                        await ctx.EditResponseAsync(builder);
                    }
                    catch (Exception)
                    {
                        var builder = new DiscordWebhookBuilder().WithContent("At first, use command Spotify search");
                        await ctx.EditResponseAsync(builder);
                    }
                    

                }

            }


            private static async Task VoiceReceiveHandler(VoiceNextConnection connection, VoiceReceiveEventArgs args)
            {
                var transmit = connection.GetTransmitSink();
                await transmit.WriteAsync(args.PcmData);
            }

            //ф-ия для слэш-команды show, чтобы правильно отоброжались песни в сообщении
            public static string BuildString(List<string> list)
            {
                stringBuilder.Clear();
                list = ShowSongs();
                for (int i = 0; i < list.Count; i++)
                {
                    stringBuilder.Append($"{i + 1} {list[i]} \r\n");
                }
                return stringBuilder.ToString();
            }

            //считывание песен для слэш команды show
            public static List<string> ShowSongs()
            {
                List<string> songs = new();
                string cs = $"URI=file:{Path.GetFullPath(@"music.db")}";
                using var con = new SQLiteConnection(cs);
                con.Open();
                string stm = "SELECT * FROM music";
                using var cmd = new SQLiteCommand(stm, con);
                using SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    songs.Add(rdr.GetString(1));
                }
                return songs;
            }

            //считывание бд для слэш команды play
            private static string ReadSqlite(double number)
            {
                string e = "";
                string cs = $"URI=file:{Path.GetFullPath(@"music.db")}";
                using var con = new SQLiteConnection(cs);
                con.Open();
                string stm = "SELECT * FROM music";
                using var cmd = new SQLiteCommand(stm, con);
                using SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (rdr.GetInt32(0) == number)
                    {
                        e = rdr.GetString(2);
                        break;
                    }
                }
                return e;
            }

            private static void AddSqlite(string name, string path)
            {
                string cs = $"URI=file:{Path.GetFullPath(@"music.db")}";
                using var con = new SQLiteConnection(cs);
                con.Open();


                string query1 = "INSERT INTO music (id ,name, path) VALUES (@id, @name, @path)";
                var command1 = new SQLiteCommand(query1, con);
                
                string query2 = "SELECT COUNT(*) FROM music";
                var command2 = new SQLiteCommand(query2, con);
                int rowCount = Convert.ToInt32(command2.ExecuteScalar());
                
                command1.Parameters.AddWithValue("id", rowCount + 1);
                command1.Parameters.AddWithValue("name", name);
                command1.Parameters.AddWithValue("path", path);
                command1.ExecuteNonQuery();
                con.Close();

            }
        }
    }
}