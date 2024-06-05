using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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

namespace UNI_Tools_AR.UpdateLegends
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBar : Window
    {
        static int _counter;
        static int _countItems;
        int count = 0;
        public ProgressBar(int countItems)
        {
            InitializeComponent();
            _countItems = countItems;
            progressBar.Maximum = _countItems;
            _counter = 0;

            Timer timer = new Timer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = 1000; // интервал в миллисекундах
            timer.Start();
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            count++;

            timerLabel.Content = $"{count.ToString()} сек";

        }

        public void valueChanged()
        {
            _counter++;
            string content = $"Обработано легенд {_counter} из {_countItems}";
            labelInfo.Content = content;
            progressBar.Value = _counter;
            System.Windows.Forms.Application.DoEvents();
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
