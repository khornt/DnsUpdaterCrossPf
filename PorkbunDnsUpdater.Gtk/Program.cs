using PorkbunDnsUpdater.Core.Backend.PorkBun.WebClient;
using PorkbunDnsUpdater.Core.Models;
using PorkbunDnsUpdater.Core.Services;
using PorkbunDnsUpdater.Gtk;

var app = Gtk.Application.New("com.horntvedt.porkbundnsupdater", Gio.ApplicationFlags.FlagsNone);
app.OnActivate += (sender, _) =>
{
    var gtkApp = (Gtk.Application)sender;
    var window = Gtk.ApplicationWindow.New(gtkApp);
    window.Title = "Porkbun Dynamic DNS Updater";
    window.SetDefaultSize(640, 620);

    var config = new AppConfig();
    var backend = new PorkbunUpdaterService(new PorkbunHttpClient(config));
    new DnsWindow(window, config, backend).Build();
    window.Present();
};
return app.RunWithSynchronizationContext(null);
