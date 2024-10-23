using System;
using System.Windows;
using System.Windows.Forms;

namespace UNI_Tools_AR.UpdateLegends
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBar : Window
    {
        static int _counter;
        static int _countItems;
        static string _nameEvent;
        int count = 0;
        public ProgressBar(int countItems, string nameEvent)
        {
            InitializeComponent();
            _countItems = countItems;
            progressBar.Maximum = _countItems;
            _counter = 0;
            _nameEvent = nameEvent;

            Timer timer = new Timer();
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = 1000; // интервал в миллисекундах
            timer.Start();
        }
        private void timerTick(object sender, EventArgs e)
        {
            count++;

            timerLabel.Content = $"{count.ToString()} сек";

        }

        public void valueChanged()
        {
            _counter++;
            string content = $"{_nameEvent} {_counter} из {_countItems}";
            labelInfo.Content = content;
            progressBar.Value = _counter;
            System.Windows.Forms.Application.DoEvents();
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
