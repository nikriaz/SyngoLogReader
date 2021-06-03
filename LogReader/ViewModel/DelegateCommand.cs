using System;
using System.Windows.Input;

namespace LogReader.ViewModel
{
    //This is classic version of Commands for ViewModel
    class DelegateCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action<object> _executeAction;
        private readonly Func<object, bool> _canExecuteAction;

        public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecuteAction = null)
        {
            _executeAction = executeAction;
            _canExecuteAction = canExecuteAction;
        }

        #region ICommand Members

        public void Execute(object parameter)
        {
            _executeAction(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteAction?.Invoke(parameter) ?? true;
        }

        public void OnCanExecuteChanged() 
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
