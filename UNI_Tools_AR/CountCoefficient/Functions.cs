using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;


namespace UNI_Tools_AR.CountCoefficient
{
    internal class Functions
    {
        static Autodesk.Revit.DB.Document _doc;
        static Autodesk.Revit.ApplicationServices.Application _app;
        public Functions(
            Autodesk.Revit.DB.Document doc,
            Autodesk.Revit.ApplicationServices.Application app)
        {
            _doc = doc;
            _app = app;
        }

        public IList<Element> GetViewSchedules()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc);
            IList<Element> viewSchedules = collector.OfClass(typeof(ViewSchedule)).ToElements();
            return viewSchedules;
        }

        public Dictionary<string, ViewSchedule> NameForSchedule()
        {
            Dictionary<string, ViewSchedule> nameForSchedule = new Dictionary<string, ViewSchedule>();

            foreach (Element viewSchedule in GetViewSchedules())
            {
                string name = viewSchedule.get_Parameter(BuiltInParameter.VIEW_NAME).AsString();
                nameForSchedule[name] = (ViewSchedule)viewSchedule;
            }

            return nameForSchedule;
        }

        public double GetLastValueFromSchedule(ViewSchedule viewSchedule)
        {
            TableData tableData = viewSchedule.GetTableData();
            TableSectionData bodySectionData = tableData.GetSectionData(SectionType.Body);

            int lastIndexRow = bodySectionData.NumberOfRows - 1;
            int lastIndexCollumn = bodySectionData.NumberOfColumns - 1;

            string valueCell = "0";
            try
            {
                valueCell = bodySectionData.GetCellText(lastIndexRow, lastIndexCollumn);
            }
            catch { }

            double resultIndex = 0;
            bool resultCell = double.TryParse(valueCell, out resultIndex);
            return resultIndex;
        }

        public GlobalParameter GetOrCreateGlobalParameterForName(string nameGlobalParameter)
        {
            Dictionary<string, GlobalParameter> collecitonGlobalParameters = NameForGlobalParmaeters();
            if (collecitonGlobalParameters.ContainsKey(nameGlobalParameter))
            {
                return collecitonGlobalParameters[nameGlobalParameter];
            }
            else
            {
                // Updated to use ForgeTypeId instead of ParameterType
                GlobalParameter newGlobalParameter = GlobalParameter.Create(_doc, nameGlobalParameter, SpecTypeId.Number);
                return newGlobalParameter;
            }
        }
        public GlobalParameter GetGlobalParameterNumberTypeForName(string nameGlobalParameter)
        {
            Dictionary<string, GlobalParameter> collecitonGlobalParameters = NameForGlobalParmaeters();
            if (collecitonGlobalParameters.ContainsKey(nameGlobalParameter))
            {
                GlobalParameter globalParameter = collecitonGlobalParameters[nameGlobalParameter];
                Definition parameterDefinition = globalParameter.GetDefinition();
                if (parameterDefinition.ParameterType is ParameterType.Number)
                {
                    return globalParameter;
                }
                else
                {
                    string messageNotNumberType = $"Тип данных в глобальном параметре {nameGlobalParameter} " +
                        $"не 'число', удалите и пересоздайте параметр с помощью функции 'Создать расчетные коэффициенты'";
                    System.Windows.MessageBox.Show(messageNotNumberType, "Предупреждение");
                    return null;
                }
            }
            else
            {
                string messageParameterNotFound = $"Глобального параметра {nameGlobalParameter} не существует, " +
                    "создайте параметр с помощью функции 'Создать расчетные коэффициенты'";
                System.Windows.MessageBox.Show(messageParameterNotFound, "Предупреждение");
                return null;
            }
        }

        public Dictionary<string, GlobalParameter> NameForGlobalParmaeters()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc);
            IList<Element> globalParameters = collector.OfClass(typeof(GlobalParameter)).ToElements();

            Dictionary<string, GlobalParameter> collecitonGlobalParameters = new Dictionary<string, GlobalParameter>();
            foreach (Element globalParameter in globalParameters)
            {
                string nameParameter = ((GlobalParameter)globalParameter).GetDefinition().Name;
                collecitonGlobalParameters[nameParameter] = (GlobalParameter)globalParameter;
            }
            return collecitonGlobalParameters;
        }

        public void SetDoubleValueForGlobalParameter(GlobalParameter globalParameter, double value)
        {
            ParameterValue parameterValue = new DoubleParameterValue(value);
            globalParameter.SetValue(parameterValue);
        }

        public void CalculateResultValue(IList<CountItemTable> countItemTables)
        {
            foreach (CountItemTable item in countItemTables)
            {
                double fst_value = item.FstValue;
                double scd_value = item.ScdValue;
                double result;

                switch (item.VarOperations)
                {
                    case "Сложение":
                        result = fst_value + scd_value; break;
                    case "Вычитание":
                        result = fst_value - scd_value; break;
                    case "Умножение":
                        result = fst_value * scd_value; break;
                    case "Деление":
                        if (scd_value == 0) { result = 0; break; }
                        else { result = fst_value / scd_value; break; }
                    case "Деление без остатка":
                        if (scd_value == 0) { result = 0; break; }
                        else { result = (int)fst_value / (int)scd_value; break; }
                    case "Возведение в степень":
                        result = Math.Pow(fst_value, scd_value); break;
                    default:
                        result = 0.0; break;
                }
                switch (item.Rounded)
                {
                    case "0": { result = Math.Round(result); break; }
                    case "0.0": { result = Math.Round(result, 1); break; }
                    case "0.00": { result = Math.Round(result, 2); break; }
                    case "0.000": { result = Math.Round(result, 3); break; }
                    case "0.0000": { result = Math.Round(result, 4); break; }
                    case "0.00000": { result = Math.Round(result, 5); break; }
                }
                item.ResultValue = result;
            }
        }

        public ViewSchedule GetScheduleForName(string name)
        {
            Dictionary<string, ViewSchedule> scheduleColleciton = NameForSchedule();
            if (scheduleColleciton.ContainsKey(name))
            {
                return scheduleColleciton[name];
            }
            else
            {
                return null;
            }
        }

        public void SetValuesFromSchedule(IList<CountItemTable> countItemTables)
        {
            foreach (CountItemTable countItemTable in countItemTables)
            {
                string fstNameSchedule = countItemTable.FstNameSchedule;
                string scdNameSchedule = countItemTable.ScdNameSchedule;

                if (fstNameSchedule != "- -")
                {
                    ViewSchedule fstSchedule = GetScheduleForName(fstNameSchedule);
                    if (fstSchedule is null)
                    {
                        countItemTable.FstNameSchedule = "{ Удалено }";
                        countItemTable.FstValue = 0.0;
                    }
                    else { countItemTable.FstValue = GetLastValueFromSchedule(fstSchedule); }
                }
                if (scdNameSchedule != "- -")
                {
                    ViewSchedule scdSchedule = GetScheduleForName(scdNameSchedule);
                    if (scdNameSchedule is null)
                    {
                        countItemTable.ScdNameSchedule = "*Удаленная спецификация";
                        countItemTable.ScdValue = 0.0;
                    }
                    else { countItemTable.ScdValue = GetLastValueFromSchedule(scdSchedule); }
                }
            }
        }

        public string GetDocumentTitle()
        {
            string userName = _app.Username;
            string documentTitle = _doc.Title;
            if (documentTitle.Contains(userName))
            {
                documentTitle = documentTitle.Replace($"_{userName}", "");
            }
            return documentTitle;
        }
    }
}
