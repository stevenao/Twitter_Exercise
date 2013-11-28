using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xj.TwitterExercise.Core.Helpers;

namespace Xj.TwitterExercise.Core
{
    public class TweetsReader : ITweetsReader<PayByPhoneTweet>
    {
        private readonly string _screenName;
        private readonly ITwitterHttpJsonGet _twitterHttpJsonGet;
        private readonly Func<DateTime, bool> _dateFilter;

        /// <summary>
        /// Read tweets by screen name
        /// </summary>
        /// <param name="twitterHttpJsonGet"></param>
        /// <param name="dateFilter">date-filter out tweets</param>
        public TweetsReader(ITwitterHttpJsonGet twitterHttpJsonGet, Func<DateTime, bool> dateFilter)
        {
            _twitterHttpJsonGet = twitterHttpJsonGet;
            _screenName = _twitterHttpJsonGet.ForScreenName;
            _dateFilter = dateFilter;         
        }

        /// <summary>
        /// Custom Mapping from json element to data object
        /// </summary>
        /// <param name="jToken">json element</param>
        /// <returns>PayByPhoneTweet</returns>
        private PayByPhoneTweet MapPayByPhoneTweet(JToken jToken)
        {
            return new PayByPhoneTweet
            {
                Id = jToken.Id(),
                CreatedAt = jToken.CreatedAt(),
                TwitterAccount = _screenName,
                Text = jToken.Text(),
                MentionAnotherUserCount =
                    jToken.UserMentions().Count(u => !_screenName.Equals(u, StringComparison.OrdinalIgnoreCase))
            };
        }
        /// <summary>
        /// Method to get the "first page" data from twitter api endpoint
        /// </summary>
        /// <returns>PayByPhoneTweet list</returns>
        protected virtual IEnumerable<PayByPhoneTweet> GetTweetsFirst()
        {
            //not doing json object deserialization here b/c the tweet json is big and we only need a subset of the informaiton, KISS
            return JArray.Parse(_twitterHttpJsonGet.HttpGetTweets())
                .Where(x => _dateFilter(x.CreatedAt()))
                .Select(MapPayByPhoneTweet).ToList();
        }
        /// <summary>
        /// Method to get the "next page" data from twitter api endpoint
        /// </summary>
        /// <param name="maxId">the last tweet id from the "current page"</param>
        /// <returns>PayByPhoneTweet list</returns>
        protected virtual IEnumerable<PayByPhoneTweet> GetTweetsSubsequent(string maxId)
        {
            return
                JArray.Parse(_twitterHttpJsonGet.HttpGetTweetsSubsequent(maxId))
                    .Where(x => _dateFilter(x.CreatedAt()))
                    .Where(x => x.Id() != maxId)
                    .Select(MapPayByPhoneTweet).ToList();
        }

        /// <summary>
        /// Get Tweets
        /// </summary>
        /// <returns>PayByPhoneTweet list</returns>
        public IEnumerable<PayByPhoneTweet> GetTweets()
        {
            var tweets = GetTweetsFirst().ToList(); //get some tweets first
            while (true)
            {
                foreach (var tweet in tweets)
                {
                    yield return tweet; //return tweets to caller as soon as we have data
                }

                if (tweets.Count() == 15) //as long as nothing got date-filtered out in the last GET call. That means there is potienial more valid data in the next call 
                    tweets = GetTweetsSubsequent(tweets.Last().Id).ToList();
                else yield break; //end when next call not needed
            }
        }
        
        /// <summary>
        /// Create Reader for each screen names
        /// </summary>
        /// <param name="secret">Bearer token returned from OAuth</param>
        /// <param name="dateFilter">date-filter out tweets</param>
        /// <param name="screenNames">screen names </param>
        /// <returns>A list of TweetReaders</returns>
        public static IEnumerable<ITweetsReader<PayByPhoneTweet>> CreateReadersFor(string secret, Func<DateTime, bool> dateFilter, params string[] screenNames)
        {
            return screenNames.Select(screenName => new TweetsReader(new TwitterHttpJsonGet(secret,screenName), dateFilter));
        }
    }
}