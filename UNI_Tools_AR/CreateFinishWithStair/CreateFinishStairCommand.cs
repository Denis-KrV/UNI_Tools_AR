using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNI_Tools_AR.CreateFinish.FinishWall;

namespace UNI_Tools_AR.CreateFinishWithStair
{
    [Transaction(TransactionMode.Manual)]
    internal class CreateFinishStairCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        /* */
        {
            Autodesk.Revit.UI.UIApplication uiApplication = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application application = uiApplication.Application;
            Autodesk.Revit.UI.UIDocument uiDocument = uiApplication.ActiveUIDocument;
            Autodesk.Revit.DB.Document document = uiDocument.Document;

            Funcitons func = new Funcitons();

            Stairs stair = document.GetElement(new ElementId(549642)) as Stairs;

            BuilderFinishStair builderFinishStair = new BuilderFinishStair(document, stair);

            using (Transaction t = new Transaction(document, "test"))
            {
                t.Start();
                builderFinishStair.CreateRiserFinish();
                builderFinishStair.CreateFlankFinish();
                builderFinishStair.CreateTreadFinish();
                builderFinishStair.CreateOtherFloorFinish();
                t.Commit();
            }

            //CreateFinishWalls finishWallForm = new CreateFinishWalls(uiApplication, application, uiDocument, document);
            //finishWallForm.ShowDialog();

            return Result.Succeeded;
        }
    }
}
