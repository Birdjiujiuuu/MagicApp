using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;

namespace MagicApp.Pages
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();

            this.GetNotice(this, new RoutedEventArgs());
        }

        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        private async void GetNotice(object sender, RoutedEventArgs e)
        {
            NoticeProgressRing.IsActive = true;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string url = "https://birdjiujiuuu.github.io/magicapp/source/winui3/home/Notice.xml";
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string retString = await response.Content.ReadAsStringAsync();

                        var doc = XDocument.Parse(retString);
                        var noticeElement = doc.Descendants("notice").FirstOrDefault();

                        if (noticeElement != null)
                        {
                            string? Title = noticeElement.Element("title")?.Value;
                            string? Content = noticeElement.Element("body")?.Value;

                            NoticeTitle.Text = Title;
                            NoticeContent.Text = Content;

                            NoticeProgressRing.IsActive = false;
                        }
                    }
                    else
                    {
                        NoticeTitle.Text = _resourceLoader.GetString("Pages_Home_Notice_CanNotLoad");
                        NoticeContent.Text = null;

                        NoticeProgressRing.IsActive = false;
                    }
                }
                catch
                {
                    NoticeTitle.Text = _resourceLoader.GetString("Pages_Home_Notice_CanNotLoad");
                    NoticeContent.Text = null;

                    NoticeProgressRing.IsActive = false;
                }
            }
        }
    }
}
