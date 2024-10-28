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

            XYZ windowPoint = new XYZ(-0.234484077, -3.875063079, 9.678477690);

            IList<XYZ> insolationPoint = insolationObject.ReturnNonIntersectionObject(windowPoint);

            //using (Transaction t = new Transaction(document, "Test"))
            //{
            //    t.Start();

            //    XYZ windowPoint = glassObjects.GetCenterPointWindows().Last();

            //    IList<XYZ> insolationPoint = insolationObject.ReturnNonIntersectionObject(windowPoint);

            //    IList<GeometryObject> geometryArcObjects = insolationPoint.Select(xyz => Line.CreateBound(windowPoint, xyz)).ToList<GeometryObject>();
            //    geometryArcObjects.Add(Autodesk.Revit.DB.Point.Create(XYZ.Zero) );

            //    func.CreateDirectShape(document, geometryArcObjects);

            //    t.Commit();
            //}

            return Result.Succeeded;
        }
    }
}
