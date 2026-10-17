using System;
using System.Windows.Input;
using PorkbunDnsUpdater.Commands;
using PorkbunDnsUpdater.Models;
using PorkbunDnsUpdater.Services;

namespace PorkbunDnsUpdater.ViewModels
{
    public class DnsUpdaterViewModel : ViewModelBase
    {

        CancellationTokenSource? cts;
       
        private readonly PorkbunUpdaterService _porkbunUpdaterService;
        private readonly AppConfig _appConfig;

        private string? _currentIPv4;
        private string? _currentIPv6;
        private string? _dnsHost;
        private string? _dnsDomain;

        private string? _checkInterval;        
        private string? _dnsProgress;
        private bool _isRunning;
        private bool _notRunning;
        private bool _includeIPv6;


        public DnsUpdaterViewModel(AppConfig appConfig, PorkbunUpdaterService porkbunUpdaterService)
        {            
            _appConfig = appConfig;
            _porkbunUpdaterService = porkbunUpdaterService;            
            StartDnsUpdater = new TaskDelegateCommand(ExecuteStartDnsUpdater, CanExecuteStartDnsUpdater).ObservesProperty(() => DnsProgress);
            StopDnsUpdater = new DelegateCommand(ExecuteStopDnsUpdater);
            _isRunning = false;
            _notRunning = true;
        }


        public ICommand StartDnsUpdater { get; private set; }

        public ICommand StopDnsUpdater { get; private set; }
         

        public string? CurrentIPv4
        {
            get { return _currentIPv4; }
            set { _currentIPv4 = value; OnPropertyChanged("CurrentIPv4"); }
        }

        public string? CurrentIPv6
        {
            get { return _currentIPv6; }
            set { _currentIPv6 = value; OnPropertyChanged("CurrentIPv6"); }  //Not implemented
        }

        public string? DnsHost
        {
            get { return _dnsHost; }
            set { _dnsHost = value; OnPropertyChanged("DnsHost"); }
        }

        public string? DnsDomain
        {
            get { return _dnsDomain; }
            set { _dnsDomain = value; OnPropertyChanged("DnsDomain"); }
        }
        
        public string CheckInterval
        {
            get { return _checkInterval; }
            set { _checkInterval = value; OnPropertyChanged("CheckInterval"); }
        }

        public string? DnsProgress
        {
            get { return _dnsProgress; }
            set { _dnsProgress = value; OnPropertyChanged("DnsProgress"); }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; OnPropertyChanged("IsRunning"); }
        }

        public bool NotRunning
        {
            get { return _notRunning; }
            set { _notRunning = value; OnPropertyChanged("NotRunning"); }
        }

        public bool IncludeIPv6
        {
            get { return _includeIPv6; }
            set { _includeIPv6 = value; OnPropertyChanged("IncludeIPv6"); }
        }


        private int _intervalInMinutes;
        
        public int IntervalInMinutes
        {
            get { return _intervalInMinutes; }
            set
            {
                _intervalInMinutes = value; 
                OnPropertyChanged("IntervalInMinutes");
                OnPropertyChanged("IntervalDisplay");             
            }
        }
        

        public string IntervalDisplay
        {
            get
            {
                if (IntervalInMinutes == 1440)
                    return "1 day";

                if (IntervalInMinutes >= 60)
                {
                    int hours = IntervalInMinutes / 60;
                    int minutes = IntervalInMinutes % 60;
                    if (minutes == 0)
                        return $"{hours} hour{(hours > 1 ? "s" : "")}";
                    else
                        return $"{hours}h {minutes}m";
                }

                return $"{IntervalInMinutes} min";
            }
        }


        public List<string> IntervalDropDown { get; }


