using MagicApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MagicApp.Pages
{
    public sealed partial class PicturesPage : Page
    {
        private bool _isLoaded;

        // 获取 Bing 的最新日期
        private DateTime _bingCurrentDate = DateTime.MinValue;

        // 防止初始化 DatePicker 时触发 DateChanged 导致重复加载
        private bool _isUpdatingDatePicker;

        // 当前图片的 URL 不含分辨率后缀的基础部分
        private string _currentUrlBase = "";

        public PicturesPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                return;
            }
            _isLoaded = true;

            BitmapImage BingIcons = new();
            BingIcons.UriSource = new Uri(this.BaseUri, "https://www.bing.com/favicon.ico");
            BingIcon.Source = BingIcons;

            await LoadBingImageAsync(0);
        }

        private async Task LoadBingImageAsync(int idx)
        {
            ProgressRing.IsActive = true;
            Picture.Source = null;
            InfoButton.IsEnabled = false;
            DownloadButton.IsEnabled = false;
            _currentUrlBase = "";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string url = $"https://www.bing.com/HPImageArchive.aspx?n=1&idx={idx}";
                    var response = await httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                        return;

                    string retString = await response.Content.ReadAsStringAsync();
                    var doc = XDocument.Parse(retString);
                    var imageElement = doc.Descendants("image").FirstOrDefault();

                    if (imageElement == null)
                        return;

                    // 解析 Bing 返回的日期（yyyyMMdd）
                    string? startDateText = imageElement.Element("startdate")?.Value;
                    if (string.IsNullOrEmpty(startDateText))
                        return;

                    DateTime imageDate = DateTime.ParseExact(
                        startDateText, "yyyyMMdd", CultureInfo.InvariantCulture);

                    // 用 Bing 的最新日期初始化日历范围
                    if (_bingCurrentDate == DateTime.MinValue)
                    {
                        _bingCurrentDate = imageDate;
                        SetupDatePickerRange();
                    }

                    // 保存不含后缀的 base URL，供下载时使用
                    string? urlBase = imageElement.Element("urlBase")?.Value;
                    if (string.IsNullOrEmpty(urlBase))
                        return;

                    _currentUrlBase = "http://www.bing.com" + urlBase;

                    // 预览使用最高画质 UHD
                    string imgurl = _currentUrlBase + "_UHD.jpg";

                    string? infoTitle = imageElement.Element("headline")?.Value;
                    string? infoBody = imageElement.Element("copyright")?.Value;

                    BitmapImage picture = new();
                    picture.UriSource = new Uri(this.BaseUri, imgurl);
                    Picture.Source = picture;

                    InfoTitle.Text = infoTitle;
                    InfoBody.Text = infoBody;

                    InfoButton.IsEnabled = true;
                    DownloadButton.IsEnabled = true;
                }
                catch
                {

                }
                finally
                {
                    ProgressRing.IsActive = false;
                }
            }
        }

        // 根据 Bing 的最新日期设置日历可选范围
        private void SetupDatePickerRange()
        {
            if (_bingCurrentDate == DateTime.MinValue)
                return;

            // 用 DateTimeOffset 的日期部分，避免时区偏移影响日历显示
            var minDate = new DateTimeOffset(_bingCurrentDate.AddDays(-7), TimeSpan.Zero);
            var maxDate = new DateTimeOffset(_bingCurrentDate, TimeSpan.Zero);

            _isUpdatingDatePicker = true;

            DatePicker.MinDate = minDate;
            DatePicker.MaxDate = maxDate;

            // 默认选中最新日期
            DatePicker.Date = maxDate;

            _isUpdatingDatePicker = false;
        }

        private async void DatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            // 忽略由 SetupDatePickerRange 设置 Date 时触发的事件
            if (_isUpdatingDatePicker)
                return;

            if (sender.Date == null)
                return;

            if (_bingCurrentDate == DateTime.MinValue)
                return;

            DateTime selectedDate = sender.Date.Value.Date;

            // 计算 idx：Bing 今天 - 选中日期
            int idx = (_bingCurrentDate - selectedDate).Days;

            // 保险钳制在 0–7
            if (idx < 0) idx = 0;
            if (idx > 7) idx = 7;

            await LoadBingImageAsync(idx);
        }

        // 按所选画质后缀下载图片
        private async void DownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item)
                return;

            string suffix = item.Tag?.ToString() ?? "";
            if (string.IsNullOrEmpty(_currentUrlBase) || string.IsNullOrEmpty(suffix))
                return;

            DownloadButton.IsEnabled = false;
            try
            {
                string imageUrl = _currentUrlBase + suffix;
                string resolution = suffix.TrimStart('_').Replace(".jpg", "");
                string suggestedFileName = $"Bing_Wallpaper_{resolution}";

                await FileDownloadService.DownloadImageAsync(
                    imageUrl,
                    suggestedFileName,
                    this.XamlRoot);
            }
            catch
            {

            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }
        }
    }
}