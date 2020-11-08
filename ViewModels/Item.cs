using System;

namespace Display.ViewModels
{
    public class Item
    {
        public Item(string title, string imageAddress, DateTime date)
        {
            Title = title;
            ImageAddress = imageAddress;
            Date = date;
        }

        public string Title { get; set; }
        public string ImageAddress { get; set; }
        public DateTime Date { get; set; }
    }
}
