using System;
using System.Windows;
using System.Windows.Forms;


namespace UNI_Tools_AR.CountInsolation
{
    public partial class InsolationProgressBar_Form : Window
    {
        static int _counter;
        static int _countItems;
        static string _nameEvent;
        int count = 0;

        public InsolationProgressBar_Form(int countItems, string nameEvent)
        {
            InitializeComponent();
            _countItems = countItems;
            InsolationProgressBar.Maximum = _countItems;
            _counter = 0;
            _nameEvent = nameEvent;

            Timer timer = new Timer();
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = 1000;
            timer.Start();
        }
        private void timerTick(object sender, EventArgs e)
        {
            count++;
            timerLabel.Content = $"{count} сек";
        }

        public void valueChanged()
        {
            _counter++;
            string content = $"{_nameEvent} {_counter} из {_countItems}";
            labelInfo.Content = content;
            InsolationProgressBar.Value = _counter;
            System.Windows.Forms.Application.DoEvents();
        }

        private void InsolationProgressBar_ValueChanged(
            object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
