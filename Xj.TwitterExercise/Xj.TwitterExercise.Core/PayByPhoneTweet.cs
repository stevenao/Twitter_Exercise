using System;

namespace Xj.TwitterExercise.Core
{
    public class PayByPhoneTweet
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TwitterAccount { get; set; }
        public string Text { get; set; }

        public int MentionAnotherUserCount { get; set; }
    }
}
