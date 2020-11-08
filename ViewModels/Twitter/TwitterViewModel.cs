using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CefSharp.Internals;

namespace Display.ViewModels.Twitter
{
    public class TwitterViewModel
        : DisplayBaseViewModel
    {
        private readonly RestClient _restCient;
        private const string BaseUrl = "https://api.twitter.com";
        private const string AppOnlyAuthMethod = "oauth2/token";
        //private const string RequestTokenMethod = "oauth2/request_token";
        //private const string AuthorizeMethod = "oauth2/authorize";
        //private const string AccessTokenMethod = "oauth2/access_token";
        private const string SearchMethod = "1.1/search/tweets.json";

        private static string TwitterImagePath = Path.Combine(WebHelpers.ApplicationTempDirectory, "Twitter");

        private ObservableCollection<TweetViewModel> _writableTweets;
        private string _query;
        private string _escapedQuery;

        public TwitterViewModel(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException("query");

            Query = query;
            _writableTweets = new ObservableCollection<TweetViewModel>();
            Tweets = new ReadOnlyObservableCollection<TweetViewModel>(_writableTweets);

            _restCient = new RestClient();
            Refresh += OnRefresh;

            if (!Directory.Exists(TwitterImagePath))
                Directory.CreateDirectory(TwitterImagePath);
        }

        public ReadOnlyObservableCollection<TweetViewModel> Tweets { get; private set; }

        public string Query
        {
            get { return _query; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException("value", "Query string cannot be null or empty");

                _escapedQuery = Uri.EscapeDataString(value);
                Log.TraceMsg("TwitterViewModel: Query string set to '{0}'", _escapedQuery);

                _query = value;
                Title = "Twitter: " + value;
            }
        }

        #region Refresh code

        protected override int RefreshIntervalInMinutes { get { return 20; } }
    
        private string BearerToken { get; set; }

        private void OnRefresh()
        {
            const int retryAttempts = 2;
            var attemptsLeft = retryAttempts;

            do
            {
                try
                {
                    if (string.IsNullOrEmpty(BearerToken))
                        BearerToken = GetBearerToken();

                    _restCient.Method = HttpMethod.Get;
                    var parameters = new Dictionary<string, string>
                    {
                        {"q", _escapedQuery},
                        {"lang", "en"},
                        {"count", "30"},
                        {"include_entities", "true"},
                        {"tweet_mode", "extended"}
                    };
                    var headers = new Dictionary<string, string>
                    {
                        {"Authorization", string.Concat("Bearer ", BearerToken)}
                    };

                    var searchResponse = _restCient.MakeRequest<SearchResponse>(BaseUrl, SearchMethod, null, parameters, headers);

                    var filteredResponses = FilterSearchResponses(searchResponse.Statuses);
                    ProcessResponses(filteredResponses);

                    CleanUpProfilePictures();

                    Invoke(() =>
                    {
                        _writableTweets.Clear();

                        foreach (var status in filteredResponses)
                        {
                            _writableTweets.Add(new TweetViewModel(status));
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.TraceErr("Unable to search for Tweets. Attempting to refresh Bearer token. {0}", ex.ToString());

                    // Try to refresh the Bearer token
                    BearerToken = null;
                }
            } while (--attemptsLeft != 0);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private string GetBearerToken()
        {
            Log.TraceEntry();

            var encodedConsumerSecret = Uri.EscapeDataString(WebHelpers.TwitterConsumerSecret);
            var encodedTwitterConsumerKey = Uri.EscapeDataString(WebHelpers.TwitterConsumerKey);
            var concatKeySecret = string.Join(":", encodedTwitterConsumerKey, encodedConsumerSecret);
            var encodedConcatKeySecret = Base64Encode(concatKeySecret);

            var headers = new Dictionary<string, string>
            {
                {"Authorization", string.Concat("Basic ", encodedConcatKeySecret)}
            };
            var content = "grant_type=client_credentials";

            _restCient.Method = HttpMethod.Post;
            _restCient.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            var bearerResponse = _restCient.MakeRequest<BearerResponse>(BaseUrl, AppOnlyAuthMethod, content, null, headers);

            if (string.Equals(bearerResponse.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase))
            {
                Log.TraceExit();
                return bearerResponse.AccessToken;
            }

            throw new Exception("Unable to retrieve bearer token");
        }

        private IList<StatusResponse> FilterSearchResponses(StatusResponse[] statuses)
        {
            return statuses.Where(response =>
            {
                return !response.FullText.StartsWith("RT @");
            }).ToList();
        }

        private void ProcessResponses(IEnumerable<StatusResponse> responses)
        {
            foreach (var response in responses)
            {
                var text = response.FullText;

                // Trim to the specified display text range, if possible
                try
                {
                    var startIndex = response.DisplayTextRange[0];
                    var length = response.DisplayTextRange[1] - startIndex;

                    if (startIndex != 0)
                    {
                        var startText = text.Substring(0, startIndex - 1);
                        response.ReplyingTo = startText.Split(' ');
                    }

                    text = text.Substring(startIndex, length);
                }
                catch (Exception ex)
                {
                    Log.TraceErr("TwitterViewModel: Couldn't trim tweet text to supplied display text range values. {0}", ex.ToString());
                }

                // Expand any urls
                var urls = response.Entities.Urls;
                if (urls != null
                    && urls.Length != 0)
                {
                    foreach (var url in urls)
                    {
                        text = text.Replace(url.Url, url.ExpandedUrl);
                    }
                }

                response.FullText = text;
            }
        }

        private void CleanUpProfilePictures()
        {
            var path = WebHelpers.TwitterProfileImagePath;

            if (!Directory.Exists(path))
                return;

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

            Log.TraceMsg("TwitterViewModel.CleanUpProfilePictures: Cleaning up unused profile pictures");

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Log.TraceErr("TwitterViewModel.CleanUpProfilePictures: Unable to delete file {0}. {1}", file, ex.Message);
                }
            }
        }

        #region Response Contracts
        [DataContract]
        internal class BearerResponse
        {
            [DataMember(Name = "token_type")]
            public string TokenType { get; set; }

            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }
        }

        [DataContract]
        internal class SearchResponse
        {
            [DataMember(Name = "statuses")]
            public StatusResponse[] Statuses { get; set; }
        }

        [DataContract]
        internal class StatusResponse
        {
            [DataMember(Name = "created_at")]
            public string CreatedAt { get; set; }

            [DataMember(Name = "full_text")]
            public string FullText { get; set; }

            [DataMember(Name = "user")]
            public UserResponse User { get; set; }

            public string[] ReplyingTo { get; set; }

            [DataMember(Name = "entities")]
            public EntitiesResponse Entities { get; set; }

            [DataMember(Name = "display_text_range")]
            public int[] DisplayTextRange { get; set; }
        }

        [DataContract]
        internal class UserResponse
        {
            [DataMember(Name = "screen_name")]
            public string ScreenName { get; set; }

            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "profile_image_url")]
            public string ProfileImageUrl { get; set; }
        }

        [DataContract]
        internal class EntitiesResponse
        {
            [DataMember(Name = "hashtags")]
            public HashtagsResponse[] Hashtags { get; set; }

            [DataMember(Name = "user_mentions")]
            public UserMentionsResponse[] UserMentions { get; set; }

            [DataMember(Name = "urls")]
            public UrlResponse[] Urls { get; set; }
        }

        [DataContract]
        internal class HashtagsResponse
        {
            [DataMember(Name = "text")]
            public string Text { get; set; }
        }

        [DataContract]
        internal class UserMentionsResponse
        {
            [DataMember(Name = "screen_name")]
            public string ScreenName { get; set; }

            [DataMember(Name = "name")]
            public string Name { get; set; }
        }

        [DataContract]
        internal class UrlResponse
        {
            [DataMember(Name = "url")]
            public string Url { get; set; }

            [DataMember(Name = "expanded_url")]
            public string ExpandedUrl { get; set; }
        }
        #endregion
        #endregion
    }
}
