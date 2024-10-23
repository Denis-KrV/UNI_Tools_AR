using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using UNI_Tools_AR.CreateFinish.FinishFloor;

namespace UNI_Tools_AR.CreateFinish
{
    [Transaction(TransactionMode.Manual)]
    internal class CreateFloorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        /* */
        {
            UIApplication uiApplication = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application application = uiApplication.Application;
            UIDocument uiDocument = uiApplication.ActiveUIDocument;
            Document document = uiDocument.Document;

            CreateFinishFloor createFinishFloor = new CreateFinishFloor(uiApplication, application, uiDocument, document);
            createFinishFloor.ShowDialog();

            return Result.Succeeded;
        }
    }
}
