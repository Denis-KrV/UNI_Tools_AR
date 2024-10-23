using Autodesk.Revit.DB;

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace UNI_Tools_AR.CountCoefficient
{
    public partial class CoefItems_Form : Window
    {
        static Functions _func;
        static JsonItem _jsonItem;

        static Autodesk.Revit.DB.Document _document;
        static Autodesk.Revit.ApplicationServices.Application _application;
        public CoefItems_Form(
            Autodesk.Revit.DB.Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            _document = doc;
            _application = app;
            _func = new Functions(_document, _application);
            _jsonItem = new JsonItem(_func.GetDocumentTitle());

            InitializeComponent();
            dataGrig.CanUserAddRows = false;
            jsonFileName.Content = $"Файл: {_jsonItem.GetCoefficientFile()}";
            BindCountItems();
            _func.SetValuesFromSchedule((IList<CountItemTable>)dataGrig.ItemsSource);
        }

        private void grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _func.SetValuesFromSchedule((IList<CountItemTable>)dataGrig.ItemsSource);
        }
        private void grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CountItemTable path = dataGrig.SelectedItem as CountItemTable;
            MessageBox.Show(" ID: " + path.Name);
        }
        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (CountItemTable countItemTable in dataGrig.ItemsSource)
                {
                    GlobalParameter globalParameter = _func.GetOrCreateGlobalParameterForName(countItemTable.Name);
                    _func.SetDoubleValueForGlobalParameter(globalParameter, countItemTable.ResultValue);
                }
                MessageBox.Show("Глобальные параметры созданы/изменены.", "Информация");
                _jsonItem.SaveItemsTableInJson((IList<CountItemTable>)dataGrig.ItemsSource);
                Close();
            }
            catch
            {
                MessageBox.Show("Поправьте введенные данные", "Ошибка валидации");
            }
        }
        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IList<CountItemTable> dataGrigItems = (IList<CountItemTable>)dataGrig.ItemsSource;
                CountItemTable newItem = new CountItemTable
                {
                    Name = "Переименуйте",
                    FstNameSchedule = "- -",
                    FstValue = 0,
                    ScdNameSchedule = "- -",
                    ScdValue = 0,
                    VarOperations = "Сложение",
                    ResultValue = 0,
                    Rounded = "- -"
                };
                dataGrigItems.Add(newItem);
                dataGrig.ItemsSource = dataGrigItems;
                dataGrig.Items.Refresh();
            }
            catch
            {
                MessageBox.Show("Поправьте введенные данные", "Ошибка валидации");
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dataGrig.IsArrangeValid)
                {
                    IList<CountItemTable> dataGridItems = (IList<CountItemTable>)dataGrig.ItemsSource;
                    dataGridItems.RemoveAt(dataGridItems.Count - 1);
                    dataGrig.ItemsSource = dataGridItems;
                    dataGrig.Items.Refresh();
                }
            }
            catch { MessageBox.Show("Поправьте введенные данные", "Ошибка валидации"); }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _func.SetValuesFromSchedule((IList<CountItemTable>)dataGrig.ItemsSource);
                _func.CalculateResultValue((IList<CountItemTable>)dataGrig.ItemsSource);
                dataGrig.Items.Refresh();
            }
            catch { MessageBox.Show("Поправьте введенные данные", "Ошибка валидации"); }
        }

        private void BindCountItems()
        {
            dataGrig.ItemsSource = _jsonItem.GetJsonCountItemData();
            FstNameSchedule.ItemsSource = GetNameSchedules();
            ScdNameSchedule.ItemsSource = GetNameSchedules();
            VarOperations.ItemsSource = new List<string>() {
                "Сложение", "Вычитание", "Умножение", "Деление",
                "Деление без остатка", "Возведение в степень"
            };
            Rounded.ItemsSource = new List<string>()
            {
                "- -", "0", "0.0", "0.00", "0.000", "0.0000", "0.00000"
            };
        }

        public IList<string> GetNameSchedules()
        {
            IList<Element> viewShedules = _func.GetViewSchedules();

            IList<string> names = new List<string>() { "- -", "{ Удалено }" };
            foreach (Element schedule in viewShedules)
            {
                string scheduleName = schedule.get_Parameter(BuiltInParameter.VIEW_NAME).AsString();
                names.Add(scheduleName);
            }
            return names;
        }

        private void changeJsonFile_Click(object sender, RoutedEventArgs e)
        {
            string newCoeficientFile = _jsonItem.ChangePathJsonCoefficientFile();
            BindCountItems();
            dataGrig.Items.Refresh();
        }
    }
    class CountItemTable
    {
        public string Name { get; set; }
        public string FstNameSchedule { get; set; }
        public double FstValue { get; set; }
        public string ScdNameSchedule { get; set; }
        public double ScdValue { get; set; }
        public string VarOperations { get; set; }
        public double ResultValue { get; set; }
        public string Rounded { get; set; }
    }

}
