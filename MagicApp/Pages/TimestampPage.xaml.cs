using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace MagicApp.Pages
{
    public sealed partial class TimestampPage : Page
    {
        private DispatcherTimer? _timer;

        public TimestampPage()
        {
            this.InitializeComponent();

            // 初始化日期时间选择器为当前时间
            DatePickerInput.Date = DateTimeOffset.Now;
            TimePickerInput.Time = DateTimeOffset.Now.TimeOfDay;

            // 启动实时时钟
            StartClock();
        }

        private void StartClock()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateCurrentTimeDisplay();
            _timer.Start();
            UpdateCurrentTimeDisplay();
        }

        private void UpdateCurrentTimeDisplay()
        {
            var now = DateTime.Now;
            CurrentTimeText.Text = now.ToString("yyyy-MM-dd HH:mm:ss");

            // 获取 UTC 时间戳
            long unixSeconds = ((DateTimeOffset)now).ToUnixTimeSeconds();
            CurrentTimestampText.Text = unixSeconds.ToString();
        }

        //日期时间转换时间戳 
        private void OnConvertDateTimeToTimestamp(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime selectedDate = DatePickerInput.Date.DateTime;
                TimeSpan selectedTime = TimePickerInput.Time;
                DateTime localDateTime = selectedDate.Date + selectedTime;

                long unixSeconds = ((DateTimeOffset)localDateTime).ToUnixTimeSeconds();
                ConvertedTimestampResult.Text = unixSeconds.ToString();
            }
            catch
            {
                
            }
        }

        // 时间戳转换日期时间 
        private void OnConvertTimestampToDateTime(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!long.TryParse(TimestampInput.Text, out long unixSeconds))
                {
                    ConvertedDateTimeResult.Text = "请输入有效数字";
                    return;
                }

                DateTimeOffset utcDateTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                DateTime localDateTime = utcDateTime.LocalDateTime;
                ConvertedDateTimeResult.Text = localDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (ArgumentOutOfRangeException)
            {
                ConvertedDateTimeResult.Text = "时间戳超出有效范围";
            }
        }

        // 页面离开时停止定时器，释放资源
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _timer?.Stop();
        }
    }
}