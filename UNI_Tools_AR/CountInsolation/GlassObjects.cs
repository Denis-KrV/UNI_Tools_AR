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
        private IList<Element> windows => _functions.GetAllWindows();
        private Options _options => new Options { IncludeNonVisibleObjects = true };

        public GlassObjects(
            Autodesk.Revit.UI.UIApplication uiApplication,
            Autodesk.Revit.ApplicationServices.Application application,
            Autodesk.Revit.UI.UIDocument uiDocument,
            Autodesk.Revit.DB.Document document
        ) 
        {
            _uiApplication = uiApplication;
            _application = application;
            _document = document;
            _uiDocument = uiDocument;
        }

        public IList<XYZ> GetCenterPointWindows()
        {
            IList<XYZ> result = new List<XYZ>();
            foreach (Element window in windows)
            {
                BoundingBoxXYZ boundingBoxXYZ = window.get_Geometry(_options).GetBoundingBox();
                result.Add((boundingBoxXYZ.Min + boundingBoxXYZ.Max) / 2);
            }
            return result;
        }
    }
}
