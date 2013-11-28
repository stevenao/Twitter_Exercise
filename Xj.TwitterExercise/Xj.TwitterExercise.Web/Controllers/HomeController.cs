using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Xj.TwitterExercise.Core;
using Xj.TwitterExercise.Core.Helpers;

namespace Xj.TwitterExercise.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly TweetReaderBatchOperation _batchOperationReader;

        public HomeController()
        {
            _batchOperationReader = new TweetReaderBatchOperation();
        }
        //
        // GET: /Home/
        public ActionResult Index()
        {
            return View();
        }

        [NonAction]
        private string GetSecret()
        {
            return ConfigurationManager.AppSettings["TwitterOAuthBearerToken"];
        }

        [NonAction]
        private Func<DateTime, bool> GetDateFilter()
        {
            var numOfDays = double.Parse(ConfigurationManager.AppSettings["ForDaysPeriod"]);
            var cutOffDatetime = DateTime.Now.AddDays(-1 * numOfDays);
            Func<DateTime, bool> dateFilter = d => d >= cutOffDatetime;
            return dateFilter;
        }

        [NonAction]
        private string[] GetScreenNames()
        {
           return ConfigurationManager.AppSettings["ForScreenNames"].Split(new []{","}, StringSplitOptions.RemoveEmptyEntries);
        }

        [Route("pay_by_phone_tweets")]
        [HttpPost]
        public ActionResult TweetsData()
        {
            var tweetFeeds =
                _batchOperationReader.GetTweetsFrom(TweetsReader.CreateReadersFor(GetSecret(), GetDateFilter(), GetScreenNames()))
                    .ToList();
            var result = new
            {
                PayByPhoneTweets = tweetFeeds.SortByDateTime(isDescendingOrder:true),
                TweetsCountPerAccount = tweetFeeds.TweetsCountPerAccount(),
                MentionOtherCountPerAccount = tweetFeeds.MentionOtherCountPerAccount(),
                Accounts = GetScreenNames()
            };
            return Json(result); 
        }

	}
}