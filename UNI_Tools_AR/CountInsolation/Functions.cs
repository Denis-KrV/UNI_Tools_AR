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

        public void CreateDirectShape(Document document, IList<GeometryObject> gElements)
        {
            DirectShape directShape = DirectShape.CreateElement(document, new ElementId(-2000011));
            if (directShape.IsValidShape(gElements))
            {
                directShape.SetShape(gElements);
            }
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
            ElementId categoryIdWindow = new ElementId(-2000014);
            
            IList<Element> windows = new FilteredElementCollector(_document, _document.ActiveView.Id)
                .OfCategoryId(categoryIdWindow)
                .WhereElementIsNotElementType()
                .ToElements();

            return windows;
        }

    }
}
