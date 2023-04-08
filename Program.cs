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

namespace _132
{

    public   class Program
    {
        private static StringBuilder stringBuilder = new StringBuilder();

        private static DiscordClient discord;     

        private static SpotifyClientConfig config = SpotifyClientConfig
          .CreateDefault()
          .WithAuthenticator(new ClientCredentialsAuthenticator("1eae436156fa4655b98928e9321bd845", "8bfd61271ae24d0398cfcfc61c647bee"));
        
        private static SpotifyClient spotify = new SpotifyClient(config);

        private static List<string> tracks = new List<string>();


        static DiscordClient Inicialization()
        {
            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = "MTA4ODA1ODkyOTIzNTM3ODE4Nw.GKm6_C.nLFUToqixvfuOdIXrKDYz4v1I32Ok8XZl3gxq4",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
            });
            return discord;
        }
        
        static async Task Main(string[] args)
        {

            Inicialization();

            var slash = discord.UseSlashCommands(new SlashCommandsConfiguration
            {
               Services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider()
            });

            slash.RegisterCommands<Empty>();
            slash.RegisterCommands<Empty>(1083380097718960259);
            slash.RegisterCommands<MusicSL>(1083380097718960259);


            discord.MessageCreated += async (s, e) =>
            {
                if (e.Message.Content.ToLower().StartsWith("ping"))
                    await e.Message.RespondAsync("pong!");
            };

            await discord.ConnectAsync();            
            discord.UseVoiceNext();

            await Task.Delay(-1);
        }


        public class Empty : ApplicationCommandModule { }


        public class MusicSL : ApplicationCommandModule
        {
            [SlashCommand("join", "join to a channel")]
            public async Task JoinCommand(InteractionContext ctx)
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
            public async Task PlayMusicCommand(InteractionContext ctx, [Option("number", "choose song's number")] double number = -1)
            {
                var builder = new DiscordWebhookBuilder();
                var fullPath = "";
                if (number == -1)
                {
                    fullPath = "https://pool.anison.fm:9000/AniSonFM(320)";
                }
                else
                {
                    string path = Sqlite(number);
                    fullPath = Path.GetFullPath(path);
                }
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var vnext = ctx.Client.GetVoiceNext();
                var connection = vnext.GetConnection(ctx.Guild);
                var channel = ctx.Member.VoiceState?.Channel;
                if (connection == null && channel != null)
                {
                    connection = await vnext.ConnectAsync(channel);
                    connection.VoiceReceived += VoiceReceiveHandler;
                }
                else if (connection == null && channel == null)
                {
                    builder = new DiscordWebhookBuilder().WithContent("You must be in a voice channel to use this command.");
                    await ctx.EditResponseAsync(builder);
                    return;
                }
                var transmit = connection.GetTransmitSink();
                builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(fullPath)}");
                await ctx.EditResponseAsync(builder);
                if (!connection.IsPlaying)
                    await ConvertAudioToPcmAsync(fullPath, transmit, ctx);
            }

            [SlashCommand("leave", "leave voice channel")]
            public async Task LeaveCommand(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var vnext = ctx.Client.GetVoiceNext();
                var connection = vnext.GetConnection(ctx.Guild);
                connection.Disconnect();
                var builder = new DiscordWebhookBuilder().WithContent("voice channel leaved");
                await ctx.EditResponseAsync(builder);
            }


            [SlashCommand("Turn_off", "Turn off sound")]
            public async Task PauseCommand(InteractionContext ctx)
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

                transmit.Pause();
                builder = new DiscordWebhookBuilder().WithContent("Done!");

                await ctx.EditResponseAsync(builder);
            }

            [SlashCommand("Turn_on", "Turn on sound")]
            public async Task ResumeCommand(InteractionContext ctx)
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

                await transmit.ResumeAsync();
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
            public async Task ShowCommand(InteractionContext ctx)
            {
                tracks.Clear();    
                
                tracks = ShowSongs();
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(BuildString(tracks)));
                
                tracks.Clear();
            }

            [SlashCommandGroup("spotify", "slash-commands for spotify")]
            public class GroupContainer : ApplicationCommandModule
            {
                [SlashCommand("search", "search popular songs")]
                public async Task SearchCommand(InteractionContext ctx, [Option("search_query", "enter the word")] string search)
                {
                    stringBuilder.Clear();
                    tracks.Clear();

                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    SearchRequest request = new SearchRequest(SearchRequest.Types.Track, search);
                    var searchResult = await spotify.Search.Item(request);
                    var e = searchResult.Tracks;
                    int counter = 1;
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
                public async Task FeaturesCommand(InteractionContext ctx, [Option("song_number", "song's number from search")] double number)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    var builder = new DiscordWebhookBuilder();

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

                    builder = new DiscordWebhookBuilder().WithContent(stringBuilder.Append($"Results for track: {number}\r\nAcousticness: {track.Acousticness} " +
                        $"\r\nLoudness: {track.Loudness}\r\nInstrumentalness: {track.Instrumentalness}" +
                        $"\r\nDanceability: {track.Danceability}\r\nLiveness: {track.Liveness}" +
                        $"\r\nDurationMs: {track.DurationMs}\r\nEnergy: {track.Energy}" +
                        $"\r\nKey: {track.Key}\r\nSpeechiness: {track.Speechiness}" +
                        $"\r\nTempo: {track.Tempo}").ToString());
                    tracks.Clear();
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
            List<string> songs = new List<string>();
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
        private static string Sqlite(double number)
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

        private static async Task ConvertAudioToPcmAsync(string filePath, VoiceTransmitSink output, InteractionContext ctx)
        {
            MediaFoundationReader reader = new MediaFoundationReader(filePath);
            using (reader)
            {
                var buffer = new byte[81920];
                int byteCount;
                while ((byteCount = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await output.WriteAsync(buffer, 0, byteCount);
                    
                    //await CheckMessage(output, ctx);
                }
            }
                                  
            reader.Close();
            output.Pause();
            output.Dispose();                                               
            
        }
    }
}
