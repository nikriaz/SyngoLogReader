using System.Windows.Controls;

namespace LogReader.View
{
    /// <summary>
    /// Interaction logic for InfoDialog.xaml
    /// </summary>
    public partial class InfoDialog : UserControl
    {
        public string PopupText { get; }
        public InfoDialog(string popupText)
        {
            PopupText = popupText;
            InitializeComponent();
        }

    }
}
