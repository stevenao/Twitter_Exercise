namespace Xj.TwitterExercise.Core
{
    public interface ITwitterHttpJsonGet
    {
        /// <summary>
        /// Method to get the "first page" data from twitter api endpoint
        /// </summary>
        /// <returns>json formatted string</returns>
        string HttpGetTweets();

        /// <summary>
        /// Method to get the "next page" data from twitter api endpoint
        /// </summary>
        /// <param name="maxId">the last tweet id from the "current page"</param>
        /// <returns>json formatted string</returns>
        string HttpGetTweetsSubsequent(string maxId);

        string ForScreenName { get; }
    }
}