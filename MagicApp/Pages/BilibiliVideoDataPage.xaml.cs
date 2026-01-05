using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicApp.Pages;

public sealed partial class BilibiliVideoDataPage : Page
{
    private readonly HttpClient _httpClient;
    private bool _isLoading = false;

    public BilibiliVideoDataPage()
    {
        InitializeComponent();

        // 初始化 HttpClient
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    /// <summary>
    /// 格式化数字（添加千分位逗号）
    /// </summary>
    private string FormatNumber(int number)
    {
        return number.ToString("N0");
    }

    /// <summary>
    /// 格式化数字（如果超过1万，显示为x.x万）
    /// </summary>
    private string FormatStatNumber(int number)
    {
        if (number >= 10000)
        {
            double wan = number / 10000.0;
            return wan.ToString("F1") + "万";
        }
        return FormatNumber(number);
    }

    /// <summary>
    /// Unix时间戳转换为DateTime
    /// </summary>
    private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }

    /// <summary>
    /// 加载图片
    /// </summary>
    private async Task LoadImageAsync(string imageUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                CoverImage.Source = bitmapImage;
            }
        }
        catch
        {
            // 图片加载失败时使用默认图片
            CoverImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/DefaultVideoImage.png"));
        }
    }

    /// <summary>
    /// 查询B站视频数据
    /// </summary>
    private async Task QueryBilibiliVideoDataAsync(string bvid)
    {
        try
        {
            // 显示加载状态
            SetLoadingState(true);

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
                string message = root.GetProperty("message").GetString();
                ShowError($"API错误: {message}");
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

    /// <summary>
    /// 使用视频数据更新UI
    /// </summary>
    private async Task UpdateUIWithVideoData(JsonElement data)
    {
        // 基本信息
        TitleTextBlock.Text = data.GetProperty("title").GetString();
        UpNameTextBlock.Text = data.GetProperty("owner").GetProperty("name").GetString();
        BvidTextBlock.Text = data.GetProperty("bvid").GetString();
        AidTextBlock.Text = data.GetProperty("aid").ToString();

        // 分区信息
        var typeName = data.GetProperty("tname").GetString();
        TypeNameTextBlock.Text = typeName;

        // 发布时间
        long pubdate = data.GetProperty("pubdate").GetInt64();
        DateTime publishTime = UnixTimeStampToDateTime(pubdate);
        PubDateTextBlock.Text = publishTime.ToString("yyyy-MM-dd");
        PubTimeTextBlock.Text = publishTime.ToString("yyyy-MM-dd HH:mm:ss");

        // 统计数据
        var stat = data.GetProperty("stat");
        ViewTextBlock.Text = FormatStatNumber(stat.GetProperty("view").GetInt32());
        DanmakuTextBlock.Text = FormatStatNumber(stat.GetProperty("danmaku").GetInt32());
        LikeTextBlock.Text = FormatStatNumber(stat.GetProperty("like").GetInt32());
        CoinTextBlock.Text = FormatStatNumber(stat.GetProperty("coin").GetInt32());
        FavoriteTextBlock.Text = FormatStatNumber(stat.GetProperty("favorite").GetInt32());
        ShareTextBlock.Text = FormatStatNumber(stat.GetProperty("share").GetInt32());
        ReplyTextBlock.Text = FormatStatNumber(stat.GetProperty("reply").GetInt32());

        // 视频简介
        var description = data.GetProperty("desc").GetString();
        DescriptionTextBlock.Text = string.IsNullOrEmpty(description) ? "暂无简介" : description;

        // 加载封面图片
        string coverUrl = data.GetProperty("pic").GetString();
        if (!string.IsNullOrEmpty(coverUrl))
        {
            await LoadImageAsync(coverUrl);
        }
    }

    /// <summary>
    /// 设置加载状态
    /// </summary>
    private void SetLoadingState(bool isLoading)
    {
        _isLoading = isLoading;
        LoadingGrid.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        SearchButton.IsEnabled = !isLoading;
    }

    /// <summary>
    /// 设置结果显示状态
    /// </summary>
    private void SetResultState(bool showResult)
    {
        VideoInfoContainer.Visibility = showResult ? Visibility.Visible : Visibility.Collapsed;
        StatsContainer.Visibility = showResult ? Visibility.Visible : Visibility.Collapsed;
        EmptyGrid.Visibility = showResult ? Visibility.Collapsed : Visibility.Visible;
        ErrorGrid.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// 显示错误信息
    /// </summary>
    private void ShowError(string message)
    {
        ErrorMessageTextBlock.Text = message;
        ErrorGrid.Visibility = Visibility.Visible;
        VideoInfoContainer.Visibility = Visibility.Collapsed;
        StatsContainer.Visibility = Visibility.Collapsed;
        LoadingGrid.Visibility = Visibility.Collapsed;
        EmptyGrid.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// 搜索按钮点击事件
    /// </summary>
    private async void SearchButton_Click(object sender, RoutedEventArgs e)
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

    /// <summary>
    /// 页面卸载时清理资源
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _httpClient?.Dispose();
    }
}