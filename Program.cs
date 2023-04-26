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
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using System.Security.Policy;

namespace _132
{
    class Config
    {
        public string? BotToken { get; set; }
        public ulong GuildId { get; set; }

    }

    public static class Program
    {
              

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
                    await e.Message.RespondAsync("лупа!");
            };

            await discord.ConnectAsync();
            discord.UseVoiceNext();

            await Task.Delay(-1);
        }


        public class Empty : ApplicationCommandModule { }


        public class MusicSL : ApplicationCommandModule
        {
            private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

            [SlashCommand("List", "List of all slash-commands")]
            public static async Task ListCommand(InteractionContext ctx)
            {
                await SendEmbed(ctx, DiscordColor.Aquamarine, "1.Help - find a bug?\r\n" +
                    "2.Control - Control music\r\n3.Join - Joining to a voice channel\r\n4.Play - Playing radio or song\r\n" +
                    "5.Leave - Leaving a voice channel\r\n6.Stop - Stop playing music\r\n" +
                    "7.Nightcore - Make nightcore or slow and reverb a song\r\n8.Show - show all downloaded songs\r\n" +
                    "9.Download - download audio track from YouTube");
            }

            [SlashCommand("Nightcore", "Make nightcore or slow and reverb a song")]
            public static async Task NightcoreCommand(InteractionContext ctx, [Option("link", "link from youtube")] string link, [Option("choose", "true: nightcore, false: slow and reverb")] bool choose)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var builder = new DiscordWebhookBuilder().WithContent("Good choice! Now downloading your song...");
                await ctx.EditResponseAsync(builder);

                
                await Download(link, 1);
                builder = new DiscordWebhookBuilder().WithContent("Your song has been downloaded! Now changing your song!");
                await ctx.EditResponseAsync(builder);

                string check1 = res.Data.Replace(" ","");
                          
                string acceleration;
                string tempo;
                string type = "";
                if (choose == true)
                {
                    type = "(nightcore)";
                    acceleration = "1.05";
                    tempo = "1.1";
                }
                else
                {
                    type = "(slowed)";
                    acceleration = "0.95";
                    tempo = "1";
                }
                string check2 = res.Data.Insert(res.Data.Length - 4, $"{type}").Replace(" ", "");
                var ffMpeg = new FFMpegConverter();
                ffMpeg.Invoke(
                    $@"-i {check1} -af aformat=sample_fmts=s16:sample_rates=44100,atempo={acceleration},asetrate=44100*{acceleration},atempo={tempo} -vn {check2}"                
                );
                File.Delete($"{check1}");
                Stream stream = new FileStream(check2, FileMode.Open, FileAccess.Read);
                builder = new DiscordWebhookBuilder().WithContent(":3").AddFile($"{check2}", stream);
                await ctx.EditResponseAsync(builder);
            }

            [SlashCommand("Help", "Find a bug?")]
            public static async Task HelpCommand(InteractionContext ctx)
            {
                await SendEmbed(ctx, DiscordColor.Magenta, "If you find a bug in the bot, you can contact its developers in telegram:\r\n@belugach4n\r\n@Sadfaded");                               
            }

            [SlashCommand("Join", "Join to a voice channel")]
            public static async Task JoinCommand(InteractionContext ctx)
            {
                DiscordChannel channel;
                try
                {
                    channel = ctx.Member.VoiceState.Channel;
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine(ex.Message);
                    await SendEmbed(ctx,DiscordColor.Orange, "You must be in a voice channel to use this command.");                    
                    return;
                }
                await SendEmbed(ctx, DiscordColor.Aquamarine, $"Joining {channel.Name}");             
                var vnext = ctx.Client.GetVoiceNext();
                var connection = await vnext.ConnectAsync(channel);
                connection.VoiceReceived += VoiceReceiveHandler;
            }


            [SlashCommand("Play", "Playing radio or song")]
            public static async Task PlayMusicCommand(InteractionContext ctx, [Option("url", "write youtube url")] string url = null,[Option("number", "choose song's number")] double number = -1)
            {
                if (url!= null)
                {
                    try
                    {
                        await PlayerControl.PlayMusic(ctx, url, cancelTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                        var builder = new DiscordWebhookBuilder().WithContent("incorrect url, please try again");
                        await ctx.EditResponseAsync(builder);
                    }
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    var builder = new DiscordWebhookBuilder();
                    string fullPath = "";
                    if (number == -1)
                    {
                        fullPath = "https://pool.anison.fm:9000/AniSonFM(320)";
                    }
                    else
                    {
                        string path = ReadSqlite(number);
                        fullPath = Path.GetFullPath(path);
                    }                  
                    try
                    {
                        builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(fullPath)}");
                        await ctx.EditResponseAsync(builder);
                        await PlayerControl.PlayMusic(ctx, fullPath, cancelTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        await SendEmbed(ctx, DiscordColor.Lilac, "incorrect song's index, please try again");
                    }
                }

                //if (string.IsNullOrEmpty(url))
                //{
                //    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                //    var builder = new DiscordWebhookBuilder().WithContent("Write a link");
                //    await ctx.EditResponseAsync(builder);
                //    return;
                //}

            }

            [SlashCommand("Leave", "Leave voice channel")]
            public static async Task LeaveCommand(InteractionContext ctx)
            {
                DiscordChannel channel = ctx.Member.VoiceState.Channel; ;
                try
                {
                    await SendEmbed(ctx, DiscordColor.Chartreuse, "voice channel leaved");
                    var vnext = ctx.Client.GetVoiceNext();
                    var connection = vnext.GetConnection(ctx.Guild);
                    connection.Disconnect();                                        
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    await SendEmbed(ctx, DiscordColor.IndianRed, $"something went wrong and I can't disconnect from {channel.Name}");
                }                
            }


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

                await SendEmbed(ctx, DiscordColor.HotPink, "Now you can control music", new DiscordComponent[] { buttonCompResume, buttonCompPause });

                discord.ComponentInteractionCreated += async (s, e) =>
                {
                    if (pause.CustomId == e.Id)
                    {
                        pause.PauseMusic(connection);
                    }
                    else if(resume.CustomId == e.Id)
                    {
                        resume.ResumeMusic(connection);
                    }
                    var builder = new DiscordInteractionResponseBuilder().WithContent("Done!");
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                };                                               
            }

            [SlashCommand("Stop", "stop radio or song")]
            public async Task StopCommand(InteractionContext ctx)
            {               
                var vnext = ctx.Client.GetVoiceNext();
                var connection = vnext.GetConnection(ctx.Guild);
                
                if (connection == null)
                {
                    await SendEmbed(ctx, DiscordColor.Chartreuse, "Nothing playing here");
                    return;
                }

                cancelTokenSource.Cancel();
                cancelTokenSource = new CancellationTokenSource();

                await SendEmbed(ctx, DiscordColor.Wheat, "stopped");

            }

            [SlashCommand("Show", "show all songs")]
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
                    await SendEmbed(ctx,DiscordColor.Aquamarine, "Something went wrong :(");
                }
                
                tracks.Clear();
            }


            [SlashCommand("Download", "download audio from youtube")]
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
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    await ctx.Channel.DeleteMessageAsync(message);
                    builder = new DiscordWebhookBuilder().WithContent($"Your download link is incorrect.\r\nlink examples: https://www.youtube.com/watch?v=p77-glF--GA&t=7s\r\nor https://music.youtube.com/watch?v=ZmJ5oBdJTXQ");
                    await ctx.EditResponseAsync(builder);                    
                }                                                
                                            
            }            

            private static async Task SendEmbed(InteractionContext ctx, DiscordColor color, string description, DiscordComponent[]? components = null, string? title = null)
            {
                var message = new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder
                {
                    Color = color,
                    Title = title,
                    Description = $"{description}",

                });
                if (components != null)
                {
                    message.AddComponents(components);                    
                }
                await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,message);
                //await ctx.Channel.SendMessageAsync(message);
            }

            private static async Task Download(string link, int check)
            {
                var ytdl = new YoutubeDL() { YoutubeDLPath = "yt-dlp.exe" };
                ytdl.FFmpegPath = "ffmpeg.exe";
                ytdl.OutputFolder = $"{Environment.CurrentDirectory}\\Music";
                res = await ytdl.RunAudioDownload(
                    link,
                    AudioConversionFormat.Wav
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