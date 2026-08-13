using MagicApp.Controls;
using MagicApp.Helpers;
using MagicApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;

namespace MagicApp.Pages;

public sealed partial class BilibiliVideoDataPage : Page
{
    private readonly HttpClient _httpClient;
    private bool _isLoading = false;
    private DateTime _fetchTime;

    public BilibiliVideoDataPage()
    {
        InitializeComponent();

        // 初始化 HttpClient
        var handler = new HttpClientHandler
        {
            Proxy = null,      // 不使用任何代理
            UseProxy = false   // 禁用代理功能
        };
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        // 初始化字段
        _currentCoverUrl = string.Empty;
    }

    private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

    // 获取视频类型（版权信息）
    private string GetCopyrightText(int copyright)
    {
        return copyright switch
        {
            1 => _resourceLoader.GetString("Pages_BilibiliVideoData_CopyrightSelfMade"),
            2 => _resourceLoader.GetString("Pages_BilibiliVideoData_CopyrightReprinted"),
            _ => _resourceLoader.GetString("Pages_BilibiliVideoData_unknown")
        };
    }

    // 获取视频状态文本
    private string GetVideoStateText(int state)
    {
        return state switch
        {
            0 => "正常",
            1 => "限制",
            2 => "审核中",
            3 => "已删除",
            4 => "已锁定",
            _ => $"未知({state})"
        };
    }

