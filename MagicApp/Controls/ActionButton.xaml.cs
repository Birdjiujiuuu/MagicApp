using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;

namespace MagicApp.Controls;

public sealed partial class ActionButton : Button
{
    // 依赖属性：主图标字符
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(ActionButton), new PropertyMetadata(""));

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    // 依赖属性：成功图标字符
    public static readonly DependencyProperty SuccessGlyphProperty =
        DependencyProperty.Register(nameof(SuccessGlyph), typeof(string), typeof(ActionButton), new PropertyMetadata("\uE73E"));

    public string SuccessGlyph
    {
        get => (string)GetValue(SuccessGlyphProperty);
        set => SetValue(SuccessGlyphProperty, value);
    }

    // 依赖属性：失败图标字符
    public static readonly DependencyProperty FailureGlyphProperty =
        DependencyProperty.Register(nameof(FailureGlyph), typeof(string), typeof(ActionButton), new PropertyMetadata("\uE711"));

    public string FailureGlyph
    {
        get => (string)GetValue(FailureGlyphProperty);
        set => SetValue(FailureGlyphProperty, value);
    }

    // 业务逻辑委托（开发者赋值）
    public Func<Task<bool>>? Action { get; set; }

    // 模板部件引用
    private Storyboard? _actionAnimation;
    private ContentPresenter? _statusPresenter;

    public ActionButton()
    {
        DefaultStyleKey = typeof(ActionButton);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // 获取模板中的 Storyboard 和 StatusPresenter
        _actionAnimation = GetTemplateChild("ActionAnimation") as Storyboard;
        _statusPresenter = GetTemplateChild("StatusPresenter") as ContentPresenter;

        // 注册点击事件
        Click -= OnActionButtonClick;
        Click += OnActionButtonClick;
    }

    private async void OnActionButtonClick(object sender, RoutedEventArgs e)
    {
        if (Action == null)
            return;

        // 执行业务逻辑，捕获异常视为失败
        bool success = false;
        try
        {
            success = await Action.Invoke();
        }
        catch
        {
            success = false;
        }

        // 根据结果设置状态图标并播放动画
        PlayResultAnimation(success);
    }

    private void PlayResultAnimation(bool success)
    {
        if (_statusPresenter == null || _actionAnimation == null)
            return;

        // 创建 FontIcon 作为状态图标
        var fontIcon = new FontIcon
        {
            Glyph = success ? SuccessGlyph : FailureGlyph,
            FontSize = FontSize,
            FontFamily = FontFamily ?? Application.Current.Resources["SymbolThemeFontFamily"] as FontFamily,
            Foreground = Foreground
        };

        // 更新 StatusPresenter 内容
        _statusPresenter.Content = fontIcon;

        // 开始动画
        _actionAnimation.Begin();
    }
}