using DSharpPlus.VoiceNext.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using _132.PlayerController;
using DSharpPlus.VoiceNext;

namespace _132.SlashCommands
{
    public class Empty : ApplicationCommandModule { }

    public class MusicSL : ApplicationCommandModule
    {
        private static CancellationTokenSource cancelTokenSource = new();

        private static PlayerControl playerControl = new();

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
        }


        [SlashCommand("play", "playing radio or song")]
        public static async Task PlayMusicCommand(
            InteractionContext ctx,
            [Option("number", "choose song's number")] double number = -1,
            [Option("url", "youtube song url")] string urlString = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            try
            {
                if(number != -1)
                {
                    await playerControl.PlayMusic(ctx, number, cancelTokenSource.Token);
                }
                else if(urlString != null)
                {
                    var url = new Uri(urlString);
                    await playerControl.PlayMusic(ctx, url, cancelTokenSource.Token);
                }
                else
                {
                    var builder = new DiscordWebhookBuilder().WithContent("Nothing to play");
                    await ctx.EditResponseAsync(builder);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var builder = new DiscordWebhookBuilder().WithContent("incorrect song's index, please try again");
                await ctx.EditResponseAsync(builder);
                
            }


        }

        [SlashCommand("leave", "leave voice channel")]
        public static async Task LeaveCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var builder = new DiscordWebhookBuilder();
            DiscordChannel channel = ctx.Member.VoiceState.Channel; ;
            try
            {

                var vnext = ctx.Client.GetVoiceNext();
                var connection = vnext.GetConnection(ctx.Guild);
                connection.Disconnect();
                builder = new DiscordWebhookBuilder().WithContent("voice channel leaved");
                playerControl.audioTransmit.Closing();
            }
            catch (Exception)
            {
                builder = new DiscordWebhookBuilder().WithContent($"something went wrong and I can't disconnect from {channel.Name}");
            }
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

            playerControl.PauseMusic();

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

            playerControl.ResumeMusic();

            builder = new DiscordWebhookBuilder().WithContent("Done!");
            await ctx.EditResponseAsync(builder);

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

        [SlashCommand("loop", "loop song")]
        public static async Task LoopCommand(InteractionContext ctx, [Option("bool", "true or false")] bool looped = false)
        {
            var builder = new DiscordWebhookBuilder();
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            playerControl.LoopMusic(looped);

            builder = new DiscordWebhookBuilder().WithContent($"loop is {looped}");
            await ctx.EditResponseAsync(builder);
        }

        private static async Task VoiceReceiveHandler(VoiceNextConnection connection, VoiceReceiveEventArgs args)
        {
            var transmit = connection.GetTransmitSink();
            await transmit.WriteAsync(args.PcmData);
        }

        
    }
}