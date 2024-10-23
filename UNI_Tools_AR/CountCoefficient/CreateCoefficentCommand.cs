using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;


namespace UNI_Tools_AR.CountCoefficient
{
    [Transaction(TransactionMode.Manual)]
    internal class CreateCoefficentCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Autodesk.Revit.UI.UIApplication uiapp = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Autodesk.Revit.UI.UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.DB.Document doc = uidoc.Document;

            Functions functions = new Functions(doc, app);

            string documentTitle = functions.GetDocumentTitle();

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

            CoefItems_Form form = new CoefItems_Form(doc, app);


            using (Transaction t = new Transaction(doc, "Создание глобальных параметров и изменение их значений"))
            {
                t.Start();
                form.ShowDialog();
                t.Commit();
            }

            return Result.Succeeded;
        }

    }

}
