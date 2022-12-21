using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweetScrapConsole
{
    public class ModelTweet
    {
        public string User { get; set; }
        public string Handle { get; set; }
        public string PostDate { get; set; }
        public string TweetText { get; set; }
        public string ReplyCount { get; set; }
        public string RetweetCount { get; set; }
        public string LikeCount { get; set; }
    }
}
