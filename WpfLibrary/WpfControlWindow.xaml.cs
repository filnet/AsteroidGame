using System.Windows;
using System.Windows.Controls;

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

        public void setSelected1(object obj)
        {
            this.PropertyGrid1.SelectedObject = obj;
        }

        public void setSelected2(object obj)
        {
            this.PropertyGrid2.SelectedObject = obj;
        }

    }
}