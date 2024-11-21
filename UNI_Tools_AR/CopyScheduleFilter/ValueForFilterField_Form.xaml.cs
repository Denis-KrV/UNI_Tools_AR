using Autodesk.Revit.DB;
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

namespace UNI_Tools_AR.CopyScheduleFilter
{
    public partial class ValueForFilterField_Form : Window
    {
        public bool Cancel = false;

        private Document document;
        public ValueForFilterField_Form(SchedulableField field, Document document)
        {
            InitializeComponent();
            this.document = document;
            NameField.Content = field.GetName(document);
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!confirmButton.IsMouseOver)
            {
                Cancel = true;
            }
        }
    }
}
