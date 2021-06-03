using System.ComponentModel;
using System.Runtime.CompilerServices;

//This is the base class for Models implemented INPC (INotifyPropertyChanged), particuralry to use in ViewModel

namespace LogReader.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged; 
        protected virtual void OnPropertyChanged<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            {
                field = newValue; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
