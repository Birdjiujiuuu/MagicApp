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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        // 初始化字段
        _currentCoverUrl = string.Empty;
    }

    // 获取视频类型（版权信息）
    private string GetCopyrightText(int copyright)
    {
        return copyright switch
        {
            1 => "自制",
            2 => "转载",
            _ => "未知"
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
                ShowError($"API错误: {message ?? "未知错误"}");
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
            ShowError($"网络错误: {ex.Message}");
        }
        catch (JsonException ex)
        {
            ShowError($"数据解析错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowError($"发生错误: {ex.Message}");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    // 在类中添加私有字段来保存封面URL
    private string _currentCoverUrl;

    // 使用视频数据更新UI
    private async Task UpdateUIWithVideoData(JsonElement data)
    {
        try
        {
            // 基本信息 - 使用空值合并运算符处理可能的空值
            TitleTextBlock.Text = data.GetProperty("title").GetString() ?? "未知标题";
            UpNameTextBlock.Text = data.GetProperty("owner").GetProperty("name").GetString() ?? "未知UP主";
            BvidTextBlock.Text = data.GetProperty("bvid").GetString() ?? "未知BV号";
            AidTextBlock.Text = data.GetProperty("aid").ToString();

            // 发布时间（详细时间） - 使用 FormatHelper.UnixTimeStampToDateTime
            long pubdate = data.GetProperty("pubdate").GetInt64();
            DateTime publishTime = FormatHelper.UnixTimeStampToDateTime(pubdate);
            PubDateTextBlock.Text = publishTime.ToString("yyyy-MM-dd HH:mm:ss");

            // 统计数据 - 使用 FormatHelper.FormatNumber
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
            DescriptionTextBlock.Text = string.IsNullOrEmpty(description) ? "暂无简介" : description;

            // 视频类型（版权信息）
            int copyright = data.GetProperty("copyright").GetInt32();
            CopyrightTextBlock.Text = GetCopyrightText(copyright);

            // 视频状态
            int state = data.GetProperty("state").GetInt32();
            VideoStateTextBlock.Text = GetVideoStateText(state);

            // 视频时长 - 使用 FormatHelper.FormatDuration
            int duration = data.GetProperty("duration").GetInt32();
            DurationTextBlock.Text = FormatHelper.FormatDuration(duration);

            // 抓取时间 - 使用 FormatHelper.FormatDateTime
            FetchTimeTextBlock.Text = FormatHelper.FormatDateTime(_fetchTime, "yyyy-MM-dd HH:mm:ss");

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
            ShowError($"数据解析错误: {ex.Message}");
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
            ShowError("请输入BV号");
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
            ShowError("BV号格式不正确");
        }
    }

    // 下载封面按钮点击事件
    private async void DownloadCoverButton_Click(object sender, RoutedEventArgs e)
    {
        DownloadCoverButton.IsEnabled = false;

        try
        {
            // 检查是否有封面URL
            if (string.IsNullOrEmpty(_currentCoverUrl))
            {
                DownloadCoverButton.IsEnabled = true;
                return;
            }

            // 检查是否有图片可以下载
            if (CoverImage.Source == null)
            {
                DownloadCoverButton.IsEnabled = true;
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
            DownloadCoverButton.IsEnabled = true;
        }
    }

    // 复制链接按钮点击事件
    private async void CopyLinkButton_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText("https://www.bilibili.com/video/" + BvIdTextBox.Text);
        Clipboard.SetContent(package);
    }

    // 打开浏览器按钮点击事件
    private async void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
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
        }
        catch
        {
            
        }
    }

    // 复制简介按钮点击事件
    private async void CopyDescriptionButton_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(DescriptionTextBlock.Text);
        Clipboard.SetContent(package);
    }

    // 页面卸载时清理资源
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _httpClient?.Dispose();
    }
}