using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
                .AddText(ContectTitle.Text)
                .AddText(Contect.Text)
                .SetAudioEvent(soundEvent)
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
    }
}
