using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UNI_Tools_AR.UpdateLegends
{
    /// <summary>
    /// Interaction logic for UpdateLegends_Form.xaml
    /// </summary>
    public partial class UpdateLegends_Form : Window
    {
        static Functions _func;
        static Document _doc;
        public UpdateLegends_Form(Document doc)
        {
            _func = new Functions();
            _doc = doc;

            InitializeComponent();
            tbParameterName.Text = _func.settingsLineValue(0);
        }

        private void runButton(object sender, RoutedEventArgs e)
        {
            string oldParameterName = _func.settingsLineValue(0);
            _func.replaceSettingsLineValue(oldParameterName, tbParameterName.Text);
            Legends legends = new Legends(_doc);
            IList<LegendViewItem> legendsViewItems = legends.createLegendViewItems();

            if (!(_func.checkElementsForParmaeter(legendsViewItems, tbParameterName.Text)))
            {
                Close();
            }
            UpdaterImage updaterImage = new UpdaterImage(_doc, legendsViewItems);
            updaterImage.updateImages(tbParameterName.Text);
            Close();
        }

        private void cancelButton(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
