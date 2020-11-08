using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Display.ViewModels
{
    public class DilbertDailyImageFromRssFeedViewModel
        : DailyImageFromRssFeedViewModel
    {
        private const string DescriptionImgSrcPattern = @"img src=""[^""]+""";

        public DilbertDailyImageFromRssFeedViewModel()
            : base("Dilbert")
        {
        }

        public override string FileExtension
        {
            get { return "gif"; }
        }

        public override string Address
        {
            get { return "http://www.comicsyndicate.org/Feed/Dilbert"; }
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

                    DateTime date;
                    try
                    {
                        var link = childNode["link"].InnerText;
                        date = DateTime.Parse(link.Substring(link.Length - 10, 10));
                    }
                    catch (Exception ex)
                    {
                        Log.TraceErr("Unable to parse date from RSS feed. {0}", ex.ToString());
                        date = DateTime.Now;
                    }

                    items.Add(new Item(title, imageAddress, date));
                }
            }

            items.Sort((a, b) => b.Date.CompareTo(a.Date));
            return items.First();
        }
    }
}
