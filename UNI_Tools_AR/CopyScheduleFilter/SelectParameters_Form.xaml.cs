using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UNI_Tools_AR.CopyScheduleFilter
{
    public partial class SelectParameters_Form : Window
    {
        public bool Cancel = false;

        public SelectParameters_Form(Document document, IList<SchedulableField> schedulableFields)
        {
            InitializeComponent();
            DataViewField.ItemsSource = GetFieldInDataGrid(document, schedulableFields);
        }

        private IList<FieldInDataGrid> GetFieldInDataGrid(
            Document document, IList<SchedulableField> schedulableFields)
        {
            return schedulableFields
                .Select(field => new FieldInDataGrid(field, document))
                .OrderBy(field => field.fieldName)
                .ToList();
        }

        public SchedulableField GetSelectedField()
        {
            var selectedFields = DataViewField.SelectedItem;
            if (selectedFields is null) return null;
            return (selectedFields as FieldInDataGrid).field;
        }

        private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!confirmButton.IsMouseOver)
            {
                Cancel = true;
            }
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    class FieldInDataGrid
    {
        private Document document;
        public SchedulableField field;
        public string fieldName => field.GetName(document);

        public FieldInDataGrid(SchedulableField field, Document document)
        {
            this.field = field;
            this.document = document;
        }
    }
}
