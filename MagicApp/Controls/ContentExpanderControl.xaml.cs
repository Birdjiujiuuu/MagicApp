using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;

namespace MagicApp.Controls
{
    [ContentProperty(Name = nameof(SettingActionableElement))]
    public sealed partial class ContentExpanderControl : UserControl
    {
        private const int ExpandDurationMs = 250;
        private const int CollapseDurationMs = 200;
        private const int HoverInDurationMs = 120;
        private const int HoverOutDurationMs = 180;

        // 悬停覆盖层的最大不透明度。数值越小，颜色越淡
        private const double HoverMaxOpacity = 0.4;

        private static readonly CornerRadius HeaderTopRoundedOnly = new(5, 5, 0, 0);
        private static readonly CornerRadius HeaderAllRounded = new(5);

        private Storyboard? _currentStoryboard;
        private Storyboard? _hoverStoryboard;

        public ContentExpanderControl()
        {
            InitializeComponent();

            IsEnabledChanged += (_, _) =>
            {
                if (IsEnabled) return;
                HoverOverlay.Opacity = 0;
                StopHoverAnimation();
            };

            Loaded += (_, _) =>
            {
                UpdateHeaderCornerRadius();
                if (!IsExpanded) return;

                ContentRoot.Visibility = Visibility.Visible;
                ContentRoot.Height = double.NaN;
                ChevronRotation.Angle = 180;
            };
        }

        // ================= 依赖属性 =================

        public static readonly DependencyProperty SettingActionableElementProperty =
            DependencyProperty.Register(nameof(SettingActionableElement),
                typeof(FrameworkElement), typeof(ContentExpanderControl),
                new PropertyMetadata(null));

        public FrameworkElement? SettingActionableElement
        {
            get => (FrameworkElement?)GetValue(SettingActionableElementProperty);
            set => SetValue(SettingActionableElementProperty, value);
        }

        public ObservableCollection<object> Items { get; } = new();

        public static readonly DependencyProperty ItemsTemplateProperty =
            DependencyProperty.Register(nameof(ItemsTemplate),
                typeof(DataTemplate), typeof(ContentExpanderControl),
                new PropertyMetadata(null));

        public DataTemplate? ItemsTemplate
        {
            get => (DataTemplate?)GetValue(ItemsTemplateProperty);
            set => SetValue(ItemsTemplateProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header),
                typeof(string), typeof(ContentExpanderControl),
                new PropertyMetadata(""));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description),
                typeof(string), typeof(ContentExpanderControl),
                new PropertyMetadata(""));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(nameof(Image),
                typeof(string), typeof(ContentExpanderControl),
                new PropertyMetadata(""));

        public string Image
        {
            get => (string)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon),
                typeof(IconElement), typeof(ContentExpanderControl),
                new PropertyMetadata(null));

        public IconElement? Icon
        {
            get => (IconElement?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded),
                typeof(bool), typeof(ContentExpanderControl),
                new PropertyMetadata(false, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ContentExpanderControl)d;
            if (!control.IsLoaded) return;

            if ((bool)e.NewValue) control.PlayExpand();
            else control.PlayCollapse();
        }

        // ================= 悬停 =================

        private void HeaderArea_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;
            AnimateHover(HoverMaxOpacity, HoverInDurationMs, EasingMode.EaseOut);
        }

        private void HeaderArea_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;
            AnimateHover(0, HoverOutDurationMs, EasingMode.EaseIn);
        }

        private void AnimateHover(double toOpacity, double durationMs, EasingMode easingMode)
        {
            _hoverStoryboard?.Stop();

            var sb = new Storyboard();
            sb.Children.Add(CreateDoubleAnimation(
                HoverOverlay, "Opacity", toOpacity, durationMs, easingMode));

            sb.Completed += (_, _) =>
            {
                if (ReferenceEquals(_hoverStoryboard, sb)) _hoverStoryboard = null;
            };

            _hoverStoryboard = sb;
            sb.Begin();
        }

        private void StopHoverAnimation()
        {
            _hoverStoryboard?.Stop();
            _hoverStoryboard = null;
        }

        private void UpdateHeaderCornerRadius()
            => HoverOverlay.CornerRadius = IsExpanded ? HeaderTopRoundedOnly : HeaderAllRounded;

        // ================= 展开 / 折叠 =================

        private void PlayExpand()
        {
            StopCurrentStoryboard();
            UpdateHeaderCornerRadius();

            ContentRoot.Visibility = Visibility.Visible;
            ContentRoot.Height = double.NaN;
            ContentRoot.UpdateLayout();

            var targetHeight = ContentRoot.ActualHeight;
            if (targetHeight <= 0)
            {
                ChevronRotation.Angle = 180;
                return;
            }

            ContentRoot.Height = 0;

            var sb = new Storyboard();
            sb.Children.Add(CreateDoubleAnimation(ContentRoot, "Height",
                 targetHeight, ExpandDurationMs, EasingMode.EaseOut, from: 0, dependent: true));
            sb.Children.Add(CreateDoubleAnimation(ContentRoot, "Opacity",
                 1, ExpandDurationMs, EasingMode.EaseOut, from: 0));
            sb.Children.Add(CreateDoubleAnimation(ChevronRotation, "Angle",
                 180, ExpandDurationMs, EasingMode.EaseOut, from: 0, dependent: true));

            sb.Completed += (_, _) =>
            {
                ContentRoot.Height = double.NaN;
                ContentRoot.Opacity = 1;
                ChevronRotation.Angle = 180;
                _currentStoryboard = null;
            };

            _currentStoryboard = sb;
            sb.Begin();
        }

        private void PlayCollapse()
        {
            StopCurrentStoryboard();
            UpdateHeaderCornerRadius();

            var startHeight = ContentRoot.ActualHeight;
            if (startHeight <= 0)
            {
                ContentRoot.Visibility = Visibility.Collapsed;
                ContentRoot.Height = 0;
                ChevronRotation.Angle = 0;
                return;
            }

            var sb = new Storyboard();
            sb.Children.Add(CreateDoubleAnimation(ContentRoot, "Height",
                 0, CollapseDurationMs, EasingMode.EaseIn, from: startHeight, dependent: true));
            sb.Children.Add(CreateDoubleAnimation(ContentRoot, "Opacity",
                 0, CollapseDurationMs, EasingMode.EaseIn, from: 1));
            sb.Children.Add(CreateDoubleAnimation(ChevronRotation, "Angle",
                 0, CollapseDurationMs, EasingMode.EaseIn, from: 180, dependent: true));

            sb.Completed += (_, _) =>
            {
                ContentRoot.Visibility = Visibility.Collapsed;
                ContentRoot.Height = 0;
                ContentRoot.Opacity = 1;
                ChevronRotation.Angle = 0;
                _currentStoryboard = null;
            };

            _currentStoryboard = sb;
            sb.Begin();
        }

        private void StopCurrentStoryboard()
        {
            _currentStoryboard?.Stop();
            _currentStoryboard = null;
        }

        // ================= 动画辅助 =================

        private static DoubleAnimation CreateDoubleAnimation(
            DependencyObject target,
            string property,
            double to,
            double durationMs,
            EasingMode easingMode,
            double? from = null,
            bool dependent = false)
        {
            var anim = new DoubleAnimation
            {
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new CubicEase { EasingMode = easingMode },
                EnableDependentAnimation = dependent
            };
            if (from.HasValue) anim.From = from.Value;

            Storyboard.SetTarget(anim, target);
            Storyboard.SetTargetProperty(anim, property);
            return anim;
        }
    }
}