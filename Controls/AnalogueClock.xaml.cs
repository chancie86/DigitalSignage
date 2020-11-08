using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Display.Controls
{
    /// <summary>
    /// Interaction logic for AnalogueClock.xaml
    /// </summary>
    public partial class AnalogueClock : UserControl
    {
        public AnalogueClock()
        {
            InitializeComponent();
        }

        #region Text Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(AnalogueClock), new FrameworkPropertyMetadata(TextChanged));

        private static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var clock = d as AnalogueClock;

            if (clock == null)
                return;

            clock.FaceText.Text = clock.Text;
        }
        #endregion

        #region Time Property
        public DateTime Time
        {
            get { return (DateTime)GetValue(TimeProperty); }
            set { SetValue(TimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Time.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register("Time", typeof(DateTime), typeof(AnalogueClock), new FrameworkPropertyMetadata(TimeChanged));

        private static void TimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var clock = d as AnalogueClock;

            if (clock == null)
                return;
            
            clock.UpdateTime();
        }

        private void UpdateTime()
        {
            //secondHand.Angle = Time.Second * 6;
            minuteHandTransform.Angle = Time.Minute * 6;
            hourHandTransform.Angle = (Time.Hour * 30) + (Time.Minute * 0.5);
            DigitalTime.Text = Time.ToString("t");
        }
        #endregion

        #region ClockName Property
        public string ClockName
        {
            get { return (string)GetValue(ClockNameProperty); }
            set { SetValue(ClockNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ClockName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ClockNameProperty =
            DependencyProperty.Register("ClockName", typeof(string), typeof(AnalogueClock), new FrameworkPropertyMetadata(ClockNameChanged));

        private static void ClockNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var clock = d as AnalogueClock;

            if (clock == null)
                return;

            clock.ClockNameText.Text = clock.ClockName;
        }
        #endregion

        public static readonly DependencyProperty IsDayProperty = DependencyProperty.Register(
            "IsDay", typeof (bool), typeof (AnalogueClock), new PropertyMetadata(false));

        public bool IsDay
        {
            get { return (bool) GetValue(IsDayProperty); }
            set { SetValue(IsDayProperty, value); }
        }

        #region Clock Hands
        private void AnalogueClock_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMinuteHand(e.NewSize);
            UpdateHourHand(e.NewSize);
        }

        private void UpdateMinuteHand(Size newSize)
        {
            UpdateHand(newSize, rectangleMinute, (newSize.Height / 2) - 40, minuteHandTransform);
        }

        private void UpdateHourHand(Size newSize)
        {
            UpdateHand(newSize, rectangleHour, (newSize.Height / 2) - 100, hourHandTransform);
        }

        private void UpdateHand(Size newSize, Rectangle hand, double height, RotateTransform transform)
        {
            var left = (newSize.Width / 2) - (hand.Width / 2);
            var right = left - hand.Width;
            var bottom = (newSize.Height / 2) + 1;
            var top = bottom - height;
            hand.Margin = new Thickness(left, top, right, bottom);

            transform.CenterX = hand.Width / 2;
            transform.CenterY = height;
        }
        #endregion
    }
}
