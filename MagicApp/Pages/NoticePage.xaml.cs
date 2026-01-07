using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NoticePage : Page
    {
        public NoticePage()
        {
            InitializeComponent();
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
