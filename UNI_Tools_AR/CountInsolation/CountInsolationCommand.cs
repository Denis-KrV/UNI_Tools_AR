using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using UNI_Tools_AR.CreateFinish.FinishWall;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace UNI_Tools_AR.CountInsolation
{
    [Transaction(TransactionMode.Manual)]
    internal class CountInsolationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        /* */
        {
            Autodesk.Revit.UI.UIApplication uiApplication = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application application = uiApplication.Application;
            Autodesk.Revit.UI.UIDocument uiDocument = uiApplication.ActiveUIDocument;
            Autodesk.Revit.DB.Document document = uiDocument.Document;

            Functions func = new Functions(uiApplication, application, uiDocument, document);

            SunAndShadowSettings sunAndShadowSettings = func.GetSunAndShadowSettings();

            if (sunAndShadowSettings is null)
            {
                string titleDialog = "Ошибка";
                string exceptMessage = "На активном виде не найдено солнце.";
                TaskDialog.Show(titleDialog, exceptMessage);
                return Result.Failed;
            }

            InsolationObject insolationObject = new InsolationObject(document, sunAndShadowSettings);

            using (Transaction t = new Transaction(document, "Test"))
            {
                t.Start();
                IList<GeometryObject> sunLines = insolationObject.SunTimePoint
                    .Select(xyz => Line.CreateBound(insolationObject.CentralPoint, xyz) as GeometryObject)
                    .ToList();

                func.CreateDirectShape(document, sunLines);
                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
