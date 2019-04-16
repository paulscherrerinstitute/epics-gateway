using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var exampleFilesPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ExampleFiles");
            AnomalyFilesList.ItemsSource = Directory.EnumerateFiles(exampleFilesPath).Concat(Directory.EnumerateFiles(@"C:\temp\t2\anomalies"));
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

        private void OpenGenerator(object sender, RoutedEventArgs e)
        {
            new GraphGenerator().ShowDialog();
        }
    }
}