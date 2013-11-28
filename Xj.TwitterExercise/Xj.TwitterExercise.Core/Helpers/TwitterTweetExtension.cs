using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Xj.TwitterExercise.Core.Helpers
{
    /// <summary>
    /// Helper class to read tweet json element
    /// </summary>
    public static class TwitterTweetExtension
    {
        public static DateTime CreatedAt(this JToken tweet)
        {
            const string format = "ddd MMM dd HH:mm:ss %zzzz yyyy";
            var createdAtString = tweet["created_at"].ToString();

            if (string.IsNullOrEmpty(createdAtString))
                return default(DateTime);

            return DateTime.ParseExact(createdAtString, format,
                System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string ScreenName(this JToken tweet)
        {
            return tweet.SelectToken("user.screen_name").Value<string>();
        }

        public static IEnumerable<string> UserMentions(this JToken tweet)
        {
            return
                tweet.SelectToken("entities.user_mentions")
                    .Select(x => x["screen_name"].ToString())
                    .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public static string Text(this JToken tweet)
        {
            return tweet["text"].ToString();
        }

        public static string Id(this JToken tweet)
        {
            return tweet["id_str"].ToString();
        }

    }
}