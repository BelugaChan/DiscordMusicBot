using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _132;
using Swan.Parsers;
using System.Security.Policy;

namespace _132
{
    public class AudioTransmit
    {
        public bool IsLooped { get; set; } = false;
        public bool IsPlaying { get; set; } = false;
        public bool IsPaused { get; set; } = false;

        public void Closing()
        {
            IsLooped = false;
            IsPlaying = false;
            IsPaused = false;
        }

        public async Task YoutubePcmAsync(InteractionContext ctx, Uri url, VoiceNextConnection connection, CancellationToken Token)
        {
            if (url.ToString().Contains("youtu.be") || url.ToString().Contains("youtube.com"))
            {
                await YoutubePcm(url, connection, Token);

                var builder = new DiscordWebhookBuilder().WithContent($"Now playing: {url}");
                await ctx.EditResponseAsync(builder);

                while (IsLooped)
                {
                    await YoutubePcm(url, connection, Token);
                }

                Closing();
            }
            else
            {
                var builder = new DiscordWebhookBuilder().WithContent($"I need youtube link to play");
                await ctx.EditResponseAsync(builder);
                Closing();
            }
            
        }

        public async Task PcmAsync(InteractionContext ctx, string filePath, VoiceNextConnection connection, CancellationToken Token)
        {
            await ConvertAudioToPcmAsync(filePath, connection, Token);

            var builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(filePath)}");
            await ctx.EditResponseAsync(builder);

            while (IsLooped)
            {
                await ConvertAudioToPcmAsync(filePath, connection, Token);
            }

            Closing();
        }
          

        public async Task ConvertAudioToPcmAsync(string filePath, VoiceNextConnection connection, CancellationToken Token)
        {

            var transmit = connection.GetTransmitSink();
            MediaFoundationReader reader = new(filePath);

            using (reader)
            {
                var buffer = new byte[81920];

                int byteCount;
                while ((byteCount = await reader.ReadAsync(buffer, 0, buffer.Length, Token)) > 0)
                {
                    while (IsPaused)
                    {
                        await Task.Delay(10);
                    }
                    await transmit.WriteAsync(buffer, 0, byteCount, Token);
                }
            }

            reader.Close();
            reader.Dispose();

        }

        public async Task YoutubePcm(Uri url, VoiceNextConnection connection, CancellationToken Token)
        {
            var ytdl = Process.Start(new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $@"-f bestaudio[abr<=128000] ""{url}"" -g",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });

            var pcm = ytdl.StandardOutput.ReadLine();

            await ConvertAudioToPcmAsync(pcm, connection, Token);
        }
    }
}
