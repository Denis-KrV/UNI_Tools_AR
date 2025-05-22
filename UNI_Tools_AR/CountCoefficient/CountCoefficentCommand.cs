using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.IO;

namespace UNI_Tools_AR.CountCoefficient
{
    [Transaction(TransactionMode.Manual)]
    internal class CountCoefficentCommand : IExternalCommand
    {
        static Autodesk.Revit.UI.UIApplication _uiapp;
        static Autodesk.Revit.ApplicationServices.Application _app;

        static Functions _function;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Autodesk.Revit.UI.UIApplication uiapp = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Autodesk.Revit.UI.UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.DB.Document doc = uidoc.Document;

            _function = new Functions(doc, app);

            string documentTitle = _function.GetDocumentTitle();

            JsonItem jsonItem = new JsonItem(documentTitle);

            string coeficientFile = jsonItem.GetCoefficientFile();

            if (coeficientFile is null | !(File.Exists(coeficientFile)))
            {
                coeficientFile = jsonItem.ChangePathJsonCoefficientFile();
            }
            if (coeficientFile is null)
            {
                return Result.Failed;
            }

            IList<CountItemTable> countItemTables = jsonItem.GetJsonCountItemData();
            _function.SetValuesFromSchedule(countItemTables);
            _function.CalculateResultValue(countItemTables);

            using (Transaction t = new Transaction(doc, "Перерасчет глобальных параметров"))
            {
                t.Start();
                foreach (CountItemTable countItemTable in countItemTables)
                {
                    GlobalParameter globalParameter = _function.GetGlobalParameterNumberTypeForName(countItemTable.Name);
                    if (globalParameter == null)
                    {
                        TaskDialog.Show("Ошибка", "Параметры не были перерасчитаны.");
                        return Result.Failed;
                    }
                    _function.SetDoubleValueForGlobalParameter(globalParameter, countItemTable.ResultValue);
                }
                t.Commit();
            }
            TaskDialog.Show("Информация", "Параметры были перерасчитаны.");

            jsonItem.SaveItemsTableInJson(countItemTables);

            return Result.Succeeded;
        }

    }

}
