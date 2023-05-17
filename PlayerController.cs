using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using NAudio.Wave;
using DSharpPlus.EventArgs;
using EmbedIO.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using _132;
using YoutubeDLSharp;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Swan.Parsers;
using SpotifyAPI.Web;
using System.Data.SQLite;

namespace _132.PlayerController
{
    public class PlayerControl
    {
        // Флаг для отслеживания состояния паузы

        public AudioTransmit audioTransmit = new();

        private async Task<VoiceNextConnection?> PreparingTransmit(InteractionContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNext();
            var connection = vnext.GetConnection(ctx.Guild);
            var channel = ctx.Member.VoiceState?.Channel;
            if (connection == null && channel != null)
            {
                connection = await vnext.ConnectAsync(channel);
            }
            else if (connection == null && channel == null)
            {
                var builder = new DiscordWebhookBuilder().WithContent("You must be in a voice channel to use this command.");
                await ctx.EditResponseAsync(builder);
            }

            return connection;
        }

        // Метод для воспроизведения музыки
        public async Task PlayMusic(InteractionContext ctx, string filePath, CancellationToken cancelToken)
        {
            DiscordWebhookBuilder builder;

            var connection = await PreparingTransmit(ctx);

            if (!audioTransmit.IsPlaying)
            {
                audioTransmit.IsPaused = false;
                audioTransmit.IsPlaying = true;
                builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(filePath)}");
                await ctx.EditResponseAsync(builder);
                await audioTransmit.ConvertAudioToPcmAsync(filePath, connection, cancelToken);
            }
        }

        public async Task PlayMusic(InteractionContext ctx, double number, CancellationToken cancelToken)
        {
            DiscordWebhookBuilder builder;

            var connection = await PreparingTransmit(ctx);

            if (!audioTransmit.IsPlaying)
            {
                audioTransmit.IsPaused = false;
                audioTransmit.IsPlaying = true;

                string path = ReadSqlite(number);
                var filePath = Path.GetFullPath(path);

                builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(filePath)}");
                await ctx.EditResponseAsync(builder);

                await audioTransmit.PcmAsync(ctx, filePath, connection, cancelToken);
            }
        }

        public async Task PlayMusic(InteractionContext ctx, Uri url, CancellationToken cancelToken)
        {
            DiscordWebhookBuilder builder;

            if (audioTransmit.IsPlaying)
            {           
                StopMusic();
            }
            var connection = await PreparingTransmit(ctx);
            audioTransmit.IsPaused = false;
            audioTransmit.IsPlaying = true;
            builder = new DiscordWebhookBuilder().WithContent($"Now playing: {url}");
            await ctx.EditResponseAsync(builder);
            await audioTransmit.YoutubePcmAsync(ctx, url, connection, cancelToken);

        }

        public async Task PlayMusic(InteractionContext ctx, Uri url, double number, CancellationToken cancelToken)
        {
            DiscordWebhookBuilder builder;

            var connection = await PreparingTransmit(ctx);

            if (!audioTransmit.IsPlaying)
            {
                if(number == -1)
                {
                    audioTransmit.IsPaused = false;
                    audioTransmit.IsPlaying = true;
                    builder = new DiscordWebhookBuilder().WithContent($"Now playing: {url}");
                    await ctx.EditResponseAsync(builder);

                    await audioTransmit.YoutubePcmAsync(ctx, url, connection, cancelToken);
                    
                }
                else if(url == null)
                {
                    audioTransmit.IsPaused = false;
                    audioTransmit.IsPlaying = true;

                    string path = ReadSqlite(number);
                    var fullPath = Path.GetFullPath(path);

                    builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(fullPath)}");
                    await ctx.EditResponseAsync(builder);

                    await audioTransmit.PcmAsync(ctx, fullPath, connection, cancelToken);
                }
            }

        }

        // Метод для приостановки воспроизведения музыки
        public void PauseMusic()
        {
            audioTransmit.IsPaused = true;
        }

        // Метод для возобновления воспроизведения музыки
        public void ResumeMusic()
        {
            audioTransmit.IsPaused = false;
        }

        public void LoopMusic(bool isLooped)
        {
            audioTransmit.IsLooped = isLooped;
        }

        public void StopMusic()
        {
            audioTransmit.Closing();
        }


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
    }
}