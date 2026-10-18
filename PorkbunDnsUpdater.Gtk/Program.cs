using Microsoft.Extensions.Configuration;
using PorkbunDnsUpdater.Core.Backend.PorkBun.WebClient;
using PorkbunDnsUpdater.Core.Models;
using PorkbunDnsUpdater.Core.Services;
using PorkbunDnsUpdater.Gtk;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var config = configuration.GetSection("Porkbun").Get<AppConfig>()
             ?? throw new InvalidOperationException("Missing Porkbun configuration section in appsettings.json.");

if (string.IsNullOrWhiteSpace(config.PorkbunApiKey) ||
    string.IsNullOrWhiteSpace(config.PorkbunApiSecret))
{
    throw new InvalidOperationException("Porkbun credentials are missing in appsettings.json.");
}

var backend = new PorkbunUpdaterService(new PorkbunHttpClient(config));
var app = Gtk.Application.New("com.horntvedt.porkbundnsupdater", Gio.ApplicationFlags.FlagsNone);

app.OnActivate += (sender, _) =>
{
    var gtkApp = (Gtk.Application)sender;
    var window = Gtk.ApplicationWindow.New(gtkApp);
    window.Title = "Porkbun Dynamic DNS Updater";
    window.SetDefaultSize(640, 620);

    new DnsWindow(window, config, backend).Build();
    window.Present();
};

return app.RunWithSynchronizationContext(null);