        private async Task ExecuteStartDnsUpdater()
        {
            NotRunning = false;
            IsRunning = true;

            cts = new CancellationTokenSource();

            DnsProgress = "";

            var record = await GetDnsRecordToUpdate();

            if (record == null)
            {
                NotRunning = true;
                IsRunning = false;
                return;
            }

            StatusWindowUpdater("Starting up DnsUpdater!!", false);
                        
            Progress<StatusReport> report = new Progress<StatusReport>();

            report.ProgressChanged += DnsUpdaterReport;

            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += DnsUpdaterRrogress;

            var isInitOk = await InitDnsIP(record, report, cts.Token);
            if (!isInitOk) return;

            StatusWindowUpdater("Scheduled update will be in intervals of: " + IntervalDisplay);
            



            try
            {
                var IPv4Task = _porkbunUpdaterService.ContinuouslyUpdateIP(record, DnsType.A, CurrentIPv4, _intervalInMinutes,  report, progress, cts.Token);
                
                var taskList = new List<Task>
                {
                    IPv4Task
                };

                if (_includeIPv6)
                {                                        
                    var IPv6Task = _porkbunUpdaterService.ContinuouslyUpdateIP(record, DnsType.AAAA, CurrentIPv6, _intervalInMinutes, report, progress, cts.Token);
                    taskList.Add(IPv6Task);
                }
                else 
                {
                    CurrentIPv6 = "NaN";
                }

                await Task.WhenAll(taskList.ToArray());
            }
            catch (OperationCanceledException)
            {
                StatusWindowUpdater("STOP!!");
            }
        }

        private async Task<bool> InitDnsIP(Record record, Progress<StatusReport> report, CancellationToken ct)
        {
            CurrentIPv4 = await _porkbunUpdaterService.InitDnsUpdaterdater(record, DnsType.A, report, ct);

            if (string.IsNullOrEmpty(CurrentIPv4))
            {
                StatusWindowUpdater("STOP!!");
                NotRunning = true;
                IsRunning = false;
                return false;
            }

            if (_includeIPv6)
            {
                CurrentIPv6 = await _porkbunUpdaterService.InitDnsUpdaterdater(record, DnsType.AAAA, report, ct);
                if (string.IsNullOrEmpty(CurrentIPv6))
                {
                    StatusWindowUpdater("STOP!!");
                    NotRunning = true;
                    IsRunning = false;
                    return false;
                }
            }
            else
            {
                CurrentIPv6 = "NaN";
            }
            return true;
        }

        private void ExecuteStopDnsUpdater()
        {

            cts.Cancel();
            NotRunning = true;
            IsRunning = false;
        }

        private async Task<Record?> GetDnsRecordToUpdate()
        {
            var key = _appConfig.PorkbunApiKey;
            var secret = _appConfig.PorkbunApiSecret;

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret)) 
            {

                StatusWindowUpdater("Missing ApiKey / Secret config...", false);                
                await Task.Delay(2000);
                StatusWindowUpdater("STOP!!");
                return null;
            }

            if (string.IsNullOrEmpty(_dnsDomain))
            {
                StatusWindowUpdater("Enter Domain to update...", false);                
                await Task.Delay(2000);
                StatusWindowUpdater("STOP!!");
                return null;
            }

            if (string.IsNullOrEmpty(_dnsHost))
            {
                StatusWindowUpdater("Enter hostname to update...", false);
                await Task.Delay(2000);
                StatusWindowUpdater("STOP!!");
                return null;
            }

            var record = new Record
            {
                Domain = _dnsDomain,
                HostName = _dnsHost
            };

            return record;
        }

        private void DnsUpdaterReport(object? sender, StatusReport e)
        {
            StatusWindowUpdater(e.Content, e.Newline);
        }
        
        private void DnsUpdaterRrogress(object? sender, ProgressReport progress)   
        {            

            if (progress.DnsType == DnsType.A)
            {
                CurrentIPv4 = progress.IP;
            } 
            else if (progress.DnsType == DnsType.AAAA)
            {
                CurrentIPv6 = progress.IP;
            }
        }

        private void StatusWindowUpdater(string logg, bool newLine = true)
        {

            var textToPrint = "";
            if (newLine) 
            { 
                textToPrint = Environment.NewLine;
                textToPrint += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": ";
            }
            
            textToPrint += logg;
            DnsProgress += textToPrint;
        }

        private bool CanExecuteStartDnsUpdater()
        {
            return true;
        }     
    }
}
