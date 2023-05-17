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
        private List<string> tracks = new();
        
        private Queue<string> queue = new Queue<string>();

        public void AddTrack(string url)
        {
            queue.Enqueue(url);            
        }
        
        public void RemoveTrack(int index)
        {
            tracks = queue.ToList();
            tracks.RemoveAt(index);
            queue = new Queue<string>(tracks);
        }

        public string GetNextTrack()
        {
            return queue.Dequeue();
        }

        public List<string> ShowQueue()
        {
            return queue.ToList();
        }

    }

    //public class MusicTrack
    //{
    //    public MusicTrack(string url)
    //    {
    //        Url = url;
    //    }
    //    public string Url { get; set; }

    //    // Дополнительные свойства и методы, если необходимо
    //}
}
