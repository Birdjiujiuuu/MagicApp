using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace MagicApp.Pages
{
    public sealed partial class NoticePage : Page
    {
        public NoticePage()
        {
            InitializeComponent();
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            //设置初始内容
            AppName.Text = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;
            ContentTitlePreview.Text = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;
        }

        //设置预览
        private void ContentTitle_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(ContentTitle.Text))
            {
                ContentTitlePreview.Text = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;
                return;
            }
            ContentTitlePreview.Text = ContentTitle.Text;
        }

        private void ContentContent_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ContentContentPreview.Text = ContentContent.Text;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = string.Empty;
            ContentContent.Text = string.Empty;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            var selectedSound = SoundEvent_ComboBox.SelectedItem as ComboBoxItem;
            AppNotificationSoundEvent soundEvent = AppNotificationSoundEvent.Default;

            if (selectedSound != null)
            {
                switch (selectedSound.Name?.ToString())
                {
                    case "IM":
                        soundEvent = AppNotificationSoundEvent.IM;
                        break;
                    case "Reminder":
                        soundEvent = AppNotificationSoundEvent.Reminder;
                        break;
                    case "SMS":
                        soundEvent = AppNotificationSoundEvent.SMS;
                        break;
                    case "Alarm":
                        soundEvent = AppNotificationSoundEvent.Alarm;
                        break;
                    case "Call":
                        soundEvent = AppNotificationSoundEvent.Call;
                        break;
                    default:
                        soundEvent = AppNotificationSoundEvent.Default;
                        break;
                }
            }

            AppNotification notification = new AppNotificationBuilder()
                .AddText(ContentTitle.Text)
                .AddText(ContentContent.Text)
                .SetAudioEvent(soundEvent)
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
    }
}
