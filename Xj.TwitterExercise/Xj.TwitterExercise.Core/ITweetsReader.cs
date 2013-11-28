using System.Collections.Generic;
namespace Xj.TwitterExercise.Core
{
    public interface ITweetsReader<T>
    {
        /// <summary>
        /// Get Tweets
        /// </summary>
        /// <returns>Result of type T</returns>
        IEnumerable<T> GetTweets();
    }
}