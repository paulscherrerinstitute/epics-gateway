using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GraphAnomalyVisualizer
{
    /// <summary>
    /// Interaction logic for FilePicker.xaml
    /// </summary>
    public partial class FilePicker : Window
    {
        public FilePicker()
        {
            InitializeComponent();
            AnomalyFilesList.ItemsSource = Directory.EnumerateFiles(@"C:\temp\t2\anomalies");
            AnomalyFilesList.SelectionMode = SelectionMode.Single;

            KeyUp += (sender, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                }
            };
        }

        private void Item_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item)
            {
                new MainWindow((string)item.Content).ShowDialog();
            }
        }
    }
}