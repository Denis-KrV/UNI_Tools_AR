using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            IList<Element> insolationPoint = func.GetAllInsolationPoints(); 

            if (!(activeView.ViewType is ViewType.ThreeD))
            {
                TaskDialog.Show(
                    Constants.exceptionTitle,
                    Constants.exceptionActiveViewNotThreeD
                );
                return Result.Failed;
            }
            if (sunAndShadowSettings is null)
            {
                TaskDialog.Show(
                    Constants.exceptionTitle,
                    Constants.exceptionNotSearchSunInActiveView
                );
                return Result.Failed;
            }
            if (!func.CheckParameterPorjectInDocument(Constants.nameTimeParameter))
                return Result.Failed;
            if (!func.CheckParameterPorjectInDocument(Constants.nameTypeParameter))
                return Result.Failed;
            if (!(insolationPoint is null)) 
                return Result.Failed;

            InsolationObject insolationObject = 
                new InsolationObject(document, sunAndShadowSettings, func);

            GlassObjects glassObjects = 
                new GlassObjects(uiApplication, application, uiDocument, document, insolationObject);

            using (Transaction t = new Transaction(document, Constants.nameTransaction))
            {
                t.Start();
                InsolationProgressBar_Form progressBar =
                    new InsolationProgressBar_Form(glassObjects.windows.Count(), Constants.nameProcess);

                foreach (GlassObject glassObject in glassObjects.windows)
                {
                    glassObject.SetTime(Constants.nameTimeParameter);
                    glassObject.SetTypeTime(Constants.nameTypeParameter);
                    progressBar.valueChanged();
                    progressBar.Show();
                }
                progressBar.Close();
                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
