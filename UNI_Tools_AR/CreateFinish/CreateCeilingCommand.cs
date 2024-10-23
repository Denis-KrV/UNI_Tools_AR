using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using UNI_Tools_AR.CreateFinish.FinishCeiling;
using UNI_Tools_AR.CreateFinish.FinishWall;

namespace UNI_Tools_AR.CreateFinish
{
    [Transaction(TransactionMode.Manual)]
    internal class CreateCeilingCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        /* */
        {
            Autodesk.Revit.UI.UIApplication uiApplication = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application application = uiApplication.Application;
            Autodesk.Revit.UI.UIDocument uiDocument = uiApplication.ActiveUIDocument;
            Autodesk.Revit.DB.Document document = uiDocument.Document;

            CreateFinishCeiling createFinishCeiling = 
                new CreateFinishCeiling(uiApplication, application, uiDocument, document);

            createFinishCeiling.ShowDialog();

            return Result.Succeeded;
        }
    }
}