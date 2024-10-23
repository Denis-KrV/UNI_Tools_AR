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
    }
}
