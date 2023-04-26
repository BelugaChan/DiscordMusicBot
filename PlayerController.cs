﻿using DSharpPlus;
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
using NReco.VideoConverter;

namespace _132.PlayerController
{
    public static class PlayerControl
    {
        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

        private static CancellationToken token = cancelTokenSource.Token;

        // Флаг для отслеживания состояния паузы
        private static Dictionary<DiscordChannel, bool> pauseFlags = new();

        // Метод для воспроизведения музыки
        //public static async Task PlayMusic(InteractionContext ctx, string fullPath, CancellationToken cancelToken)
        //{
        //    DiscordWebhookBuilder builder;


        //    var vnext = ctx.Client.GetVoiceNext();
        //    var connection = vnext.GetConnection(ctx.Guild);
        //    var channel = ctx.Member.VoiceState?.Channel;
        //    if (connection == null && channel != null)
        //    {
        //        connection = await vnext.ConnectAsync(channel);
        //    }
        //    else if (connection == null && channel == null)
        //    {
        //        builder = new DiscordWebhookBuilder().WithContent("You must be in a voice channel to use this command.");
        //        await ctx.EditResponseAsync(builder);
        //        return;
        //    }

        //    pauseFlags[connection.TargetChannel] = false;
        //    builder = new DiscordWebhookBuilder().WithContent($"Now playing: {Path.GetFileNameWithoutExtension(fullPath)}");
        //    await ctx.EditResponseAsync(builder);
        //    if (!connection.IsPlaying)
        //    {                
        //        await ConvertAudioToPcmAsync(fullPath, connection, cancelToken);
        //    }

        //}

        public static async Task PlayMusic(InteractionContext ctx, string fullPath, CancellationToken token)
        {
            DiscordWebhookBuilder builder;
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var vnext = ctx.Client.GetVoiceNext();
            var connection = vnext.GetConnection(ctx.Guild);
            var channel = ctx.Member.VoiceState?.Channel;

            if (connection == null && channel != null)
            {
                connection = await vnext.ConnectAsync(channel);
            }
            else if (connection == null && channel == null)
            {
                builder = new DiscordWebhookBuilder().WithContent("You must be in a voice channel to use this command.");
                await ctx.EditResponseAsync(builder);
                return;
            }

            if (!connection.IsPlaying)
            {
                pauseFlags[connection.TargetChannel] = false;
                builder = new DiscordWebhookBuilder().WithContent($"Now playing: {fullPath}");
                await ctx.EditResponseAsync(builder);

                await ConvertAudioPcm(fullPath, connection, token);
                //connection.Dispose();
            }

        }

        // Метод для приостановки воспроизведения музыки
        public static void PauseMusic(VoiceNextConnection voiceConnection)
        {
            voiceConnection.Pause();
            pauseFlags[voiceConnection.TargetChannel] = true;
        }

        // Метод для возобновления воспроизведения музыки
        public static void ResumeMusic(VoiceNextConnection voiceConnection)
        {
            voiceConnection.ResumeAsync();
            pauseFlags[voiceConnection.TargetChannel] = false;
        }

        //public static async Task ConvertAudioToPcmAsync(string filePath, VoiceNextConnection connection, CancellationToken Token)
        //{

        //    var transmit = connection.GetTransmitSink();
        //    MediaFoundationReader reader = new(filePath);
        //    using (reader)
        //    {
        //        var buffer = new byte[16384];
        //        int byteCount;
        //        while ((byteCount = await reader.ReadAsync(buffer, 0, buffer.Length, Token)) > 0)
        //        {
        //            while (pauseFlags[connection.TargetChannel])
        //            {
        //                await Task.Delay(10);
        //            }
        //            await transmit.WriteAsync(buffer, 0, byteCount, Token);
        //        }
        //    }
        //    reader.Close();
        //    reader.Dispose();

        //}

        private static async Task ConvertAudioToPcmAsync(string filePath, VoiceNextConnection connection, CancellationToken token)
        {
            var transmit = connection.GetTransmitSink();
            MediaFoundationReader reader = new(filePath);
            using (reader)
            {
                var buffer = new byte[81920];
                int byteCount;
                while ((byteCount = await reader.ReadAsync(buffer, 0, buffer.Length,token)) > 0)
                {

                    if (pauseFlags[connection.TargetChannel])
                    {
                        await Task.Delay(10, token);
                        continue;
                    }

                    await transmit.WriteAsync(buffer, 0, byteCount,token);
                }
            }

            reader.Close();
            transmit.Pause();
            transmit.Dispose();

        }

        private static async Task ConvertAudioPcm(string filePath, VoiceNextConnection connection, CancellationToken token)
        {
            var ytdl = Process.Start(new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $@"-f bestaudio ""{filePath}"" -g",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });


            var pcm = ytdl.StandardOutput.ReadLine();

            await ConvertAudioToPcmAsync(pcm, connection,token);
        }
    }
}