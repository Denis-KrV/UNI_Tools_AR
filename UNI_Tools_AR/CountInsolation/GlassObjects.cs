using Autodesk.Revit.Creation;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNI_Tools_AR.CountInsolation
{
    internal class GlassObjects : BaseRevitDocumentClass
    {
        public InsolationObject insolationObject;
        public IList<GlassObject> windows => GetGlassObjects();

        public GlassObjects(
            Autodesk.Revit.UI.UIApplication uiApplication,
            Autodesk.Revit.ApplicationServices.Application application,
            Autodesk.Revit.UI.UIDocument uiDocument,
            Autodesk.Revit.DB.Document document,
            InsolationObject insolationObject
        )
        {
            _uiApplication = uiApplication;
            _application = application;
            _document = document;
            _uiDocument = uiDocument;
            this.insolationObject = insolationObject;
        }

        private IList<GlassObject> GetGlassObjects()
        {
            IList<GlassObject> glassObjects = new List<GlassObject>();
            foreach (Element window in _functions.GetAllWindows())
            {
                XYZ centerPoint = _functions.GetCenterPointElement(window);
                GlassObject glassObject = 
                    new GlassObject(window, centerPoint, insolationObject);
                glassObjects.Add(glassObject);
            }
            return glassObjects;
        }
    }

    class GlassObject
    {
        public InsolationObject insolationObject;
        public Element window;
        public XYZ centerPoint;
        public IList<SunSegment> segments => 
            insolationObject.ReturnNonIntersectionObject(centerPoint);
        public double sumTimeSun => segments
            .Select(segment => segment.time).Sum();

        public GlassObject(Element glassObject, XYZ centerPoint, InsolationObject insolationObject)
        {
            window = glassObject;
            this.centerPoint = centerPoint;
            this.insolationObject = insolationObject;
        }

        public XYZ insolaTionPoint()
        {
            return new XYZ(1, 1, 1);
        }

        public void SetTime(string nameParameterSunTime)
        {
            window.LookupParameter(nameParameterSunTime).Set(sumTimeSun);
        }

        public void SetTypeTime(string nameParameterSunType)
        {
            double timeType = GetTypeTime();
            window.LookupParameter(nameParameterSunType).Set(timeType);
        }

        private double GetTypeTime()
        {
            foreach (SunSegment confirmSegment in segments)
            {
                double time = confirmSegment.time;
                if (time >= Constants.confirmTimeSeconds)
                {
                    return Constants.confirmTypeTime;
                }
            }
            if (sumTimeSun > 0)
            {
                return Constants.averageTypeTime;
            }
            return Constants.noTimeType;
        }
    }
}
