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
using NReco.VideoConverter;
using System.Security.Policy;
using EmbedIO.Utilities;
using DSharpPlus.Interactivity;
using System;
using System.Reflection;
using System.Data.SqlTypes;

namespace _132
{
    class Config
    {
        public string? BotToken { get; set; }
        public ulong GuildId { get; set; }

    }

    public class Program
    {

        /*FROM mcr.microsoft.com/dotnet/runtime:7.0 AS build
        WORKDIR /src
        COPY 132.csproj .
        RUN dotnet restore "132.csproj"
        COPY . .
        RUN dotnet publish "132.csproj" -c Release -o /publish

        FROM mcr.microsoft.com/dotnet/runtime:7.0 AS final
        WORKDIR /app
        COPY --from=build /publish .

        ENTRYPOINT ["dotnet", "132.dll"]*/

        private static StringBuilder stringBuilder = new StringBuilder();

        private static DiscordClient discord;

        private static SpotifyClientConfig config = SpotifyClientConfig
          .CreateDefault()
          .WithAuthenticator(new ClientCredentialsAuthenticator("1eae436156fa4655b98928e9321bd845", "8bfd61271ae24d0398cfcfc61c647bee"));

        private static SpotifyClient spotify = new(config);

        private static List<string> tracks = new();

        private static RunResult<string> res;

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
                {
                    await e.Message.RespondAsync("лупа!");   
                }
            };

            await discord.ConnectAsync();
            discord.UseVoiceNext();           

