using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using UNI_Tools_AR.CreateFinish.FinishWall;

using System.Collections.Generic;
using Autodesk.Revit.DB.Architecture;

namespace UNI_Tools_AR.CreateFinish
{
    [Transaction(TransactionMode.Manual)]
    class CreateWallCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        /* */
        {
            Autodesk.Revit.UI.UIApplication uiApplication = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application application = uiApplication.Application;
            Autodesk.Revit.UI.UIDocument uiDocument = uiApplication.ActiveUIDocument;
            Autodesk.Revit.DB.Document document = uiDocument.Document;

            //CreateFinishWalls finishWallForm = new CreateFinishWalls(uiApplication, application, uiDocument, document);

            //finishWallForm.ShowDialog();

            Room room = document.GetElement(new ElementId(557001)) as Room;

            FinishWallItem wallItem = new FinishWallItem();

            BuilderWall builderWalls = new BuilderWall(document, room, new List<FinishWallItem>{ wallItem });

            using (Transaction t = new Transaction(document, "Test"))
            {
                t.Start();
                builderWalls.CreateFinishWall(0, 0, false);
                t.Commit();
            }


            return Result.Succeeded;
        }
    }
}
