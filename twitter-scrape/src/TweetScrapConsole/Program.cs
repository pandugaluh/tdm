using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Text.Json;

namespace TweetScrapConsole
{
    public class Programs
    {
        static void Main(string[] args)
        {
            string username = "";
            string password = "";
            string searchTerm = "piala dunia";

            string filename = searchTerm;
            string pageSort = "Latest";

            int lastPosition = 0;
            bool endOfScrollRegion = false;
            HashSet<string> uniqueTweets = new HashSet<string>();

            IWebDriver driver = CreateWebDriverInstance();
            bool loggedIn = LoginToTwitter(username, password, driver);
            if (!loggedIn)
            {
                return;
            }

            bool searchFound = FindSearchInputAndEnterCriteria(searchTerm, driver);
            if (!searchFound)
            {
                return;
            }

            ChangePageSort(pageSort, driver);

            while (!endOfScrollRegion)
            {
                IList<IWebElement> cards = CollectAllTweetsFromCurrentView(driver);
                List<ModelTweet> tweetList = new List<ModelTweet>();

                foreach (IWebElement card in cards)
                {
                    ModelTweet tweet;
                    try
                    {
                        tweet = ExtractDataFromCurrentTweetCard(card);
                    }
                    catch (StaleElementReferenceException)
                    {
                        continue;
                    }
                    if (tweet == null)
                    {
                        continue;
                    }

                    string tweetId = GenerateTweetId(tweet);
                    if (!uniqueTweets.Contains(tweetId))
                    {
                        uniqueTweets.Add(tweetId);
                        tweetList.Add(tweet);
                    }
                }

                endOfScrollRegion = true;

                // need to modify for scroll down page
                //Tuple<int, bool> result = ScrollDownPage(driver, lastPosition);
                //lastPosition = result.Item1;
                //endOfScrollRegion = result.Item2;

                // save tweet list to json
                WriteToJson(tweetList, filename);
            }

            driver.Quit();
        }

        public static IWebDriver CreateWebDriverInstance()
        {
            ChromeOptions options = new ChromeOptions();
            options.BinaryLocation = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
            options.AddArguments(new List<string>() { "start-maximized" });
            IWebDriver driver = new ChromeDriver(options);
            return driver;
        }

        public static bool LoginToTwitter(string username, string password, IWebDriver driver)
        {
            string url = "https://twitter.com/login";
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            try
            {
                driver.Navigate().GoToUrl(url);
                string xpathUsername = "//input[@name=\"text\"]";
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy((By.XPath(xpathUsername))));
                IWebElement uidInput = driver.FindElement(By.XPath(xpathUsername));
                uidInput.SendKeys(username);
                uidInput.SendKeys(Keys.Return);
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timeout while waiting for Login screen");
                return false;
            }