            await Task.Delay(-1);
        }


        public class Empty : ApplicationCommandModule { }


        public class MusicSL : ApplicationCommandModule
        {
            private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

            private static PlayerControl playerControl = new();

            private static MusicQueue musicQueue = new MusicQueue();

            [SlashCommand("Nightcore", "Make nightcore or slow and reverb a song")]
            public static async Task NightcoreCommand(InteractionContext ctx, [Option("link", "link from youtube")] string link, [Option("choose", "true: nightcore, false: slow and reverb")] bool choose)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var builder = new DiscordWebhookBuilder().WithContent("Good choice! Now downloading your song...");
                await ctx.EditResponseAsync(builder);


                await Download(link, 1);
                builder = new DiscordWebhookBuilder().WithContent("Your song has been downloaded! Now changing your song!");
                await ctx.EditResponseAsync(builder);

                string check1 = res.Data.Replace(" ", "");
                string check2 = CorrectString(choose);
                string acceleration;
                string tempo;
                
                var ffMpeg = new FFMpegConverter();

                if (choose == true)
                {                   
                    acceleration = "1.05";
                    tempo = "1.1";                   
                }
                else
                {                    
                    acceleration = "0.95";
                    tempo = "0.95";                  
                }
                ffMpeg.Invoke($@"-i {check1} -af aformat=sample_fmts=s16:sample_rates=44100,atempo={acceleration},asetrate=44100*{acceleration},atempo={tempo} -vn {check2}");

                File.Delete($"{check1}");

                Stream stream = new FileStream(check2, FileMode.Open, FileAccess.Read);
                builder = new DiscordWebhookBuilder().WithContent(":3").AddFile($"{check2}", stream);
                await ctx.EditResponseAsync(builder);

            }

            [SlashCommandGroup("queue", "queue commands")]
            public class QueueGroupContainer : ApplicationCommandModule
            {
                [SlashCommand("Add", "add a song to the queue")]
                public static async Task AddQueueCommand(InteractionContext ctx, [Option("url", "write youtube url")] string url)
                {
                    await SendEmbed(ctx, DiscordColor.Magenta, "oke");
                    musicQueue.AddTrack(url);
                }

                [SlashCommand("Remove", "remove a song from the queue")]
                public static async Task RemoveQueueCommand(InteractionContext ctx, [Option("index", "song's index")] double index)
                {
                    try
                    {
                        musicQueue.RemoveTrack((int)index);
                        await SendEmbed(ctx, DiscordColor.Magenta, "yosh!");
                    }
                    catch (Exception ex)
                    {
                        await SendEmbed(ctx, DiscordColor.Aquamarine, "No tracks in the queue");
                        Console.WriteLine(ex.ToString());
                        return;
                    }
                                       
                }

                [SlashCommand("control", "control queue")]
                public static async Task QueueCommand(InteractionContext ctx)
                {                    

                    Button next = new(ButtonStyle.Primary, "next_button", "next");
                    var buttonCompNext = new DiscordButtonComponent(next.Style, next.CustomId, next.Label, false, next.Emoji);

                    Button show = new(ButtonStyle.Secondary, "show_button", "show");
                    var buttonCompShow = new DiscordButtonComponent(show.Style, show.CustomId, show.Label, false, show.Emoji);

                    await SendEmbed(ctx, DiscordColor.HotPink, "Now you can control queue", new DiscordComponent[] {buttonCompNext, buttonCompShow });

                    discord.ComponentInteractionCreated += async (s, e) =>
                    {
                        if (next.CustomId == e.Id)
                        {                          
                            try
                            {
                                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Done!"));

                                string a = musicQueue.GetNextTrack();
                                var url = new Uri(a);                               

                                cancelTokenSource.Cancel();
                                cancelTokenSource = new CancellationTokenSource();

                                playerControl.PlayMusic(ctx, url, cancelTokenSource.Token);

                            }
                            catch (Exception ex)
                            {
                                await SendEmbed(ctx, DiscordColor.Aquamarine, "No tracks in the queue");                                
                                Console.WriteLine(ex.ToString());
                            }                                                       
                        }
                        else if (show.CustomId == e.Id)
                        {
                            try
                            {
                                List<string> musictracks = musicQueue.ShowQueue();
                                string a = BuildString(musictracks, 0);

                                await e.Interaction.CreateResponseAsync(
                                InteractionResponseType.UpdateMessage,
                                new DiscordInteractionResponseBuilder()
                                .WithContent(a));
                            }
                            catch (Exception ex)
                            {
                                await SendEmbed(ctx, DiscordColor.Aquamarine, "No tracks in the queue");
                                Console.WriteLine(ex.ToString()); 
                            }
                        }
                    };

                }
            }          

            [SlashCommand("help", "find a bug?")]
            public static async Task HelpCommand(InteractionContext ctx)
            {
                await SendEmbed(ctx, DiscordColor.Magenta, "If you find a bug in the bot, you can contact its developers in telegram:\r\n@belugach4n\r\n@Sadfaded");
            }

            [SlashCommandGroup("music", "slash-commands for music")]
            public class MusicGroupContainer : ApplicationCommandModule
            {
                [SlashCommand("Control", "Control music")]
                public static async Task ControlCommand(InteractionContext ctx)
                {
                    var vnext = ctx.Client.GetVoiceNext();
                    var connection = vnext.GetConnection(ctx.Guild);
                    if (connection == null)
                    {
                        await SendEmbed(ctx, DiscordColor.Chartreuse, "Nothing playing here");
                        return;
                    }
                    Button pause = new(ButtonStyle.Danger, "pause_button", "pause");
                    var buttonCompPause = new DiscordButtonComponent(pause.Style, pause.CustomId, pause.Label, false, pause.Emoji);

                    Button resume = new(ButtonStyle.Success, "resume_button", "resume");
                    var buttonCompResume = new DiscordButtonComponent(resume.Style, resume.CustomId, resume.Label, false, resume.Emoji);

                    //Button loop = new(ButtonStyle.Primary, "loop_button", "loop");
                    //var buttonCompLoop = new DiscordButtonComponent(loop.Style, loop.CustomId, loop.Label, false, loop.Emoji);

                    await SendEmbed(ctx, DiscordColor.HotPink, "Now you can control music", new DiscordComponent[] { buttonCompResume, buttonCompPause/*, buttonCompLoop */});

                    discord.ComponentInteractionCreated += async (s, e) =>
                    {
                        if (pause.CustomId == e.Id)
                        {
                            playerControl.PauseMusic();
                        }
                        else if (resume.CustomId == e.Id)
                        {
                            playerControl.ResumeMusic();
                        }
                        //else if (loop.CustomId == e.Id)
                        //{
                        //    playerControl.LoopMusic(true);
                        //}
                        var builder = new DiscordInteractionResponseBuilder().WithContent("Done!");
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                        Thread.Sleep(2000);
                        await e.Interaction.DeleteOriginalResponseAsync();
                    };
                }

                [SlashCommand("join", "join to a channel")]
                public static async Task JoinCommand(InteractionContext ctx)
                {
                    DiscordChannel channel;
                    try
                    {
                        channel = ctx.Member.VoiceState.Channel;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        await SendEmbed(ctx, DiscordColor.Orange, "You must be in a voice channel to use this command.");
                        return;
                    }
                    await SendEmbed(ctx, DiscordColor.Aquamarine, $"Joining {channel.Name}");
                    var vnext = ctx.Client.GetVoiceNext();
                    var connection = await vnext.ConnectAsync(channel);
                    connection.VoiceReceived += VoiceReceiveHandler;
                }


                [SlashCommand("Play", "Playing radio or song")]
                public static async Task PlayMusicCommand(InteractionContext ctx,
                    [Option("url", "write youtube url")] string? urlString = null,
                    [Option("number", "choose song's number")] double number = -1)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    if (urlString != null)
                    {
                        try
                        {
                            var url = new Uri(urlString);
                            await playerControl.PlayMusic(ctx, url, cancelTokenSource.Token);
                            var builder = new DiscordWebhookBuilder().WithContent($"Now playing: {urlString}");
                            await ctx.EditResponseAsync(builder);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            await SendEmbed(ctx, DiscordColor.Aquamarine, "incorrect url, please try again");
                        }
                    }
                    else
                    {
                        var builder = new DiscordWebhookBuilder();
                        string fullPath = "";
                        try
                        {
                            if (number == -1)
                            {
                                fullPath = "https://pool.anison.fm:9000/AniSonFM(320)";
                                await playerControl.PlayMusic(ctx, fullPath, cancelTokenSource.Token);
                            }
                            else
                            {
                                await playerControl.PlayMusic(ctx, number, cancelTokenSource.Token);
                            }
                            builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(fullPath)}");
                            await ctx.EditResponseAsync(builder);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            await SendEmbed(ctx, DiscordColor.Lilac, "incorrect song's index, please try again");
                        }

                    }

                }

                [SlashCommand("leave", "leave voice channel")]
                public static async Task LeaveCommand(InteractionContext ctx)
                {
                    DiscordChannel channel = ctx.Member.VoiceState.Channel; ;
                    try
                    {
                        await SendEmbed(ctx, DiscordColor.Chartreuse, "voice channel leaved");
                        var vnext = ctx.Client.GetVoiceNext();
                        var connection = vnext.GetConnection(ctx.Guild);
                        connection.Disconnect();
                        playerControl.audioTransmit.Closing();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        await SendEmbed(ctx, DiscordColor.IndianRed, $"something went wrong and I can't disconnect from {channel.Name}");
                    }
                }

                [SlashCommand("stop", "stop radio or song")]
                public static async Task StopCommand(InteractionContext ctx)
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

                    cancelTokenSource.Cancel();
                    cancelTokenSource = new CancellationTokenSource();
                    playerControl.StopMusic();

                    builder = new DiscordWebhookBuilder().WithContent("stopped");
                    await ctx.EditResponseAsync(builder);

                }

                //[SlashCommand("loop", "loop song")]
                //public static async Task LoopCommand(InteractionContext ctx, [Option("bool", "true or false")] bool looped = false)
                //{
                //    var builder = new DiscordWebhookBuilder();
                //    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                //    playerControl.LoopMusic(looped);

                //    builder = new DiscordWebhookBuilder().WithContent($"loop is {looped}");
                //    await ctx.EditResponseAsync(builder);
                //}

                [SlashCommand("show", "show all songs")]
                public static async Task ShowCommand(InteractionContext ctx)
                {
                    tracks.Clear();

                    try
                    {
                        tracks = ShowSongs();
                        await SendEmbed(ctx, DiscordColor.Aquamarine, BuildString(tracks));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        await SendEmbed(ctx, DiscordColor.Aquamarine, "Something went wrong :(");
                    }

                    tracks.Clear();
                }


                [SlashCommand("download", "download audio from youtube")]
                public static async Task DownloadCommand(InteractionContext ctx, [Option("search_link", "enter your link")] string link)
                {
                    DiscordMessage message = await ctx.Channel.SendMessageAsync("The download may take some time");
                    var builder = new DiscordWebhookBuilder();
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    try
                    {
                        await Download(link, 0);
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
            }
            
            [SlashCommandGroup("spotify", "slash-commands for spotify")]
            public class SpotifyGroupContainer : ApplicationCommandModule
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

            private static string CorrectString(bool choose)
            {
                string type = "";
                if (choose == true)
                {
                    type = "(nightcore)";
                }
                else
                {
                    type = "(slowed)";
                }
                string check2 = res.Data.Insert(res.Data.Length - 4, $"{type}").Replace(" ", "");
                return check2;
            }

            private static async Task SendEmbed(InteractionContext ctx, DiscordColor color, string description,  DiscordComponent[]? components = null)
            {
                var message = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder
                {
                    Color = color,
                    Description = $"{description}", 
                });
                if (components != null)
                {
                    message.AddComponents(components);
                }
                await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, message);
            }

            private static async Task Download(string link, int check)
            {
                var ytdl = new YoutubeDL() { YoutubeDLPath = "yt-dlp.exe" };
                ytdl.FFmpegPath = "ffmpeg.exe";
                ytdl.OutputFolder = $"{Environment.CurrentDirectory}\\Music";
                res = await ytdl.RunAudioDownload(
                    link,
                    AudioConversionFormat.Mp3
                    );
                File.Move(res.Data, res.Data.Replace(" ", ""));
                if (check == 0)
                {
                    string songName = res.Data[42..^4];
                    AddSqlite(songName, res.Data);
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

            public static string BuildString(List<string> list, int j)
            {
                stringBuilder.Clear();
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