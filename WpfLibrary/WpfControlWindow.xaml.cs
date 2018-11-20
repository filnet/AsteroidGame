using System.Windows;

namespace WpfLibrary
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WpfControlWindow : Window
    {
        public WpfControlWindow()
        {
            InitializeComponent();
        }

        public void setSelected(object obj)
        {
            this.PropertyGrid1.SelectedObject = obj;
        }
    }
}