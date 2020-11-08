using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WpfUtils;

namespace Display.ViewModels.Twitter
{
    public class TweetViewModel
        : BaseViewModel
    {
        private readonly ObservableCollection<TweetSection> _writableContents;

        internal TweetViewModel(TwitterViewModel.StatusResponse status)
        {
            if (status == null)
                throw new ArgumentNullException("status");

            Created = DateTime.ParseExact(status.CreatedAt,
                            "ddd MMM dd HH:mm:ss zzz yyyy",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal);;

            var elapsed = DateTime.UtcNow - Created;
            CreatedElapsed = elapsed.TotalMinutes < 60
                ? elapsed.Minutes + "m"
                : (int)Math.Floor(elapsed.TotalHours) + "h";

            Name = status.User.Name;
            ScreenName = status.User.ScreenName;

            _writableContents = new ObservableCollection<TweetSection>();
            Contents = new ReadOnlyObservableCollection<TweetSection>(_writableContents);

            ParseTweet(status);
        }

        public DateTime Created { get; private set; }

        public string CreatedElapsed { get; private set; }

        public string ScreenName { get; private set; }

        public string Name { get; private set; }

        public ReadOnlyObservableCollection<TweetSection> Contents { get; private set; }

        public TextBlock ReplyingTo { get; private set; }

        public TextBlock FormattedText { get; private set; }

        public SolidColorBrush BackgroundColour { get; private set; }

        public string ProfileImagePath { get; set; }

        private void ParseTweet(TwitterViewModel.StatusResponse status)
        {
            if (status.Entities == null)
            {
                _writableContents.Add(new TweetSection(status.FullText, TweetSectionType.Text));
                return;
            }

            var tweetSections = new List<TweetSection>();
            tweetSections.Add(new TweetSection(status.FullText, TweetSectionType.Text));

            // Add all the HashTag sections
            foreach (var hashTag in status.Entities.Hashtags.OrderByDescending(ht => ht.Text.Length))
            {
                UpdateListOfSections(hashTag.Text, TweetSectionType.HashTag, tweetSections);
            }
            
            // Add all the UserMention sections
            foreach (var userMention in status.Entities.UserMentions.OrderByDescending(um => um.ScreenName.Length))
            {
                UpdateListOfSections(userMention.ScreenName, TweetSectionType.UserMention, tweetSections);
            }

            // Unescape strings
            for (var i = 0; i < tweetSections.Count; i++)
            {
                var section = tweetSections[i];
                if (section.Type != TweetSectionType.Text)
                    continue;

                tweetSections[i] = new TweetSection(HttpUtility.HtmlDecode(section.Text), TweetSectionType.Text);
            }

            _writableContents.AddRange(tweetSections);

            UpdateReplyingTo(status.ReplyingTo);
            UpdateFormattedText();

            DownloadProfilePicture(status);
        }

        private void UpdateListOfSections(string splitText, TweetSectionType sectionType, List<TweetSection> listOfSections)
        {
            switch (sectionType)
            {
                case TweetSectionType.HashTag:
                    splitText = "#" + splitText;
                    break;
                case TweetSectionType.UserMention:
                    splitText = "@" + splitText;
                    break;
                default:
                    throw new ArgumentException("Invalid section type specified. Must be a HashTag or UserMention");
            }

            for (var i = 0; i < listOfSections.Count; i++)
            {
                var tweetSection = listOfSections[i];

                if (tweetSection.Type != TweetSectionType.Text)
                {
                    continue;
                }

                //var split = tweetSection.Text.Split(new[] { splitText }, StringSplitOptions.RemoveEmptyEntries);
                var split = Regex.Split(tweetSection.Text, splitText, RegexOptions.IgnoreCase);

                listOfSections.RemoveAt(i);

                for (var j = 0; j < split.Length; j++)
                {
                    if (j != 0)
                    {
                        listOfSections.Insert(i++, new TweetSection(splitText, sectionType));
                    }

                    listOfSections.Insert(i++, new TweetSection(split[j], TweetSectionType.Text));
                }
            }
        }

        private void UpdateFormattedText()
        {
            Invoke(() =>
            {
                var textBlock = new TextBlock();

                foreach (var section in Contents)
                {
                    Run run = new Run(section.Text); ;

                    switch (section.Type)
                    {
                        case TweetSectionType.HashTag:
                            run.Foreground = Brushes.DeepSkyBlue;
                            break;
                        case TweetSectionType.UserMention:
                            run.Foreground = Brushes.DeepSkyBlue;
                            run.SetValue(Run.FontWeightProperty, FontWeights.Bold);
                            break;
                        default:
                            run.Foreground = Brushes.Black;
                            break;
                    }

                    textBlock.Inlines.Add(run);
                }

                FormattedText = textBlock;
            });
        }

        private void UpdateReplyingTo(string[] replyingTo)
        {
            if (replyingTo == null
                || replyingTo.Length == 0)
                return;

            Invoke(() =>
            {
                var textBlock = new TextBlock();

                textBlock.Inlines.Add(new Run(" · Replying to") { Foreground = Brushes.DarkGray });

                foreach (var user in replyingTo)
                {
                    var run = new Run(" " + user);
                    run.Foreground = Brushes.DeepSkyBlue;
                    run.SetValue(Run.FontWeightProperty, FontWeights.Bold);
                    textBlock.Inlines.Add(run);
                }
                
                ReplyingTo = textBlock;
            });
        }

        private void DownloadProfilePicture(TwitterViewModel.StatusResponse status)
        {
            var path = WebHelpers.TwitterProfileImagePath;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            try
            {
                var extension = Path.GetExtension(status.User.ProfileImageUrl);

                var imagePath = Path.Combine(path, status.User.ScreenName + extension);
                using (var client = new WebClient())
                {
                    client.DownloadFile(status.User.ProfileImageUrl, imagePath);
                }

                ProfileImagePath = imagePath;
            }
            catch (Exception ex)
            {
                Log.TraceErr("TweetViewModel: Couldn't download profile picture from {0}. {1}", status.User.ProfileImageUrl, ex.ToString());
            }
        }

        public override string ToString()
        {
            return string.Concat(Contents.Select(x => x.Text));
        }
    }
}
