using System.Collections.Generic;

namespace Xj.TwitterExercise.Core
{
    public interface ITweetReaderBatchOperation
    {
        IEnumerable<T> GetTweetsFrom<T>(IEnumerable<ITweetsReader<T>> readers);
    }
}