using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Globalization;
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
                    string url = "https://birdjiujiuuu.github.io/magicapp/source/winui3/home/Notices.xml";
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string retString = await response.Content.ReadAsStringAsync();

                        var doc = XDocument.Parse(retString);
                        var root = doc.Root;

                        if (root != null && root.Name == "notices")
                        {
                            string currentLang = ApplicationLanguages.Languages[0];
                            string nodeName = currentLang switch
                            {
                                "zh-Hans-CN" => "notice_zh_cn",
                                "zh-Hant-TW" => "notice_zh_tw",
                                "zh-Hant-MO" => "notice_zh_mo",
                                "en-US" => "notice_en_us",
                                "ja" => "notice_ja_jp",
                                "ko" => "notice_ko_kr",
                                _ => "notice_en_us"
                            };

                            var langNode = root.Element(nodeName);
                            if (langNode != null)
                            {
                                string? title = langNode.Element("title")?.Value;
                                string? content = langNode.Element("body")?.Value;

                                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(content))
                                {
                                    NoticeTitle.Text = title;
                                    NoticeContent.Text = content;
                                    NoticeProgressRing.IsActive = false;
                                    return;
                                }
                            }
                        }
                        else
                        {
                            NoticeTitle.Text = _resourceLoader.GetString("Pages_Home_Notice_CanNotLoad");
                            NoticeContent.Text = null;

                            NoticeProgressRing.IsActive = false;
                        }
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
