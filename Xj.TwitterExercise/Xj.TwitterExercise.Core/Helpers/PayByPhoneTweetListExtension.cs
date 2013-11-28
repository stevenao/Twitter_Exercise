using System.Collections.Generic;
using System.Linq;

namespace Xj.TwitterExercise.Core.Helpers
{
    /// <summary>
    /// Helper class for PayByPhoneTweetList
    /// </summary>
    public static class PayByPhoneTweetListExtension
    {
        public static IEnumerable<PayByPhoneTweet> SortByDateTime(this IEnumerable<PayByPhoneTweet> tweets, bool isDescendingOrder = false)
        {
            return (isDescendingOrder) ? tweets.OrderByDescending(x => x.CreatedAt) : tweets.OrderBy(x => x.CreatedAt);
        }

        public static IDictionary<string, int> TweetsCountPerAccount(this IEnumerable<PayByPhoneTweet> tweets)
        {
            return tweets.GroupBy(x => x.TwitterAccount).ToDictionary(k => k.Key, v => v.Count());
        }

        public static IDictionary<string, int> MentionOtherCountPerAccount(this IEnumerable<PayByPhoneTweet> tweets)
        {
            return tweets.GroupBy(x => x.TwitterAccount)
                .ToDictionary(k => k.Key, v => v.Sum(x => x.MentionAnotherUserCount));
        }

    }
}