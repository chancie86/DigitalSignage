namespace Display.ViewModels.Twitter
{
    public class TweetSection
    {
        public TweetSection(string text, TweetSectionType type)
        {
            Text = text;
            Type = type;
        }

        public string Text { get; private set; }
        public TweetSectionType Type { get; private set; }
    }
}
