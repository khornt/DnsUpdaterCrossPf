using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using PorkbunDnsUpdater.ViewModels;
using PorkbunDnsUpdater.Services;
using PorkbunDnsUpdater.Backend.PorkBun.WebClient;
using PorkbunDnsUpdater.Models;
using PorkbunDnsUpdater.View;

namespace PorkbunDnsUpdater
{
    public partial class App : System.Windows.Application
    {
               
        private readonly IServiceProvider _serviceProvider;
        private readonly NotifyIcon _notifyIcon;

        public App()
        {

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<NotifyIcon>();
            services.AddSingleton<AppConfig>();

            services.AddSingleton<IPorkbunHttpClient, PorkbunHttpClient>();
            services.AddSingleton<PorkbunUpdaterService>(s => new PorkbunUpdaterService(s.GetRequiredService<IPorkbunHttpClient>()));
            services.AddSingleton<DnsUpdaterViewModel>();                      
            services.AddSingleton<DnsUpdaterView>(s => new DnsUpdaterView(s.GetRequiredService<NotifyIcon>())
            {
               
                DataContext = s.GetRequiredService<DnsUpdaterViewModel>()
            });
            
            _serviceProvider = services.BuildServiceProvider();            
        }

        protected override void OnStartup(StartupEventArgs e)
        {
                     
            MainWindow = _serviceProvider.GetRequiredService<DnsUpdaterView>();
            
            SetupNotify();           
            MainWindow.Show();
            base.OnStartup(e);
        }


        private void SetupNotify()
        {

            var notifyer = _serviceProvider.GetRequiredService<NotifyIcon>();
            
            notifyer.Icon = new System.Drawing.Icon("Resources/Justhead256.ico");
            notifyer.Visible = true;
            notifyer.Text = "DNS Updater";

            var aboutIcon =Image.FromFile("Resources/Justhead128.ico");

            notifyer.ContextMenuStrip = new ContextMenuStrip();
            notifyer.ContextMenuStrip.Items.Add("Show", null, OnShowClicked);
            notifyer.ContextMenuStrip.Items.Add("About", aboutIcon, OnAboutClicked);
            notifyer.ContextMenuStrip.Items.Add("Exit",null, OnExitClicked);
        }

        private void OnAboutClicked(object? sender, EventArgs e)
        {
            System.Windows.MessageBox.Show(CreateAboutText(), "Dynamic DNS Client", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {            
            System.Windows.Application.Current.Shutdown();           
        }

        private void OnShowClicked(object? sender, EventArgs e)
        {
            MainWindow = _serviceProvider.GetRequiredService<DnsUpdaterView>();
            MainWindow.Show();

            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {                        
            base.OnExit(e);
        }

        private string CreateAboutText()
        {
            var about = "Dynamic DNS Client (0.9) for Porkbun DNS" + Environment.NewLine;
            about += "Copyright © 2025 Horntvedt.com. No rights reserved" + Environment.NewLine + Environment.NewLine;

            about += "Credits: Me :-)";

            return about;
        }

    }
}
