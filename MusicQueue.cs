using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _132;

namespace _132
{

    public class MusicQueue
    {
        private List<MusicTrack> _tracks = new();

        // Методы для управления очередью треков
        public void AddTrack(MusicTrack track)
        {
            _tracks.Add(track);

            if (_tracks.Count == 1)
            {
                //musicPlayer.Play(track.Url);
            }
        }

        public void AddTrack(DiscordUser author, string url)
        {
            MusicTrack track = new(author, url);
            _tracks.Add(track);
        }

        public void RemoveTrack(int index)
        {
            if (index >= 0 && index < _tracks.Count)
            {
                _tracks.RemoveAt(index);
            }
        }

        public MusicTrack GetNextTrack()
        {
            // Возвращает следующий трек в очереди
            // или null, если очередь пуста
            return _tracks.FirstOrDefault();
        }
    }

    public class MusicTrack
    {
        public MusicTrack(DiscordUser Author, string Url)
        {
            this.Author = Author; 
            this.Url = Url;
        }
        public DiscordUser Author { get; set; }
        public string Url { get; set; }

        // Дополнительные свойства и методы, если необходимо
    }
}
