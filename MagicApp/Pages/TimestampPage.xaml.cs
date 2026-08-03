using MagicApp.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace MagicApp.Pages
{
    public sealed partial class TimestampPage : Page
    {
        private DispatcherTimer? _timer;

        public TimestampPage()
        {
            this.InitializeComponent();

            // 填充秒下拉列表
            for (int i = 0; i < 60; i++)
            {
                SecondsComboBox.Items.Add(i.ToString("D2"));
            }

            // 初始化日期时间选择器为当前时间
            DatePickerInput.Date = DateTimeOffset.Now;
            TimePickerInput.Time = DateTimeOffset.Now.TimeOfDay;
            SecondsComboBox.SelectedItem = DateTime.Now.Second.ToString("D2");            

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
            long unixSeconds = FormatHelper.DateTimeToUnixTimeStamp(now);
            CurrentTimestampText.Text = unixSeconds.ToString();
        }

        //日期时间转换时间戳 
        private void OnConvertDateTimeToTimestamp(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime selectedDate = DatePickerInput.Date.DateTime;
                int hour = TimePickerInput.Time.Hours;
                int minute = TimePickerInput.Time.Minutes;
                string secondStr = SecondsComboBox.SelectedItem?.ToString() ?? "00";
                int second = int.Parse(secondStr);

                DateTime localDateTime = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day,hour, minute, second);

                long unixSeconds = FormatHelper.DateTimeToUnixTimeStamp(localDateTime);
                ConvertedTimestampResult.Text = unixSeconds.ToString();
            }
            catch
            {
                
            }
        }

        // 时间戳转换日期时间
        private void TimestampInput_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                if (!long.TryParse(TimestampInput.Text, out long unixSeconds))
                {
                    ConvertedDateTimeResult.Text = "请输入有效数字";
                    return;
                }

                DateTime localDateTime = FormatHelper.UnixTimeStampToDateTime(unixSeconds);
                ConvertedDateTimeResult.Text = localDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (ArgumentOutOfRangeException)
            {
                ConvertedDateTimeResult.Text = "时间戳超出有效范围";
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CopyTimestampResultButton.Action = async () =>
            {
                if (string.IsNullOrWhiteSpace(ConvertedTimestampResult.Text))
                {
                    return false;
                }
                else
                {
                    try
                    {
                        var package = new DataPackage();
                        package.SetText(ConvertedTimestampResult.Text);
                        Clipboard.SetContent(package);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                
            };
        }

        // 页面离开时停止定时器，释放资源
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _timer?.Stop();
        }
    }
}