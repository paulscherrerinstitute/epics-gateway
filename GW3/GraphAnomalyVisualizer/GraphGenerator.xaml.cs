using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for GraphGenerator.xaml
    /// </summary>
    public partial class GraphGenerator : Window
    {
        private GraphGeneratorViewModel ViewModel;

        public GraphGenerator()
        {
            ViewModel = new GraphGeneratorViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void ExportClick(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".xml",
                FileName = "EXPORT_GRAPH.xml",
                CheckPathExists = true,
                OverwritePrompt = true,
                Title = "Export graph data",
                Filter = "XML-Files (*.xml)|*.xml"
            };
            var result = saveDialog.ShowDialog();
            if(result.HasValue && result.Value)
            {
                ViewModel.ExportToFile(saveDialog.FileName);
            }
        }
    }
}