using Discord;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _132.PlayerController;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;

namespace _132
{
    public class Button
    {
        private static DiscordClient discord;

        public Button(DSharpPlus.ButtonStyle style, string customId, string label, string emojiId = null)
        {
            Style = style;
            CustomId = customId;
            Label = label;
            if (emojiId != null)
            {
                Emoji = new DiscordComponentEmoji(emojiId);
            }                        
        }

        public DSharpPlus.ButtonStyle Style { get; }

        public string CustomId { get;}

        public string Label { get;}

        public DiscordComponentEmoji Emoji { get; }

        public void PauseMusic(VoiceNextConnection connection)
        {
            PlayerControl.PauseMusic(connection);
        }

        public void ResumeMusic(VoiceNextConnection connection)
        {
            PlayerControl.ResumeMusic(connection);
        }


    }
}
