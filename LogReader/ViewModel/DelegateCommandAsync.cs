using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LogReader.ViewModel
{
    //This is async version of ViewModel Commands implementation
    public interface IDelegateCommandAsync : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }
    public class DelegateCommandAsync : IDelegateCommandAsync
    {
        public event EventHandler CanExecuteChanged;

        private bool _isExecuting;
        private readonly Func<Task> _executeAction;
        private readonly Func<bool> _canExecuteAction;

        public DelegateCommandAsync(Func<Task> executeAction, Func<bool> canExecuteAction = null)
        {
            _executeAction = executeAction;
            _canExecuteAction = canExecuteAction;
        }

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    _isExecuting = true;
                    await _executeAction();
                }
                finally
                {
                    _isExecuting = false;
                }
            }

            OnCanExecuteChanged();
        }
        public bool CanExecute()
        {
            return !_isExecuting && (_canExecuteAction?.Invoke() ?? true);
        }

        public void OnCanExecuteChanged()
        {
            if (CanExecuteChanged is not null)
            {
                Application.Current.Dispatcher.BeginInvoke(new EventHandler(CanExecuteChanged), this, EventArgs.Empty); 
            }
        }

        #region Explicit implementations
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            _ = ExecuteAsync();
        }
        #endregion
    }
}
