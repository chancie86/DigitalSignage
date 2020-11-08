using WpfUtils;

namespace Display.ViewModels
{
    public class HtmlViewModel
        : DisplayBaseViewModel
    {
        public HtmlViewModel(string address)
        {
            Address = address;
            DisplayIntervalInSeconds = 90;
        }

        public string Address
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }
    }
}