    // 通用的图片加载方法
    private async Task<BitmapImage?> LoadImageAsync(string imageUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                return bitmapImage;
            }
        }
        catch
        {
            // 图片加载失败时返回null
        }

        return null;
    }

    // 查询B站视频数据
    private async Task QueryBilibiliVideoDataAsync(string bvid)
    {
        try
        {
            // 显示加载状态
            SetLoadingState(true);

            // 记录抓取开始时间
            _fetchTime = DateTime.Now;

            // 构建API URL
            string apiUrl = $"https://api.bilibili.com/x/web-interface/view?bvid={bvid}";

            // 发送请求
            var response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            // 解析JSON响应
            var jsonString = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // 检查返回码
            int code = root.GetProperty("code").GetInt32();
            if (code != 0)
            {
                string? message = root.GetProperty("message").GetString();
                ShowError(_resourceLoader.GetString("Pages_BilibiliVideoData_APIError") + " : " + (message ?? "未知错误"));
                return;
            }

            // 获取data对象
            var data = root.GetProperty("data");

            // 更新UI
            await UpdateUIWithVideoData(data);

            // 显示结果，隐藏空状态
            SetResultState(true);
        }
        catch (HttpRequestException ex)
        {
            ShowError(_resourceLoader.GetString("Pages_BilibiliVideoData_NetworkError") + " : " + ex.Message);
        }
        catch (JsonException ex)
        {
            ShowError(_resourceLoader.GetString("Pages_BilibiliVideoData_ParseError") + " : " + ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(_resourceLoader.GetString("Pages_BilibiliVideoData_UnknownError") + " : " + ex.Message);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    // 获取视频实时在线观看人数
    private async Task<string?> GetOnlineCountAsync(string bvid, long cid)
    {
        try
        {
            string url = $"https://api.bilibili.com/x/player/online/total?bvid={bvid}&cid={cid}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            if (root.GetProperty("code").GetInt32() != 0)
                return null;

            return root.GetProperty("data").GetProperty("total").GetString();
        }
        catch
        {
           
        }
        return null;
    }

    // 在类中添加私有字段来保存封面URL
    private string _currentCoverUrl;

    // 使用视频数据更新UI
    private async Task UpdateUIWithVideoData(JsonElement data)
    {
        try
        {
            // 基本信息
            TitleTextBlock.Text = data.GetProperty("title").GetString() ?? _resourceLoader.GetString("Pages_BilibiliVideoData_unknown");
            UpNameTextBlock.Text = data.GetProperty("owner").GetProperty("name").GetString() ?? _resourceLoader.GetString("Pages_BilibiliVideoData_unknown");            
            AidTextBlock.Text = data.GetProperty("aid").ToString();
            BvidTextBlock.Text = data.GetProperty("bvid").GetString() ?? _resourceLoader.GetString("Pages_BilibiliVideoData_unknown");
            CidTextBlock.Text = data.GetProperty("cid").ToString();

            // 发布详细时间
            long pubdate = data.GetProperty("pubdate").GetInt64();
            DateTime publishTime = FormatHelper.UnixTimeStampToDateTime(pubdate);
            PubDateTextBlock.Text = publishTime.ToString("yyyy/MM/dd HH:mm:ss");

            // 获取在线观看人数
            try
            {
                long cid = data.GetProperty("cid").GetInt64();
                string bvid = data.GetProperty("bvid").GetString() ?? "";

                var onlineCount = await GetOnlineCountAsync(bvid, cid);
                OnlineDataControl.DataValue = onlineCount ?? "N/A";
            }
            catch
            {
                OnlineDataControl.DataValue = "N/A";
            }

            // 统计数据
            var stat = data.GetProperty("stat");
            ViewDataControl.DataValue = FormatHelper.FormatNumber(stat.GetProperty("view").GetInt32());
            DanmakuDataControl.DataValue = FormatHelper.FormatNumber(stat.GetProperty("danmaku").GetInt32());
            ReplyDataControl.DataValue = FormatHelper.FormatNumber(stat.GetProperty("reply").GetInt32());
            LikeDataControl.DataValue = FormatHelper.FormatNumber(stat.GetProperty("like").GetInt32());
            CoinDataControl.DataValue = FormatHelper.FormatNumber(stat.GetProperty("coin").GetInt32());
            FavoriteDataControl.DataValue = FormatHelper.FormatNumber(stat.GetProperty("favorite").GetInt32());
            ShareDataControl.DataValue = FormatHelper.FormatNumber(stat.GetProperty("share").GetInt32());

            // 视频简介
            var description = data.GetProperty("desc").GetString();
            DescriptionTextBlock.Text = string.IsNullOrEmpty(description) ? _resourceLoader.GetString("Pages_BilibiliVideoData_None") : description;

            // 视频类型（版权信息）
            int copyright = data.GetProperty("copyright").GetInt32();
            CopyrightTextBlock.Text = GetCopyrightText(copyright);

            // 视频状态
            int state = data.GetProperty("state").GetInt32();
            VideoStateTextBlock.Text = GetVideoStateText(state);

            // 视频时长
            int duration = data.GetProperty("duration").GetInt32();
            DurationTextBlock.Text = FormatHelper.FormatDuration(duration);

            // 抓取时间
            FetchTimeTextBlock.Text = FormatHelper.FormatDateTime(_fetchTime, "yyyy/MM/dd HH:mm:ss");

            // 加载封面图片
            string? coverUrl = data.GetProperty("pic").GetString();
            if (!string.IsNullOrEmpty(coverUrl))
            {
                // 保存封面URL到字段
                _currentCoverUrl = coverUrl;

                var coverImage = await LoadImageAsync(coverUrl);
                if (coverImage != null)
                {
                    CoverImage.Source = coverImage;
                }
            }

            // 加载UP主头像
            string? faceUrl = data.GetProperty("owner").GetProperty("face").GetString();
            if (!string.IsNullOrEmpty(faceUrl))
            {
                var faceImage = await LoadImageAsync(faceUrl);
                if (faceImage != null)
                {
                    UpAvatar.ProfilePicture = faceImage;
                }
            }
        }
        catch (Exception ex)
        {
            ShowError(_resourceLoader.GetString("Pages_BilibiliVideoData_ParseError") + " : " + ex.Message);
        }
    }

    // 设置加载状态
    private void SetLoadingState(bool isLoading)
    {
        _isLoading = isLoading;

        // 显示或隐藏ProgressBar
        LoadingProgressBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

        // 启用或禁用搜索按钮
        SearchButton.IsEnabled = !isLoading;
    }

    // 设置结果显示状态
    private void SetResultState(bool showResult)
    {
        VideoInfoContainer.Visibility = showResult ? Visibility.Visible : Visibility.Collapsed;
        StatsContainer.Visibility = showResult ? Visibility.Visible : Visibility.Collapsed;
        OtherInfoContainer.Visibility = showResult ? Visibility.Visible : Visibility.Collapsed;
        ErrorGrid.Visibility = Visibility.Collapsed;
    }

    // 显示错误信息
    private void ShowError(string message)
    {
        ErrorMessageTextBlock.Text = message;
        ErrorGrid.Visibility = Visibility.Visible;
        VideoInfoContainer.Visibility = Visibility.Collapsed;
        StatsContainer.Visibility = Visibility.Collapsed;
        OtherInfoContainer.Visibility = Visibility.Collapsed;

        // 确保进度条在出错时也隐藏
        LoadingProgressBar.Visibility = Visibility.Collapsed;
    }

    // 搜索按钮点击事件
    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformSearch();
    }

    // 文本框键盘事件
    private async void BvIdTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await PerformSearch();
        }
    }

    // 执行搜索
    private async Task PerformSearch()
    {
        if (_isLoading) return;

        string bvid = BvIdTextBox.Text.Trim();
        if (string.IsNullOrEmpty(bvid))
        {
            ShowError(_resourceLoader.GetString("Pages_BilibiliVideoData_EnterBVNumber"));
            return;
        }

        // 清理BV号格式，确保格式正确
        if (bvid.StartsWith("BV", StringComparison.OrdinalIgnoreCase))
        {
            await QueryBilibiliVideoDataAsync(bvid);
        }
        else if (bvid.Length >= 10)
        {
            // 假设用户可能只输入了后面的部分
            await QueryBilibiliVideoDataAsync("BV" + bvid);
        }
        else
        {
            ShowError(_resourceLoader.GetString("Pages_BilibiliVideoData_InvalidBVNumberFormat"));
        }
    }

    // 下载封面按钮点击事件
    private async void SaveCoverButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCoverButton.IsEnabled = false;

        try
        {
            // 检查是否有封面URL
            if (string.IsNullOrEmpty(_currentCoverUrl))
            {
                SaveCoverButton.IsEnabled = true;
                return;
            }

            // 检查是否有图片可以下载
            if (CoverImage.Source == null)
            {
                SaveCoverButton.IsEnabled = true;
                return;
            }

            // 调用FileDownloadService
            string suggestedFileName = !string.IsNullOrEmpty(BvidTextBlock.Text)
                ? $"{BvidTextBlock.Text}_Cover"
                : $"bilibili_cover_{DateTime.Now:yyyyMMdd_HHmmss}";

            bool success = await FileDownloadService.DownloadImageAsync(
                _currentCoverUrl,
                suggestedFileName,
                this.XamlRoot);
        }
        finally
        {
            SaveCoverButton.IsEnabled = true;
        }
    }

    // 页面卸载时清理资源
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _httpClient?.Dispose();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // 复制链接按钮点击事件
        CopyLinkButton.Action = async () =>
        {
            try
            {
                var package = new DataPackage();
                package.SetText("https://www.bilibili.com/video/" + BvIdTextBox.Text);
                Clipboard.SetContent(package);
                return true;
            }
            catch
            {
                return false;
            }
        };

        // 打开浏览器按钮点击事件
        OpenInBrowserButton.Action = async () =>
        {
            try
            {
                string url = "https://www.bilibili.com/video/" + BvIdTextBox.Text;
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(processStartInfo);
                return true;
            }
            catch
            {
                return false;
            }
        };

        // 复制简介按钮点击事件
        CopyDescriptionButton.Action = async () =>
        {
            try
            {
                var package = new DataPackage();
                package.SetText(DescriptionTextBlock.Text);
                Clipboard.SetContent(package);
                return true;
            }
            catch
            {
                return false;
            }
        };
    }
}