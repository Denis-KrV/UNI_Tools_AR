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
using System.Windows;

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

            View activeView = document.ActiveView;

            SunAndShadowSettings sunAndShadowSettings = func.GetSunAndShadowSettings();

            if (!(activeView.ViewType is ViewType.ThreeD))
            {
                string titleDialog = "Ошибка";
                string exceptMessage = "Активный вид должен быть 3Д видом";
                TaskDialog.Show(titleDialog, exceptMessage);
                return Result.Failed;
            }

            if (sunAndShadowSettings is null)
            {
                string titleDialog = "Ошибка";
                string exceptMessage = "На активном виде не найдено солнце.";
                TaskDialog.Show(titleDialog, exceptMessage);
                return Result.Failed;
            }

            InsolationObject insolationObject = new InsolationObject(document, sunAndShadowSettings, func);
            GlassObjects glassObjects = new GlassObjects(uiApplication, application, uiDocument, document);

            XYZ centralPoint = insolationObject.CentralPoint;

            foreach (GlassObject window in glassObjects.windows)
            {
                using (Transaction t = new Transaction(document, "Test"))
                {
                    t.Start();
                    XYZ centerPoint = window.centerPoint;
                    IList<SunSegment> insolationSegments = insolationObject.ReturnNonIntersectionObject(centerPoint);

                    foreach (SunSegment insolation in insolationSegments)
                    {
                        IList<GeometryObject> geometryObjects = insolation.points
                            .Select(xyz => Line.CreateBound(centerPoint, xyz + centerPoint))
                            .ToList<GeometryObject>();

                        DirectShape directShape = func.CreateDirectShape(document, geometryObjects);
                        directShape.LookupParameter("Комментарии").Set($"{insolation.angle}");
                    }
                    t.Commit();
                }
            }



            return Result.Succeeded;
        }
    }
}