            try
            {
                string xpathPassword = "//input[@name=\"password\"]";
                wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy((By.XPath(xpathPassword))));
                IWebElement pwdInput = driver.FindElement(By.XPath("//input[@name=\"password\"]"));
                pwdInput.SendKeys(password);
                pwdInput.SendKeys(Keys.Return);
                url = "https://twitter.com/home";
                wait.Until(ExpectedConditions.UrlToBe(url));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timeout while waiting for home screen");
            }
            return true;
        }

        public static bool FindSearchInputAndEnterCriteria(string searchTerm, IWebDriver driver)
        {
            string xpathSearch = "//input[@aria-label='Search query']";
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy((By.XPath(xpathSearch))));
            IWebElement searchInput = driver.FindElement(By.XPath(xpathSearch));
            searchInput.SendKeys(searchTerm);
            searchInput.SendKeys(Keys.Return);
            return true;
        }

        public static void ChangePageSort(string tabName, IWebDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy((By.LinkText(tabName))));
            IWebElement tab = driver.FindElement(By.LinkText(tabName));
            tab.Click();

            string xpathTabState = $"//a[contains(text(),\"{tabName}\") and @aria-selected='true']";
        }

        public static IList<IWebElement> CollectAllTweetsFromCurrentView(IWebDriver driver, int lookbackLimit = 25)
        {
            IList<IWebElement> pageCards = new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.CssSelector("[data-testid=\"tweet\"]")));
            if (pageCards.Count <= lookbackLimit)
            {
                return pageCards;
            }
            else
            {
                return pageCards.Skip(pageCards.Count - lookbackLimit).ToList();
            }
        }

        public static ModelTweet ExtractDataFromCurrentTweetCard(IWebElement card)
        {
            ModelTweet tweet = new ModelTweet();
            try
            {
                tweet.User = card.FindElement(By.XPath(".//span")).Text;
            }
            catch (NoSuchElementException)
            {
                tweet.User = "";
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
            try
            {
                tweet.Handle = card.FindElement(By.XPath(".//span[contains(text(), '@')]")).Text;
            }
            catch (NoSuchElementException)
            {
                tweet.Handle = "";
            }
            try
            {
                tweet.PostDate = card.FindElement(By.XPath(".//time")).GetAttribute("datetime");
            }
            catch (NoSuchElementException)
            {
                return null;
            }
            try
            {
                tweet.TweetText = card.FindElement(By.XPath(".//div[2]/div[2]/div[1]")).Text;
            }
            catch (NoSuchElementException)
            {
                tweet.TweetText = "";
            }
            try
            {
                tweet.TweetText += card.FindElement(By.XPath(".//div[2]/div[2]/div[2]")).Text;
            }
            catch (NoSuchElementException)
            {
                // Do nothing
            }
            try
            {
                tweet.ReplyCount = card.FindElement(By.XPath(".//div[@data-testid='reply']")).Text;
            }
            catch (NoSuchElementException)
            {
                tweet.ReplyCount = "";
            }
            try
            {
                tweet.RetweetCount = card.FindElement(By.XPath(".//div[@data-testid='retweet']")).Text;
            }
            catch (NoSuchElementException)
            {
                tweet.RetweetCount = "";
            }
            try
            {
                tweet.LikeCount = card.FindElement(By.XPath(".//div[@data-testid='like']")).Text;
            }
            catch (NoSuchElementException)
            {
                tweet.LikeCount = "";
            }

            return tweet;
        }

        public static Tuple<int, bool> ScrollDownPage(IWebDriver driver, int lastPosition, double numSecondsToLoad = 0.5, int scrollAttempt = 0, int maxAttempts = 5)
        {
            bool endOfScrollRegion = false;
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            Thread.Sleep(TimeSpan.FromSeconds(numSecondsToLoad));
            int currPosition = (int)(long)((IJavaScriptExecutor)driver).ExecuteScript("return window.pageYOffset;");
            if (currPosition == lastPosition)
            {
                if (scrollAttempt < maxAttempts)
                {
                    endOfScrollRegion = true;
                }
                else
                {
                    Tuple<int, bool> result = ScrollDownPage(driver, lastPosition, currPosition, scrollAttempt + 1);
                    lastPosition = result.Item1;
                    endOfScrollRegion = result.Item2;
                }
            }
            lastPosition = currPosition;
            return Tuple.Create(lastPosition, endOfScrollRegion);
        }

        public static string GenerateTweetId(ModelTweet tweet)
        {
            return string.Join("", GenerateArrayString(tweet));
        }

        public static string[] GenerateArrayString(ModelTweet tweet)
        {
            string[] arrayString = new string[] { tweet.User, tweet.Handle, tweet.PostDate, tweet.TweetText, tweet.ReplyCount, tweet.RetweetCount, tweet.LikeCount };
            return arrayString;
        }

        public static void WriteToJson(List<ModelTweet> tweets, string filename)
        {
            string jsonString = JsonSerializer.Serialize(tweets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText($"output-{DateTime.Now.ToString("yyyyMMddHHmmss")}-{filename}.json", jsonString);
        }
    }
}