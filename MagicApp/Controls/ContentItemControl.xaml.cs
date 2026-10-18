using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MagicApp.Controls
{
    [ContentProperty(Name = nameof(SettingActionableElement))]
    public sealed partial class ContentItemControl : UserControl
    {
        public FrameworkElement? SettingActionableElement { get; set; }

        public ContentItemControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(ContentItemControl), new PropertyMetadata(""));
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
    }
}
