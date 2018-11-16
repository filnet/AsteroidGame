using System.Windows;

namespace AsteroidGame
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MyWindow : Window
    {
        public MyWindow()
        {
            InitializeComponent();
        }

        public void setSelected(object obj)
        {
            this.PropertyGrid1.SelectedObject = obj;
        }
    }
}
