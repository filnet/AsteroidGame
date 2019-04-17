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

        public void SetSelected1(object obj)
        {
            this.PropertyGrid1.SelectedObject = obj;
        }

        public void SetSelected2(object obj)
        {
            this.PropertyGrid2.SelectedObject = obj;
        }

        public void SetSelected3(object obj)
        {
            this.PropertyGrid3.SelectedObject = obj;
        }

        public void Refresh()
        {
            //object obj = this.PropertyGrid1.SelectedObject;
            //this.PropertyGrid1.SelectedObject = null;
            //this.PropertyGrid1.SelectedObject = obj;
            this.PropertyGrid1.RefreshPropertyList();
        }
    }
}