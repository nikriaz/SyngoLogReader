using MaterialDesignThemes.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LogReader.Helpers;
using LogReader.Models;
using LogReader.View;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace LogReader.ViewModel
{
    class MessagesViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        /// <summary>
        /// This is main ViewModel for the Main Window User Interface
        /// </summary>

        private string previousStatus;
        private FilterSet filterSet;
        private bool atLeastOneSeverityCheked;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private CancellationTokenSource cts;

        #region Event Handlers
        
        //FileNavigator informs its status changing by rising this event 
        private void _fileNavigator_OnStatusChanged(object sender, string infoKey)
        {
            Informer(infoKey);
        }

        private void MainDialogClosed(object sender, DialogClosingEventArgs eventArgs)
        {
            StatusText = previousStatus;
        }

        //this event is used to prevent "Go" without at least one severity checkboxes checked.
        private void severity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            atLeastOneSeverityCheked = SeveritiesChecked.Any(cb => cb.IsChecked == true);
            _goCommand.OnCanExecuteChanged();
        }

        #endregion

        #region Commands
        private readonly DelegateCommandAsync _goCommand;
        public IDelegateCommandAsync GoCommand => _goCommand;
        public ICommand OpenFileCommand { get; }
        public ICommand CleanDatabaseCommand { get; }
        public ICommand ExportToXMLCommand { get; }
        public ICommand SwapRelativeUnitsCommand { get; }
        public ICommand TrimTextCommand { get; }
        public ICommand AbortCommand { get; }
        #endregion

        #region Main Window Properties

        public string Version { get; set; } //App version indication in UI

        private List<Message> _logList; //Main GridView
        public List<Message> LogList
        {
            get => _logList;
            set => OnPropertyChanged(ref _logList, value);
        }

        private ObservableCollection<MessageSeverity> _severitiesChecked; //Severity checkboxes
        public ObservableCollection<MessageSeverity> SeveritiesChecked
        {
            get => _severitiesChecked;
            set
            {
                OnPropertyChanged(ref _severitiesChecked, value);
            }
        }
        private string _messageTextFilter; 
        public string MessageTextFilter
        {
            get => _messageTextFilter;
            set
            {
                OnPropertyChanged(ref _messageTextFilter, value);
                ValidateMessageId();
            }
        }

        private string _messageIdFilter;
        public string MessageIdFilter
        {
            get => _messageIdFilter;
            set
            {
                OnPropertyChanged(ref _messageIdFilter, value);
                ValidateMessageId();
            }
        }
        private string _sourceNameFilter;
        public string SourceNameFilter
        {
            get => _sourceNameFilter;
            set
            {
                OnPropertyChanged(ref _sourceNameFilter, value);
                ValidateMessageId();
            }
        }

        private DateTime _fromDtFilter;
        public DateTime FromDtFilter
        {
            get => _fromDtFilter;
            set
            {
                OnPropertyChanged(ref _fromDtFilter, value);
            }
        }

        private DateTime _toDtFilter;
        public DateTime ToDtFilter
        {
            get => _toDtFilter;
            set
            {
                OnPropertyChanged(ref _toDtFilter, value);
            }
        }

        private SummaryHeader _summaryHeader = new SummaryHeader();
        public SummaryHeader SummaryHeader
        {
            get => _summaryHeader;
            set
            {
                OnPropertyChanged(ref _summaryHeader, value);
            }
        }

        private bool _isAbsoluteTimeRange;
        public bool IsAbsoluteTimeRange
        {
            get => _isAbsoluteTimeRange;
            set
            {
                OnPropertyChanged(ref _isAbsoluteTimeRange, value);
                if (_errorsByPropertyName.Any(pn => pn.Key == "RelativeDuration")) RelativeDuration = "0"; //if error in Relative Duration was not corrected but clicked on Absolute Duration instead 
            }
        }
        public string[] RelativeDurationsList { get; set; } //list of nmumbers in Relative Duration combobox

        private RelativeUnit _relativeUnits;
        public RelativeUnit RelativeUnits
        {
            get => _relativeUnits;
            set
            {
                OnPropertyChanged(ref _relativeUnits, value);
            }
        }

        private string _relativeDuration;
        public string RelativeDuration
        {
            get => _relativeDuration;
            set
            {
                OnPropertyChanged(ref _relativeDuration, value);
                ValidateRelativeDuration();
            }
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set
            {
                OnPropertyChanged(ref _statusText, value);
            }
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set
            {
                OnPropertyChanged(ref _isError, value);
            }
        }

        private bool _isProgress;
        public bool IsProgress
        {
            get => _isProgress;
            set
            {
                OnPropertyChanged(ref _isProgress, value);
            }
        }

        private bool _someViewExist; //at least one row in main GridView exists
        public bool SomeViewExist
        {
            get => _someViewExist;
            set
            {
                OnPropertyChanged(ref _someViewExist, value);
            }
        }

        private ViewResult ViewResults { get; set; } = new ViewResult(); //Current data, incl. main GridView transferred from SQL to Main Window and further to export

        #endregion

        #region Default Constructor
        public MessagesViewModel(IServiceScopeFactory serviceScopeFactory) //because ViewModel is singleton, we need to create unique scopes for each service with shorter lifetime and caanot use DI directly
        {
            _serviceScopeFactory = serviceScopeFactory;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Version = string.Format("Syngo Log Reader - Version {0}.{1}.{2}",
                version.Major, version.Minor, version.Build);

            _goCommand = new DelegateCommandAsync(OnGoAsync, CanGo);
            OpenFileCommand = new DelegateCommandAsync(OnOpenFileAsync);
            CleanDatabaseCommand = new DelegateCommandAsync(CleanDatabase);
            ExportToXMLCommand = new DelegateCommand(OnExportToXML);
            SwapRelativeUnitsCommand = new DelegateCommand(OnSwapRelativeUnits);
            TrimTextCommand = new DelegateCommand(OnTrimText);
            AbortCommand = new DelegateCommand(OnAbort);

            RelativeDurationsList = new string[15];
            for (int i = 0; i < RelativeDurationsList.Length; i++)
            {
                RelativeDurationsList[i] = i.ToString();
            }

            RelativeUnits = new RelativeUnit { DaysOrHours = "days", IconName = "Calendar" };
            
            SeveritiesChecked = new ObservableCollection<MessageSeverity>(Variables.severities);
            foreach (var item in SeveritiesChecked) item.PropertyChanged += severity_PropertyChanged; //subscribe to each checkbox event via INPC-enabled model
            SeveritiesChecked.Where(s => s.Severity == "E").Single().IsChecked = true;

            CleanUI();
        }

        #endregion

        #region Command Handlers
        private void OnTrimText(object obj)
        {
            RelativeDuration = RelativeDuration.Trim();
        }
        private void OnSwapRelativeUnits(object obj)
        {
            switch (RelativeUnits.DaysOrHours)
            {
                case "days":
                    RelativeUnits = new RelativeUnit { DaysOrHours = "hours", IconName = "ClockOutline" };
                    break;
                case "hours":
                    RelativeUnits = new RelativeUnit { DaysOrHours = "days", IconName = "Calendar" };
                    break;
            }
        }
        private async Task OnOpenFileAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var fileNavigator = scope.ServiceProvider.GetService<IFileNavigator>();
                fileNavigator.StatusChanged += _fileNavigator_OnStatusChanged;
                cts = new CancellationTokenSource();
                await fileNavigator.OpenFileAsync(cts.Token);
                cts.Dispose();
            }

            if (!IsError)
            {
                Informer("file-loaded");
                await FileOpenedSuccessfully();
            } 
        }
        public async Task OnGoAsync()
        {
            MessageIdFilter = MessageIdFilter.Trim();
            SourceNameFilter = SourceNameFilter.Trim();
            filterSet = FilterBuilder();

            Informer("query-run");
            await SQLscope("read");
            LogList = ViewResults.LogList;
            Informer("result");
            previousStatus = StatusText;
        }
        private void OnExportToXML(object obj)
        {
            string saveFileResult;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var xmlWriter = scope.ServiceProvider.GetService<IXMLWriter>();
                saveFileResult = xmlWriter.SaveFile(ViewResults, filterSet);
            }
            
            Informer(saveFileResult);
        }
        private async Task CleanDatabase()
        {
            await SQLscope("clean");
            CleanUI();
        }
        private void OnAbort(object obj)
        {
            cts.Cancel();
        }

        #endregion

        #region Can Command handlers
        private bool CanGo()
        {
            bool goEnabled = !HasErrors && Variables.totalMessages > 0 && !IsProgress && atLeastOneSeverityCheked;
            return goEnabled;
        }
        #endregion

        #region Helpers
        internal FilterSet FilterBuilder()
        {
            var severetiesChecked = SeveritiesChecked.Where(cb => cb.IsChecked).Select(cb => cb.Severity).ToArray();
            filterSet = new FilterSet
            {
                SeveretiesChecked = severetiesChecked,
                MessageText = MessageTextFilter,
                MessageId = MessageIdFilter,
                SourceName = SourceNameFilter,
            };

            if (IsAbsoluteTimeRange)
            {
                filterSet.FromDt = FromDtFilter;
                filterSet.ToDt = ToDtFilter.AddSeconds(-ToDtFilter.Second).AddSeconds(59);
            }
            else
            {
                filterSet.ToDt = Variables.totalMaxDt;
                var duration = -int.Parse(RelativeDuration);
                if (duration == 0)
                {
                    filterSet.FromDt = Variables.totalMinDt;
                }
                else
                {
                    switch (RelativeUnits.DaysOrHours)
                    {
                        case "days":
                            filterSet.FromDt = Variables.totalMaxDt.AddSeconds(-Variables.totalMaxDt.Second).AddDays(duration);
                            break;
                        case "hours":
                            filterSet.FromDt = Variables.totalMaxDt.AddSeconds(-Variables.totalMaxDt.Second).AddHours(duration);
                            break;
                    }
                }

            }

            return filterSet;
        }

        //This is Status: string in the bottom of UI
        internal void Informer(string infoKey)
        {
            switch (infoKey)
            {
                case "result":
                var result = ViewResults.Counter;
                switch (result)
                {
                    case 0:
                        StatusText = " Nothing found";
                        break;
                    case 1:
                        StatusText = " 1 message found";
                        break;
                    default:
                        StatusText = " " + result.ToString() + " messages found";
                        break;
            }
                    break;

                case "file-cancel":
                    StatusText = previousStatus;
                    break;

                default:
                    StatusText = " " + Variables.states[infoKey].StatusText;
                    break;
        }
            
            SomeViewExist = ViewResults.Counter > 0;
            IsError = Variables.states[infoKey].IsError;
            IsProgress = Variables.states[infoKey].IsProgress;
            var popupText = Variables.states[infoKey].PopupText;

            if (popupText != null)
            {
                SystemSounds.Exclamation.Play();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new InfoDialog(popupText);
                    DialogHost.Show(dialog, "MainDialogHost", null, MainDialogClosed); 

                });
            }

            _goCommand.OnCanExecuteChanged();
        }

        internal async Task FileOpenedSuccessfully()
        {
            await UpdateHeader();

            FromDtFilter = Variables.totalMinDt;
            ToDtFilter = Variables.totalMaxDt;

            await OnGoAsync();
        }

        internal void CleanUI()
        {
            LogList = new List<Message>();
            ViewResults = new ViewResult();

            FromDtFilter = DateTime.Now.Date;
            ToDtFilter = DateTime.Now.Date + new TimeSpan(23, 59, 59);

            MessageTextFilter = "";
            MessageIdFilter = "";
            SourceNameFilter = "";

            RelativeDuration = "1";
            IsAbsoluteTimeRange = true;

            _ = UpdateHeader();
            Informer("empty");
            previousStatus = StatusText;
        }

        internal async Task UpdateHeader()
        {
            await SQLscope("count");
            SummaryHeader = new SummaryHeader().UpdateHeader();
        }

        internal async Task SQLscope(string selector)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var sqlActions = scope.ServiceProvider.GetService<ISQLActions>();

                switch (selector)
                {
                    case "read":
                        cts = new CancellationTokenSource();
                        ViewResults = await Task.Run(() => sqlActions.SQLReadAsync(filterSet, cts.Token).GetAwaiter().GetResult());
                        cts.Dispose();
                        break;

                    case "clean":
                        await sqlActions.SQLCleanAsync();
                        break;

                    case "count":
                        await Task.Run(() => sqlActions.SQLCountAsync().GetAwaiter().GetResult());
                        break;
                }
            }
        }

        #endregion

        #region Validation Handling

        private readonly Dictionary<string, List<string>> _errorsByPropertyName = new Dictionary<string, List<string>>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            _goCommand.OnCanExecuteChanged();
        }

        public bool HasErrors => _errorsByPropertyName.Any();

        public IEnumerable GetErrors(string propertyName)
        {
            return _errorsByPropertyName.ContainsKey(propertyName) ?
                   _errorsByPropertyName[propertyName] : null;
        }

        private void ValidateMessageId()
        {
            ClearErrors(nameof(MessageIdFilter));
            if (!string.IsNullOrWhiteSpace(MessageIdFilter))
            {
                if (!int.TryParse(MessageIdFilter.Trim(), out _))
                    AddError(nameof(MessageIdFilter), "The value must be a number");
            }
        }

        private void ValidateRelativeDuration()
        {
            ClearErrors(nameof(RelativeDuration));
            if (!string.IsNullOrWhiteSpace(RelativeDuration))
            {
                int duration = -1;
                if (!int.TryParse(RelativeDuration.Trim(), out duration) || duration < 0 || duration > 99)
                    AddError(nameof(RelativeDuration), "Allowed range from 0 to 99");
            }
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errorsByPropertyName.ContainsKey(propertyName))
                _errorsByPropertyName[propertyName] = new List<string>();

            if (!_errorsByPropertyName[propertyName].Contains(error))
            {
                _errorsByPropertyName[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }
        private void ClearErrors(string propertyName)
        {
            if (_errorsByPropertyName.ContainsKey(propertyName))
            {
                _errorsByPropertyName.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }
        #endregion

    }
}
