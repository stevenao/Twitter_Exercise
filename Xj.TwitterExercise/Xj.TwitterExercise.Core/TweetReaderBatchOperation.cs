using System.Collections.Generic;
using System.Linq;

namespace Xj.TwitterExercise.Core
{
    public class TweetReaderBatchOperation : ITweetReaderBatchOperation
    {
        public IEnumerable<T> GetTweetsFrom<T>(IEnumerable<ITweetsReader<T>> readers)
        {
            return readers.AsParallel().SelectMany(x => x.GetTweets()); //perform GET calls for in parellel 
        }
    }
}