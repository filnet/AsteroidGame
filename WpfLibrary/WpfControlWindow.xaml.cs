using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace WpfLibrary
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WpfControlWindow : Window
    {
        public ObservableCollection<ComboBoxItem> RendererItems { get; set; }

        public ComboBoxItem SelectedRendererItem
        {
            get {
                return selectedRendererItem;
            }
            set
            {
                selectedRendererItem = value;
                string name = selectedRendererItem.Content.ToString();
                RendererPropertyGrid.SelectedObject = rendererMap[name];
            }
        }

        private Dictionary<string, object> rendererMap;
        private ComboBoxItem selectedRendererItem;

        public WpfControlWindow()
        {
            RendererItems = new ObservableCollection<ComboBoxItem>();
            DataContext = this;
            InitializeComponent();
        }

        public void SetSelected1(object obj)
        {
            PropertyGrid1.SelectedObject = obj;
        }

        public void SetSelected2(object obj)
        {
            PropertyGrid2.SelectedObject = obj;
        }

        public void SetSelected3(object obj)
        {
            PropertyGrid3.SelectedObject = obj;
        }

        public void SetRendererMap(Dictionary<string, object> map)
        {
            RendererItems.Clear();
            rendererMap = map;
            bool first = true;
            foreach (KeyValuePair<string, object> rendererKVP in rendererMap)
            {
                RendererItems.Add(new ComboBoxItem { Content = rendererKVP.Key, IsSelected = first,
                    HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Stretch });
                first = false;
            }
            ComboBoxItem item = RendererItems[0];
            string name = item.Content.ToString();
            RendererPropertyGrid.SelectedObject = rendererMap[name];
        }

        public void Refresh()
        {
            //object obj = this.PropertyGrid1.SelectedObject;
            //this.PropertyGrid1.SelectedObject = null;
            //this.PropertyGrid1.SelectedObject = obj;
            PropertyGrid1.RefreshPropertyList();
        }
    }
}