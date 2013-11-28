using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Xj.TwitterExercise.Core;
using Xj.TwitterExercise.Core.Helpers;

namespace Test.TwitterExercise.Core
{
    public class TwitterReaderTest
    {
        private const string TwitterBearer =
            @"AAAAAAAAAAAAAAAAAAAAAP%2FVUgAAAAAAv%2FN2%2F%2FXEPUdIvz6yZPwz%2BYduUVE%3DzT3Siv5RmnuKJt2J9hmSQa51jHs2lXZorGzkVwbW1FZ5mdMjDt";
        private const string OAuthConsumerKey = @"eONvi4l3Qke8k3ZDFGOEw";
        private const string OAuthConsumerSecret = @"R03r5zA05KxlfLpLYTYZXIpSlPjyxGXBJ27qWHOBo";
        private const string OAuthUrl = @"https://api.twitter.com/oauth2/token";

        [Test]
        public async void TestGetBearerToken()
        {
            var authHeader = string.Format("Basic {0}",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes(
                        Uri.EscapeDataString(OAuthConsumerKey) + ":" +
                        Uri.EscapeDataString((OAuthConsumerSecret)))
                        ));

            var httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });

            httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

            var responseMessage = await httpClient.PostAsync(OAuthUrl,
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "client_credentials")
                    })
                );
            responseMessage.EnsureSuccessStatusCode();

            dynamic resultJson = JsonConvert.DeserializeObject(
                await responseMessage.Content.ReadAsStringAsync());           

            Assert.AreEqual(TwitterBearer, resultJson["access_token"].ToString());
        }

        private Func<DateTime, bool> GetTwoWeeksDateFilter()
        {
            var cutOffDatetime = DateTime.Parse("2013/11/28").AddDays(-1 * 2 * 7);
            Func<DateTime, bool> dateFilter = d => d >= cutOffDatetime;
            return dateFilter;
        }

        private string[] GetAllPayByPhoneScreenNames()
        {
            return new[] { "pay_by_phone", "PayByPhone", "PayByPhone_UK" };
        }

        private IEnumerable<ITweetsReader<PayByPhoneTweet>> GetReaders()
        {
            return GetAllPayByPhoneScreenNames()
                .Select(x => new TweetsReader(new TwitterHttpJsonGet(TwitterBearer, x), GetTwoWeeksDateFilter()));
        }
        
        [Test]
        public void TestTweetReaderReturning()
        {
            var bulkReader = new TweetReaderBatchOperation();
            var tweetFeeds = bulkReader.GetTweetsFrom(GetReaders()).ToList();
            Assert.NotNull(tweetFeeds);
        }

        private IEnumerable<ITweetsReader<PayByPhoneTweet>> GetFakeReaders()
        {
            var result = new List<ITweetsReader<PayByPhoneTweet>>();

            var paybyphoneJson1 = Substitute.For<ITwitterHttpJsonGet>();
            paybyphoneJson1.HttpGetTweets().Returns(File.ReadAllText(@"..\..\TestData\pay_by_phone_1.json"));
            paybyphoneJson1.HttpGetTweetsSubsequent("0").ReturnsForAnyArgs(File.ReadAllText(@"..\..\TestData\pay_by_phone_2.json"));
            paybyphoneJson1.ForScreenName.ReturnsForAnyArgs("pay_by_phone");

            var paybyphoneJson2 = Substitute.For<ITwitterHttpJsonGet>();
            paybyphoneJson2.HttpGetTweets().Returns(File.ReadAllText(@"..\..\TestData\PayByPhone_1.json"));
            paybyphoneJson2.HttpGetTweetsSubsequent("0").ReturnsForAnyArgs("");
            paybyphoneJson2.ForScreenName.ReturnsForAnyArgs("PayByPhone");

            var paybyphoneJson3 = Substitute.For<ITwitterHttpJsonGet>();
            paybyphoneJson3.HttpGetTweets().Returns(File.ReadAllText(@"..\..\TestData\PayByPhone_UK_1.json"));
            paybyphoneJson3.HttpGetTweetsSubsequent("0").ReturnsForAnyArgs("");
            paybyphoneJson3.ForScreenName.ReturnsForAnyArgs("PayByPhone_UK");

            result.Add(new TweetsReader(paybyphoneJson1, GetTwoWeeksDateFilter()));
            result.Add(new TweetsReader(paybyphoneJson2, GetTwoWeeksDateFilter()));
            result.Add(new TweetsReader(paybyphoneJson3, GetTwoWeeksDateFilter()));
            return result;
        }


        [Test]
        public void TestTweetReaderSortByDate()
        {
            var bulkReader = new TweetReaderBatchOperation();
            var tweetFeeds = bulkReader.GetTweetsFrom(GetFakeReaders());
            var resultFeeds = tweetFeeds.SortByDateTime(true).ToArray();

            Assert.IsTrue(resultFeeds.First().CreatedAt > resultFeeds.Last().CreatedAt);            
        }
        
        [Test]
        public void TestTweetReaderTweetsToJson()
        {
            var bulkReader = new TweetReaderBatchOperation();
            var tweetFeeds = bulkReader.GetTweetsFrom(GetFakeReaders());

            var list = tweetFeeds.SortByDateTime().Select(x => new
            {
                x.TwitterAccount,
                x.Text,
                x.CreatedAt
            }).ToList();

            var jsonList = JsonConvert.SerializeObject(list, Formatting.None);

            Assert.AreEqual(jsonList,
                "[{\"TwitterAccount\":\"PayByPhone\",\"Text\":\"Dès le 1er Décembre vous pourrez payer votre stationnement à Vanves ! On vous poste très rapidement des photos !... http://t.co/ks71qhY1M4\",\"CreatedAt\":\"2013-11-14T03:37:31-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"We're fanning out over here after watching @appcentraltv and learning @ambermac is a PayByPhone user! Check it out http://t.co/28Fgmoy1o4\",\"CreatedAt\":\"2013-11-14T08:02:32-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@Alijaney Please contact the UK PayByPhone customer service team http://t.co/JziBTvkWIA or @PayByPhone_UK\",\"CreatedAt\":\"2013-11-14T09:20:34-08:00\"},{\"TwitterAccount\":\"PayByPhone\",\"Text\":\"RT @mobileparking Payez votre stationnement depuis chez vous http://t.co/keAwrBpYvT\",\"CreatedAt\":\"2013-11-14T14:25:34-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"Coin Wants To Replace Every Single Credit Card In Your Wallet http://t.co/U9THiiUYyT via @HuffPostTech\",\"CreatedAt\":\"2013-11-14T15:06:25-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @techvibes: Learning from the Poor: Why Haiti Destroys Canada in Terms of Mobile Payment Adoption http://t.co/FHvvmgDVAy\",\"CreatedAt\":\"2013-11-15T15:03:12-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @alanlepo: Last trip to San Francisco I started using @Uber. Today I paid for a parking meter using @pay_by_phone. Commerce is changing.…\",\"CreatedAt\":\"2013-11-17T19:31:37-08:00\"},{\"TwitterAccount\":\"PayByPhone\",\"Text\":\"Bientôt payez votre stationnement par mobile avec PayByPhone à Toul, France ! \\nStay Tuned !\\n\\n#PaiementMobile... http://t.co/weHnPvxMcJ\",\"CreatedAt\":\"2013-11-18T08:03:06-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@nigelarthur Please contact the UK PayByPhone customer service team http://t.co/JziBTvkWIA or @PayByPhone_UK\",\"CreatedAt\":\"2013-11-18T09:06:55-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @chandralye: Thanks @appcentraltv for featuring an app that allows us to pay for parking by smartphone: http://t.co/jZndESjzAW Works...\",\"CreatedAt\":\"2013-11-19T14:39:21-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @EasyParkAlaska: Too cold outside to feed a parking meter? Try using @pay_by_phone on 4th Ave or EasyPark lots from inside your warm car\",\"CreatedAt\":\"2013-11-19T15:40:32-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@VIAwesome @wcoastrunner SOOOOOO not lame. Thanks for the love!\",\"CreatedAt\":\"2013-11-21T10:23:01-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@miamiparking it's @pay_by_phone, thanks!\",\"CreatedAt\":\"2013-11-21T16:28:20-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @miamiparking: MPA has goodies &amp; will be signing up peeps for @pay_by_phone @ Miami Book Fair Int'l street fair! 2day &amp; 2mor! Info: http…\",\"CreatedAt\":\"2013-11-23T12:19:31-08:00\"},{\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"@gep13 Unfortunately there are currently no immediate plans to release the mobile application for Windows 8 mobile phones.1/2\",\"CreatedAt\":\"2013-11-25T01:25:41-08:00\"},{\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"@gep13 , however as they grow more popular and the demand 4 the PayByPhone application increases,a version for that devise will be released.\",\"CreatedAt\":\"2013-11-25T01:26:05-08:00\"},{\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"@gep13 Its certainly something we will feed back to the development team no problem at all\",\"CreatedAt\":\"2013-11-25T01:32:11-08:00\"},{\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"@gep13 No problem at all. Have a greta day.\",\"CreatedAt\":\"2013-11-25T01:45:43-08:00\"},{\"TwitterAccount\":\"PayByPhone\",\"Text\":\"Comment faire économiser des milliards d'euros aux collectivités grâce aux paiements par mobile ? http://t.co/clWR7L4MoQ\",\"CreatedAt\":\"2013-11-25T04:55:25-08:00\"},{\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"RT @EastleighPolice: Here's a pic of the missing dog from Sundays accident on the M27. Any sightings call 101 or contact @sweetbaboo99 http…\",\"CreatedAt\":\"2013-11-26T02:25:16-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @applesandnerds: Extremely Useful #Apps to Download --&gt; http://t.co/m3ZnP7XIdU @toshl @checkout51 @starbucks @starbuckscanada @pay_by_ph…\",\"CreatedAt\":\"2013-11-26T10:31:34-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"DYK? The parking meter will continue to show expired after your PayByPhone transaction. Enforcement will view it on their device.\",\"CreatedAt\":\"2013-11-26T14:50:19-08:00\"},{\"TwitterAccount\":\"PayByPhone\",\"Text\":\"@VilledeVanves va mettre en place PayByPhone dès le 1er décembre !\\n\\nhttp://t.co/zxUb0jc1Z9 #PaiementMobile #stationnement #PayByPhone\",\"CreatedAt\":\"2013-11-27T00:59:40-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@salvn SMS charges would depend on your carrier/plan. You can always turn on/off Text Reminders under Options in the app.\",\"CreatedAt\":\"2013-11-27T08:07:21-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @jenntakahashi: @pay_by_phone First time I paid by phone for parking today and I have a question for you - where have you been all my li…\",\"CreatedAt\":\"2013-11-27T15:46:49-08:00\"},{\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@jenntakahashi Where have YOU been? We're glad you found us :) Now tell your friends!\",\"CreatedAt\":\"2013-11-27T15:50:44-08:00\"}]");
        }

        [Test]
        public void TestTweetReaderTweetsWithAggregateInformation()
        {
            var bulkReader = new TweetReaderBatchOperation();
            var tweetFeeds = bulkReader.GetTweetsFrom(GetFakeReaders()).ToList();
            var result = new
            {
                PayByPhoneTweets = tweetFeeds.SortByDateTime(),
                TweetsCountPerAccount = tweetFeeds.TweetsCountPerAccount(),
                MentionOtherCountPerAccount = tweetFeeds.MentionOtherCountPerAccount(),
                Accounts = GetAllPayByPhoneScreenNames()
            };
            var jsonList = JsonConvert.SerializeObject(result);
            Assert.AreEqual(jsonList, "{\"PayByPhoneTweets\":[{\"Id\":\"400950618237317121\",\"CreatedAt\":\"2013-11-14T03:37:31-08:00\",\"TwitterAccount\":\"PayByPhone\",\"Text\":\"Dès le 1er Décembre vous pourrez payer votre stationnement à Vanves ! On vous poste très rapidement des photos !... http://t.co/ks71qhY1M4\",\"MentionAnotherUserCount\":0},{\"Id\":\"401017312838680576\",\"CreatedAt\":\"2013-11-14T08:02:32-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"We're fanning out over here after watching @appcentraltv and learning @ambermac is a PayByPhone user! Check it out http://t.co/28Fgmoy1o4\",\"MentionAnotherUserCount\":2},{\"Id\":\"401036951664746499\",\"CreatedAt\":\"2013-11-14T09:20:34-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@Alijaney Please contact the UK PayByPhone customer service team http://t.co/JziBTvkWIA or @PayByPhone_UK\",\"MentionAnotherUserCount\":2},{\"Id\":\"401113704349306880\",\"CreatedAt\":\"2013-11-14T14:25:34-08:00\",\"TwitterAccount\":\"PayByPhone\",\"Text\":\"RT @mobileparking Payez votre stationnement depuis chez vous http://t.co/keAwrBpYvT\",\"MentionAnotherUserCount\":1},{\"Id\":\"401123984323788801\",\"CreatedAt\":\"2013-11-14T15:06:25-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"Coin Wants To Replace Every Single Credit Card In Your Wallet http://t.co/U9THiiUYyT via @HuffPostTech\",\"MentionAnotherUserCount\":1},{\"Id\":\"401485563909652480\",\"CreatedAt\":\"2013-11-15T15:03:12-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @techvibes: Learning from the Poor: Why Haiti Destroys Canada in Terms of Mobile Payment Adoption http://t.co/FHvvmgDVAy\",\"MentionAnotherUserCount\":1},{\"Id\":\"402277890894475264\",\"CreatedAt\":\"2013-11-17T19:31:37-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @alanlepo: Last trip to San Francisco I started using @Uber. Today I paid for a parking meter using @pay_by_phone. Commerce is changing.…\",\"MentionAnotherUserCount\":2},{\"Id\":\"402467008597344256\",\"CreatedAt\":\"2013-11-18T08:03:06-08:00\",\"TwitterAccount\":\"PayByPhone\",\"Text\":\"Bientôt payez votre stationnement par mobile avec PayByPhone à Toul, France ! \\nStay Tuned !\\n\\n#PaiementMobile... http://t.co/weHnPvxMcJ\",\"MentionAnotherUserCount\":0},{\"Id\":\"402483067123933184\",\"CreatedAt\":\"2013-11-18T09:06:55-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@nigelarthur Please contact the UK PayByPhone customer service team http://t.co/JziBTvkWIA or @PayByPhone_UK\",\"MentionAnotherUserCount\":2},{\"Id\":\"402929115139158016\",\"CreatedAt\":\"2013-11-19T14:39:21-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @chandralye: Thanks @appcentraltv for featuring an app that allows us to pay for parking by smartphone: http://t.co/jZndESjzAW Works...\",\"MentionAnotherUserCount\":2},{\"Id\":\"402944510852079616\",\"CreatedAt\":\"2013-11-19T15:40:32-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @EasyParkAlaska: Too cold outside to feed a parking meter? Try using @pay_by_phone on 4th Ave or EasyPark lots from inside your warm car\",\"MentionAnotherUserCount\":1},{\"Id\":\"403589382185037824\",\"CreatedAt\":\"2013-11-21T10:23:01-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@VIAwesome @wcoastrunner SOOOOOO not lame. Thanks for the love!\",\"MentionAnotherUserCount\":2},{\"Id\":\"403681314899255296\",\"CreatedAt\":\"2013-11-21T16:28:20-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@miamiparking it's @pay_by_phone, thanks!\",\"MentionAnotherUserCount\":1},{\"Id\":\"404343477153968128\",\"CreatedAt\":\"2013-11-23T12:19:31-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @miamiparking: MPA has goodies &amp; will be signing up peeps for @pay_by_phone @ Miami Book Fair Int'l street fair! 2day &amp; 2mor! Info: http…\",\"MentionAnotherUserCount\":1},{\"Id\":\"404903709265510400\",\"CreatedAt\":\"2013-11-25T01:25:41-08:00\",\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"@gep13 Unfortunately there are currently no immediate plans to release the mobile application for Windows 8 mobile phones.1/2\",\"MentionAnotherUserCount\":1},{\"Id\":\"404903810805420032\",\"CreatedAt\":\"2013-11-25T01:26:05-08:00\",\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"@gep13 , however as they grow more popular and the demand 4 the PayByPhone application increases,a version for that devise will be released.\",\"MentionAnotherUserCount\":1},{\"Id\":\"404905345434144769\",\"CreatedAt\":\"2013-11-25T01:32:11-08:00\",\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"@gep13 Its certainly something we will feed back to the development team no problem at all\",\"MentionAnotherUserCount\":1},{\"Id\":\"404908750344953856\",\"CreatedAt\":\"2013-11-25T01:45:43-08:00\",\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"@gep13 No problem at all. Have a greta day.\",\"MentionAnotherUserCount\":1},{\"Id\":\"404956488579682304\",\"CreatedAt\":\"2013-11-25T04:55:25-08:00\",\"TwitterAccount\":\"PayByPhone\",\"Text\":\"Comment faire économiser des milliards d'euros aux collectivités grâce aux paiements par mobile ? http://t.co/clWR7L4MoQ\",\"MentionAnotherUserCount\":0},{\"Id\":\"405281090471874560\",\"CreatedAt\":\"2013-11-26T02:25:16-08:00\",\"TwitterAccount\":\"PayByPhone_UK\",\"Text\":\"RT @EastleighPolice: Here's a pic of the missing dog from Sundays accident on the M27. Any sightings call 101 or contact @sweetbaboo99 http…\",\"MentionAnotherUserCount\":2},{\"Id\":\"405403472922542080\",\"CreatedAt\":\"2013-11-26T10:31:34-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @applesandnerds: Extremely Useful #Apps to Download --&gt; http://t.co/m3ZnP7XIdU @toshl @checkout51 @starbucks @starbuckscanada @pay_by_ph…\",\"MentionAnotherUserCount\":5},{\"Id\":\"405468589773164544\",\"CreatedAt\":\"2013-11-26T14:50:19-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"DYK? The parking meter will continue to show expired after your PayByPhone transaction. Enforcement will view it on their device.\",\"MentionAnotherUserCount\":0},{\"Id\":\"405621934928703488\",\"CreatedAt\":\"2013-11-27T00:59:40-08:00\",\"TwitterAccount\":\"PayByPhone\",\"Text\":\"@VilledeVanves va mettre en place PayByPhone dès le 1er décembre !\\n\\nhttp://t.co/zxUb0jc1Z9 #PaiementMobile #stationnement #PayByPhone\",\"MentionAnotherUserCount\":1},{\"Id\":\"405729565349543937\",\"CreatedAt\":\"2013-11-27T08:07:21-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@salvn SMS charges would depend on your carrier/plan. You can always turn on/off Text Reminders under Options in the app.\",\"MentionAnotherUserCount\":1},{\"Id\":\"405845193930190848\",\"CreatedAt\":\"2013-11-27T15:46:49-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"RT @jenntakahashi: @pay_by_phone First time I paid by phone for parking today and I have a question for you - where have you been all my li…\",\"MentionAnotherUserCount\":1},{\"Id\":\"405846179122266112\",\"CreatedAt\":\"2013-11-27T15:50:44-08:00\",\"TwitterAccount\":\"pay_by_phone\",\"Text\":\"@jenntakahashi Where have YOU been? We're glad you found us :) Now tell your friends!\",\"MentionAnotherUserCount\":1}],\"TweetsCountPerAccount\":{\"PayByPhone_UK\":5,\"PayByPhone\":5,\"pay_by_phone\":16},\"MentionOtherCountPerAccount\":{\"PayByPhone_UK\":6,\"PayByPhone\":2,\"pay_by_phone\":25},\"Accounts\":[\"pay_by_phone\",\"PayByPhone\",\"PayByPhone_UK\"]}");
        }

        [Test]
        public void TestTweetReaderTweetsCountPerAccount()
        {
            var bulkReader = new TweetReaderBatchOperation();
            var tweetFeeds = bulkReader.GetTweetsFrom(GetFakeReaders()).ToList(); 
            var tweetsCountPerAccount = tweetFeeds.GroupBy(x => x.TwitterAccount).ToDictionary(k => k.Key, v => v.Count());
            Assert.AreEqual(tweetsCountPerAccount["PayByPhone"], 5);
            Assert.AreEqual(tweetsCountPerAccount["PayByPhone_UK"], 5);
            Assert.AreEqual(tweetsCountPerAccount["pay_by_phone"], 16);
        }

        [Test]
        public void TestTweetReaderMentionOtherCountPerAccount()
        {
            var bulkReader = new TweetReaderBatchOperation();
            var tweetFeeds = bulkReader.GetTweetsFrom(GetFakeReaders()); 
            
            var mentionOtherCountPerAccount = tweetFeeds.GroupBy(x => x.TwitterAccount)
                .ToDictionary(k => k.Key, v => v.Sum(x => x.MentionAnotherUserCount));
            Assert.AreEqual(mentionOtherCountPerAccount["PayByPhone"], 2);
            Assert.AreEqual(mentionOtherCountPerAccount["PayByPhone_UK"], 6);
            Assert.AreEqual(mentionOtherCountPerAccount["pay_by_phone"], 25);

        }
    }
}
