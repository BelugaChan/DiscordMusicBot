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


namespace _132
{

    public class Program
    {
        private static DiscordClient discord;

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
        //private readonly DiscordScreenSharingService _screenSharingService;
        static async Task Main(string[] args)
        {
            //discord = new DiscordClient(new DiscordConfiguration()
            //{
            //    Token = "MTA4ODA1ODkyOTIzNTM3ODE4Nw.GKm6_C.nLFUToqixvfuOdIXrKDYz4v1I32Ok8XZl3gxq4",
            //    TokenType = TokenType.Bot,
            //    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
            //});

            Inicialization();
            //регистрация слэш-команд
            var slash = discord.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider()
            });
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

        //пошли команды
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

            //Если в Option -1 то бот будет играть радио, если задать номер песни, то песню
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
                    //string path = @"Music\Luna.mp3";                    
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
                    //await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You must be in a voice channel to use this command."));
                    return;
                }
                var transmit = connection.GetTransmitSink();
                builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(fullPath)}");
                await ctx.EditResponseAsync(builder);

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

            [SlashCommand("show", "show all songs")]
            public async Task ShowCommand(InteractionContext ctx)
            {
                //await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                List<string> list = ShowSongs();
                //var builder = new DiscordWebhookBuilder().WithContent("Here they are");
                //await ctx.EditResponseAsync(builder);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(BuildString(list)));
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
            StringBuilder stringBuilder = new StringBuilder();
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
            string cs = @"URI=file:D:\Programming\132\bin\Debug\net6.0\music.db";
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
            string cs = @"URI=file:D:\Programming\132\bin\Debug\net6.0\music.db";
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

        public static async Task CheckMessage(VoiceTransmitSink output, InteractionContext ctx)
        {
            DiscordGuild guild = discord.GetGuildAsync(1083380097718960259).Result;

            // Получение объекта канала по его ID
            DiscordChannel channel = guild.GetChannel(1083380097718960262);
            //await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            //var builder = new DiscordWebhookBuilder(); ;
            //Inicialization();            
            //discord.MessageCreated += async (s, e) =>
            //{

            //    if (e.Message.Content.ToLower().StartsWith("!stop"))
            //    {
            //        output.Pause();
            //        builder = new DiscordWebhookBuilder().WithContent("song stopped!");
            //    }
            //    else if (e.Message.Content.ToLower().StartsWith("!continue"))
            //    {
            //        await output.ResumeAsync();
            //        builder = new DiscordWebhookBuilder().WithContent("song continued!");
            //    }
            //};
            CancellationToken cancellationToken = CancellationToken.None;
            while (!cancellationToken.IsCancellationRequested)
            {
                // получаем список сообщений в канале
                var messages = await channel.GetMessagesAsync();

                // проверяем наличие сообщений от пользователей
                var userMessages = messages.Where(m => !m.Author.IsBot).ToList();

                //if (userMessages.Where(t => t.Content.Contains("")))
                //{
                //    output.Pause();
                //}
                await Task.Delay(5000, cancellationToken);
            }
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

        //барахло на всякий случай

        //static SQLiteCommand GetCommand()
        //{
        //    string cs = @"URI=file:D:\Programming\132\bin\Debug\net6.0\files.db";
        //    using var con = new SQLiteConnection(cs);
        //    con.Open();
        //    string stm = "SELECT * FROM files";
        //    using var cmd = new SQLiteCommand(stm, con);
        //    return cmd;
        //}

        //sqlite for random music
        //private static string Sqlite()
        //{
        //    string e = "";
        //    Random rnd = new Random();
        //    string cs = @"URI=file:D:\Programming\132\bin\Debug\net6.0\files.db";
        //    using var con = new SQLiteConnection(cs);
        //    con.Open();
        //    string stm = "SELECT * FROM files";
        //    using var cmd = new SQLiteCommand(stm, con);
        //    using SQLiteDataReader rdr = cmd.ExecuteReader();
        //    int a = rnd.Next(rdr.FieldCount);
        //    while (rdr.Read())
        //    {
        //        if (rdr.GetInt32(0) == a)
        //        {
        //            e = rdr.GetString(2);
        //            break;
        //        }
        //    }
        //    return e;
        //}




        //        //static void Main(string[] args)
        //        //{
        //        //    string e = "";
        //        //    Random rnd = new Random();
        //        //    string cs = @"URI=file:D:\Programming\132\bin\Debug\net6.0\files.db"; 
        //        //    using var con = new SQLiteConnection(cs); 
        //        //    con.Open(); 
        //        //    string stm = "SELECT * FROM files"; 
        //        //    using var cmd = new SQLiteCommand(stm, con); 
        //        //    using SQLiteDataReader rdr = cmd.ExecuteReader();
        //        //    int a = rnd.Next(rdr.FieldCount);
        //        //    while (rdr.Read()) 
        //        //    {
        //        //        if (rdr.GetInt32(0) == a)
        //        //        {
        //        //            e = rdr.GetString(2);
        //        //            break;
        //        //        } 
        //        //    }

        //        //    //var waveOut = new WaveOutEvent();
        //        //    //MP3Stream stream = new MP3Stream(e);
        //        //    //WaveFormat waveFormat = new WaveFormat(stream.Frequency, stream.ChannelCount);
        //        //    //FastWaveBuffer fastWaveBuffer = new FastWaveBuffer(waveFormat, (int)stream.Length);
        //        //    //stream.CopyTo(fastWaveBuffer);
        //        //    //fastWaveBuffer.Seek(0, SeekOrigin.Begin);
        //        //    //waveOut.Init(fastWaveBuffer);
        //        //    //waveOut.Play();
        //        //    //Console.ReadKey();
        //        //    //string e = "";
        //        //    //string fileName = "files.db";
        //        //    //SQLiteConnection connection = new SQLiteConnection("Data Source=files.db; Version=3;");
        //        //    //using (connection)
        //        //    //{

        //        //    //    connection.Open();
        //        //    //    SQLiteCommand selectCMD = connection.CreateCommand();
        //        //    //    using (selectCMD)
        //        //    //    {
        //        //    //        selectCMD.CommandText = "SELECT * FROM files";
        //        //    //        selectCMD.CommandType = CommandType.Text;
        //        //    //    }
        //        //    //    SQLiteDataReader reader = selectCMD.ExecuteReader();
        //        //    //    Random random = new Random();
        //        //    //    int f = random.Next(1, reader.FieldCount);
        //        //    //    while (reader.Read())
        //        //    //    {
        //        //    //        if (f == (int)reader["id"])
        //        //    //        {
        //        //    //            e = reader["path"].ToString();
        //        //    //        }
        //        //    //    }
        //        //    //    connection.Close();
        //        //    //}
        //        //    //Console.WriteLine(e);
        //        //    //Console.ReadLine();
        //        //    //BassNet.Registration("tuchnyy@list.ru", "2X18834155298");
        //        //    //bool result = Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

        //        //    //int _stream = Bass.BASS_StreamCreateURL("https://pool.anison.fm:9000/AniSonFM(320)", 0, BASSFlag.BASS_DEFAULT, null, IntPtr.Zero);
        //        //    //if (_stream != 0)
        //        //    //{
        //        //    //    Bass.BASS_ChannelPlay(_stream, false);
        //        //    //    Console.WriteLine("Музон играет");
        //        //    //    Console.ReadKey();
        //        //    //}
        //        //    //Bass.BASS_StreamFree(_stream);
        //        //    //Bass.BASS_Free();
        //        //    ////Bass.BASS_ChannelSetAttribute(_stream, BASSAttribute.BASS_ATTRIB_VOL, 0.1f);

        //        //}
        //        //public sealed class FastWaveBuffer : MemoryStream, IWaveProvider
        //        //{
        //        //    public FastWaveBuffer(WaveFormat waveFormat, byte[] bytes) : base(bytes)
        //        //    {
        //        //        WaveFormat = waveFormat;
        //        //    }
        //        //    public FastWaveBuffer(WaveFormat waveFormat, int size = 4096) : base()
        //        //    {
        //        //        WaveFormat = waveFormat;
        //        //        Capacity = size;
        //        //    }
        //        //    public WaveFormat WaveFormat
        //        //    {
        //        //        get;
        //        //    }

        //        //}
        //    }
        //}

    }
}


//internal class Program
//{


//    //public class Bot
//    //{
//    //    public DiscordClient Client { get; private set; }
//    //    public InteractivityExtension Interactivity { get; private set; }
//    //    public CommandsNextExtension Commands { get; private set; }

//    //    public async Task RunAsync()
//    //    {
//    //        var config = new DiscordConfiguration() {
//    //            Token = "MTA4ODA1ODkyOTIzNTM3ODE4Nw.GKm6_C.nLFUToqixvfuOdIXrKDYz4v1I32Ok8XZl3gxq4",
//    //            Intents = DiscordIntents.All, 
//    //            TokenType = TokenType.Bot, 
//    //            AutoReconnect = true, };
//    //        Client = new DiscordClient(config); 
//    //        Client.UseInteractivity(new InteractivityConfiguration() 
//    //        { 
//    //            Timeout = TimeSpan.FromMinutes(2) 
//    //        });
//    //        Client.Ready += OnClientReady; 
//    //        Client.ComponentInteractionCreated += ButtonPressResponse;
//    //    }

//    //    Commands = Client.UseCommandsNext(commandsConfig);
//    //    var slashCommandsConfig = Client.UseSlashCommands();

//    //}
//}