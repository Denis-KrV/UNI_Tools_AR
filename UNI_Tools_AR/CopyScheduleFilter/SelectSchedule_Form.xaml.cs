using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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
    public partial class SelectSchedule_Form : Window
    {
        public bool Cancel = false;

        public SelectSchedule_Form(IList<ViewSchedule> viewSchedules)
        {
            InitializeComponent();
            DataViewSchedules.ItemsSource = GetSchedulesInDataGrid(viewSchedules);
        }

        private IList<ScheduleInDataGrid> GetSchedulesInDataGrid(IList<ViewSchedule> viewSchedules)
        {
            return viewSchedules
                .Select(schedule => new ScheduleInDataGrid(schedule))
                .OrderBy(schedule => schedule.scheduleName)
                .ToList();
        }
        
        public IList<ViewSchedule> GetSelectedSchedule()
        {
            var selectedItems = DataViewSchedules.SelectedItems;

            IList<ViewSchedule> selectedSchedules = new List<ViewSchedule>();
            foreach (var scheduleInDataGrid in selectedItems)
            {
                ScheduleInDataGrid castScheduleInDataGrid = 
                    scheduleInDataGrid as ScheduleInDataGrid;
                ViewSchedule schedule = castScheduleInDataGrid.viewSchedule;
                selectedSchedules.Add(schedule);
            }

            return selectedSchedules;
        }

        private void Form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!confirmButton.IsMouseOver)
            {
                Cancel = true;
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    class ScheduleInDataGrid
    {
        public ViewSchedule viewSchedule;
        public string scheduleName => viewSchedule
            .get_Parameter(BuiltInParameter.VIEW_NAME)
            .AsString();

        public ScheduleInDataGrid(ViewSchedule viewSchedule)
        {
            this.viewSchedule = viewSchedule;
        }
    }
}
