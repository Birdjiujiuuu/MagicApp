using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MagicApp.UI.Controls
{
    public sealed partial class DataControl : UserControl
    {
        public DataControl()
        {
            InitializeComponent();
        }

        #region DataValue 属性
        public static readonly DependencyProperty DataValueProperty =
            DependencyProperty.Register("DataValue", typeof(string), typeof(DataControl),
                new PropertyMetadata("--"));

        public string DataValue
        {
            get { return (string)GetValue(DataValueProperty); }
            set { SetValue(DataValueProperty, value); }
        }
        #endregion

        #region Description 属性
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(DataControl),
                new PropertyMetadata(""));

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
        #endregion

        #region IconGlyph 属性
        public static readonly DependencyProperty IconGlyphProperty =
            DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(DataControl),
                new PropertyMetadata("\uE8E5")); // 默认图标：Edit

        public string IconGlyph
        {
            get => (string)GetValue(IconGlyphProperty);
            set => SetValue(IconGlyphProperty, value);
        }
        #endregion

        #region IconColor 属性
        public static readonly DependencyProperty IconColorProperty =
            DependencyProperty.Register(nameof(IconColor), typeof(Brush), typeof(DataControl),
                new PropertyMetadata(new SolidColorBrush(Colors.DodgerBlue)));

        public Brush IconColor
        {
            get => (Brush)GetValue(IconColorProperty);
            set => SetValue(IconColorProperty, value);
        }
        #endregion
    }
}