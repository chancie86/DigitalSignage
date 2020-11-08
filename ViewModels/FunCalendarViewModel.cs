using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using WpfUtils;

namespace Display.ViewModels
{
    public class FunCalendarViewModel
        : DisplayBaseViewModel
    {
        private readonly RestClient _restClient;
        private const string BaseUrl = "http://www.webcal.fi/cal.php";
        private const string HolidayId = "435";
        private const string FunId = "50";
        private const string UNObservancesId = "44";
        private const string GoodToKnowId = "219";

        private readonly string[] _calendars =
        {
            HolidayId,
            FunId,
            GoodToKnowId,
            UNObservancesId
        };

        public FunCalendarViewModel()
        {
            _restClient = new RestClient(RestClient.JsonType);
            Refresh += OnRefresh;
            IsVisible = false;
        }

        public ObservableCollection<CalendarDateViewModel> Days { get; private set; }

        protected override int RefreshIntervalInMinutes { get { return 60*6; } }

        private void OnRefresh()
        {
            var now = DateTime.Now;

            // We need to retrieve the data once per year. Reset the calendar on 1st of January
            if (now.DayOfYear == 1)
                Days = null;

            if (Days == null)
            {
                var timeCalendarDayMap = new Dictionary<DateTime, CalendarDateViewModel>();

                foreach (var calendar in _calendars)
                    ParseCalendars(timeCalendarDayMap, calendar);

                Days = new ObservableCollection<CalendarDateViewModel>(timeCalendarDayMap.Values.OrderBy(x => x.Date));
            }

            CalendarDayViewModel nextPublicHoliday = null;
            CalendarDateViewModel nextDate = null;

            // Find the next holiday
            foreach (var date in Days)
            {
                if (nextPublicHoliday != null
                    && nextDate != null)
                    break;

                if (date.Date.DayOfYear <= now.DayOfYear)
                {
                    if (date.Date.DayOfYear == now.DayOfYear)
                        Today = date;

                    continue;
                }

                if (nextDate == null)
                {
                    nextDate = date;
                    continue;
                }

                foreach (var day in date.Days)
                {
                    if (nextPublicHoliday == null
                        && day is HolidayCalendarDayViewModel)
                    {
                        nextPublicHoliday = day;
                        break;
                    }
                }
            }

            NextPublicHoliday = nextPublicHoliday;
            NextDate = nextDate;
            IsVisible = true;
        }

        public CalendarDayViewModel NextPublicHoliday
        {
            get { return PropertyBag.GetAuto<CalendarDayViewModel>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public CalendarDateViewModel Today
        {
            get { return PropertyBag.GetAuto<CalendarDateViewModel>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public CalendarDateViewModel NextDate
        {
            get { return PropertyBag.GetAuto<CalendarDateViewModel>(); }
            set { PropertyBag.SetAuto(value); }
        }

        private void ParseCalendars(IDictionary<DateTime, CalendarDateViewModel> result, string calendarType)
        {
            Log.TraceEntry();

            var parameters = new Dictionary<string, string>
            {
                {"format", "json"},
                {"start_year", "current_year"},
                {"end_year", "current_year"},
                {"tz", "Europe%2FLondon"},
                {"id", calendarType}
            };

            var days = _restClient.MakeRequest<Day[]>(BaseUrl, null, null, parameters);

            Log.TraceMsg("ParseCalendars: Parsing calendar type {0}", calendarType);

            foreach (var day in days)
            {
                try
                {
                    // Some of the results span over weeks, etc. Ignore these.
                    if (string.IsNullOrWhiteSpace(day.Date))
                        continue;

                    var date = DateTime.Parse(day.Date);

                    CalendarDateViewModel vmDate;
                    if (!result.TryGetValue(date, out vmDate))
                    {
                        result[date] = vmDate = new CalendarDateViewModel(date);
                        
                    }

                    CalendarDayViewModel dayViewModel;

                    switch (calendarType)
                    {
                        case HolidayId:
                            dayViewModel = new HolidayCalendarDayViewModel(vmDate, day.Name);
                            break;
                        case FunId:
                            dayViewModel = new FunCalendarDayViewModel(vmDate, day.Name);
                            break;
                        case GoodToKnowId:
                            dayViewModel = new GoodToKnowCalendarDayViewModel(vmDate, day.Name);
                            break;
                        case UNObservancesId:
                        default:
                            dayViewModel = new UNObservancesDayViewModel(vmDate, day.Name);
                            break;
                    }

                    vmDate.Days.Add(dayViewModel);
                }
                catch (Exception ex)
                {
                    Log.TraceErr("FunCalendarViewModel.ParseCalendars: Failed to parse day. {0}", ex.ToString());

                    throw;
                }
            }

            Log.TraceExit();
        }
    }

    #region Types
    [DataContract]
    public sealed class Day
    {
        [DataMember(Name = "date")]
        public string Date { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    #region ViewModels
    public class CalendarDateViewModel
        : BaseViewModel
    {
        public CalendarDateViewModel(DateTime date)
        {
            Date = date;
            Days = new ObservableCollection<CalendarDayViewModel>();
        }

        public DateTime Date { get; private set; }
        public ObservableCollection<CalendarDayViewModel> Days { get; private set; }
    }

    public abstract class CalendarDayViewModel
        : BaseViewModel
    {
        private CalendarDateViewModel _date;
        protected CalendarDayViewModel(CalendarDateViewModel date, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            if (date == null)
                throw new ArgumentNullException("date");

            Name = name;
            _date = date;
        }

        public string Name { get; set; }
        public DateTime Date { get { return _date.Date; } }
    }

    public class HolidayCalendarDayViewModel
        : CalendarDayViewModel
    {
        public HolidayCalendarDayViewModel(CalendarDateViewModel date, string name)
            : base(date, name)
        {
        }
    }

    public class FunCalendarDayViewModel
        : CalendarDayViewModel
    {
        public FunCalendarDayViewModel(CalendarDateViewModel date, string name)
            : base(date, name)
        {
        }
    }

    public class GoodToKnowCalendarDayViewModel
        : CalendarDayViewModel
    {
        public GoodToKnowCalendarDayViewModel(CalendarDateViewModel date, string name)
            : base(date, name)
        {
        }
    }
    
    public class UNObservancesDayViewModel
        : CalendarDayViewModel
    {
        public UNObservancesDayViewModel(CalendarDateViewModel date, string name)
            : base(date, name)
        {
        }
    }
    #endregion
    #endregion
}
