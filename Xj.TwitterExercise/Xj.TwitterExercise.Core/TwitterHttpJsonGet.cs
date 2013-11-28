using System.Net.Http;
using System.Threading.Tasks;

namespace Xj.TwitterExercise.Core
{
    public class TwitterHttpJsonGet : ITwitterHttpJsonGet
    {
        private readonly string _forScreenName;
        //for this exercise 15 seems to be a optimial setting to return per get call base on some intelligent guess and my non scientific test
        private const string ApiUrlFormat = "https://api.twitter.com/1.1/statuses/user_timeline.json?count=15&screen_name={0}";
        private const string ApiUrlWithMaxIdFormat = "https://api.twitter.com/1.1/statuses/user_timeline.json?count=16&screen_name={0}&max_id={1}";
        private readonly HttpClient _httpClient;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="secret">Bearer token returned from OAuth</param>
        /// <param name="forScreenName">a Twitter screen name</param>
        public TwitterHttpJsonGet(string secret, string forScreenName)
        {
            _forScreenName = forScreenName;
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", secret));
        }

        /// <summary>
        /// Method to get the "first page" data from twitter api endpoint
        /// </summary>
        /// <returns>json formatted string</returns>
        public string HttpGetTweets()
        {
            var httpGetStringTask = Task.Run(() => _httpClient.GetStringAsync(string.Format(ApiUrlFormat, _forScreenName)));
            //not doing json object deserialization here b/c the tweet json is big and we only need a subset of the informaiton, KISS
            var result = httpGetStringTask.Result;
            return result;
        }
        /// <summary>
        /// Method to get the "next page" data from twitter api endpoint
        /// </summary>
        /// <param name="maxId">the last tweet id from the "current page"</param>
        /// <returns>json formatted string</returns>
        public string HttpGetTweetsSubsequent(string maxId)
        {
            var httpGetStringTask = Task.Run(() => _httpClient.GetStringAsync(string.Format(ApiUrlWithMaxIdFormat, _forScreenName, maxId)));
            var result = httpGetStringTask.Result;
            return result;
        }

        public string ForScreenName { get { return _forScreenName; } }
    }
}