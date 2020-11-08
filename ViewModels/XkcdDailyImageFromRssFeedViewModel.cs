using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Display.ViewModels
{
    public sealed class XkcdDailyImageFromRssFeedViewModel
        : DailyImageFromRssFeedViewModel
    {
        private const string DescriptionImgSrcPattern = @"img src=""[^""]+""";

        public XkcdDailyImageFromRssFeedViewModel()
            : base("XKCD")
        {
        }

        #region Properties

        public override string FileExtension
        {
            get { return "png"; }
        }

        public override string Address
        {
            get { return "http://xkcd.com/rss.xml"; }
        }
        #endregion

        protected override Item ParseXml(XmlNode rssNode)
        {
            var channelNode = rssNode.FirstChild;
            Title = channelNode["description"].InnerText;
            var items = new List<Item>();

            foreach (XmlNode childNode in channelNode.ChildNodes)
            { 
                if (string.Equals(childNode.Name, "item", StringComparison.OrdinalIgnoreCase))
                {
                    var title = childNode["title"].InnerText;
                    var imgSrc = Regex.Match(childNode["description"].InnerText, DescriptionImgSrcPattern).Value;
                    var imgSplit = imgSrc.Split('"');

                    if (imgSplit.Length < 2)    // This isn't an image. Probably a weird interactive comic or something so no good for displaying.
                        continue;

                    var imageAddress = imgSplit[1];
                    var date = DateTime.Parse(childNode["pubDate"].InnerText);

                    items.Add(new Item(title, imageAddress, date));
                }
            }

            items.Sort((a, b) => b.Date.CompareTo(a.Date));
            return items.First();
        }
    }
}
