using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Display.ViewModels
{
    public class DevOpsReactionsDailyImageFromRssFeedViewModel
        : DailyImageFromRssFeedViewModel
    {
        private const string DescriptionImgSrcPattern = @"src=""[^""]+""";

        public DevOpsReactionsDailyImageFromRssFeedViewModel()
            : base("DevOpsReactions")
        {
            IsStaticImage = false;
        }

        public override string FileExtension
        {
            get { return "gif"; }
        }

        public override string Address
        {
            get { return "http://devopsreactions.tumblr.com/rss"; }
        }

        protected override Item ParseXml(XmlNode rssNode)
        {
            var channelNode = rssNode.FirstChild;
            Title = channelNode["title"].InnerText;
            var items = new List<Item>();

            foreach (XmlNode childNode in channelNode.ChildNodes)
            {
                if (string.Equals(childNode.Name, "item", StringComparison.OrdinalIgnoreCase))
                {
                    var title = childNode["title"].InnerText;
                    var imgSrc = Regex.Match(childNode["description"].InnerText, DescriptionImgSrcPattern).Value;
                    var imageAddress = imgSrc.Split('"')[1];
                    var date = DateTime.Parse(childNode["pubDate"].InnerText);

                    items.Add(new Item(title, imageAddress, date));
                }
            }

            items.Sort((a, b) => b.Date.CompareTo(a.Date));
            return items.First();
        }
    }
}
