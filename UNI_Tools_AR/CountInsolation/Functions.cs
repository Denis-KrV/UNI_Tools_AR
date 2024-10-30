using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNI_Tools_AR.CountInsolation
{
    internal class Functions
    {
        private UIApplication _uiApplication;
        private Application _application;
        private UIDocument _uiDocument;
        private Document _document;

        private const int windowCategoryIntId = -2000014;

        private Options _options => new Options 
        { 
            IncludeNonVisibleObjects = true, 
            DetailLevel = ViewDetailLevel.Fine 
        };

        public Functions(
            UIApplication uiApplication,
            Application application,
            UIDocument uiDocument,
            Document documen
        )
        {
            _uiApplication = uiApplication;
            _application = application;
            _uiDocument = uiDocument;
            _document = documen;
        }

        public DirectShape CreateDirectShape(Document document, IList<GeometryObject> gElements)
        {
            DirectShape directShape = DirectShape.CreateElement(document, new ElementId(-2000011));
            if (directShape.IsValidShape(gElements))
            {
                directShape.SetShape(gElements);
            }
            return directShape;
        }

        public bool CheckParameterInProject(string nameParameter)
        {
            Element windows = new FilteredElementCollector(_document)
                .OfCategoryId(new ElementId(windowCategoryIntId))
                .WhereElementIsNotElementType()
                .First();
        }

        public bool CheckParameterInWindow(string nameParameter)
        {
            Element windows = new FilteredElementCollector(_document)
                .OfCategoryId(new ElementId(windowCategoryIntId))
                .WhereElementIsNotElementType()
                .First();
        }

        public SunAndShadowSettings GetSunAndShadowSettings()
        {
            Element sunAndShadowElement = 
                new FilteredElementCollector(_document, _document.ActiveView.Id)
                    .OfClass(typeof(SunAndShadowSettings))
                    .FirstElement();

            if (sunAndShadowElement is SunAndShadowSettings) 
            {
                return sunAndShadowElement as SunAndShadowSettings;
            }
            return null;
        }

        public IList<XYZ> HalfPastPoint(IList<XYZ> points)
        {
            XYZ fstPoint = null;

            IList<XYZ> result = new List<XYZ>();
            foreach (XYZ point in points)
            {
                if (fstPoint is null)
                {
                    fstPoint = point;
                    result.Add(point);
                    continue;
                }


                XYZ centerPoint = (fstPoint + point) / 2;

                result.Add(centerPoint);
                result.Add(point);

                fstPoint = point;
            }

            return result;
        }

        public IList<Element> GetAllWindows()
        {
            IList<Element> windows = new FilteredElementCollector(_document, _document.ActiveView.Id)
                .OfCategoryId(new ElementId(windowCategoryIntId))
                .WhereElementIsNotElementType()
                .ToElements();

            return windows;
        }

        public XYZ GetCenterPointElement(Element element)
        {
            BoundingBoxXYZ boundingBoxXYZ = element
                .get_Geometry(_options)
                .GetBoundingBox();
            return (boundingBoxXYZ.Max + boundingBoxXYZ.Min) / 2; 
        }

        public double ConvertRadToDegree(double radAngle)
        {
            return 180 * radAngle / Math.PI;
        }

        public double GetSumAngleInPoints(IList<XYZ> points)
        {
            XYZ fstPoint = null;

            double angle = 0;

            foreach (XYZ point in points)
            {
                if (fstPoint is null) 
                { 
                    fstPoint = point; 
                    continue; 
                }

                double radAngle = fstPoint.AngleTo(point);
                angle += ConvertRadToDegree(radAngle);

                fstPoint = point;
            }
            return angle;
        }
    }
}
