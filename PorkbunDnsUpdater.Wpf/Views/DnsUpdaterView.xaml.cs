using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using UserControl = System.Windows.Controls.UserControl;


namespace PorkbunDnsUpdater.View
{
    /// <summary>
    /// Interaction logic for CreateCertificateView.xaml
    /// </summary>
    public partial class DnsUpdaterView : Window
    {
        private readonly NotifyIcon _notifyIcon;
        public DnsUpdaterView(NotifyIcon notify)
        {

            
            InitializeComponent();
                        
            this.SourceInitialized += new EventHandler(OnSourceInitialized);
            _notifyIcon = notify;

        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            HwndSource source = (HwndSource)PresentationSource.FromVisual(this);
            source.AddHook(new HwndSourceHook(HandleMessages));
        }

        private IntPtr HandleMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 0x0112 == WM_SYSCOMMAND, 'Window' command message.
            // 0xF020 == SC_MINIMIZE, command to minimize the window.
            if (msg == 0x0112 && ((int)wParam & 0xFFF0) == 0xF020)
            {
                // Cancel the minimize.                                              
                
                _notifyIcon.ShowBalloonTip(5000,"DnsUpdater","Exit program from System tray", ToolTipIcon.Info);

                handled = true;
                this.Hide();
            }

            if (msg == 0x0112 && ((int)wParam & 0xFFF0) == 0xF060)
            {

                _notifyIcon.ShowBalloonTip(5000, "DnsUpdater", "Exit program from System tray", ToolTipIcon.Info);
                this.Hide();
                handled = true;

            }

            return IntPtr.Zero;


        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();


            _notifyIcon.ShowBalloonTip(5000, "DnsUpdater", "Exit program from System tray", ToolTipIcon.Info);
           
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            _notifyIcon.ShowBalloonTip(5000, "DnsUpdater", "Exit program from System tray", ToolTipIcon.Info);

        }

    }
}
