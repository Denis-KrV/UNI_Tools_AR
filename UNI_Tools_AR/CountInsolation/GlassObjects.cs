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
        public IList<GlassObject> windows => _functions.GetAllWindows()
            .Select(element => new GlassObject(element, _functions.GetCenterPointElement(element)))
            .ToList();

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
    }

    class GlassObject
    {
        public Element glassObject;
        public XYZ centerPoint;
        public IList<SunSegment> segments = new List<SunSegment>();

        public GlassObject(Element glassObject, XYZ centerPoint)
        {
            this.glassObject = glassObject;
            this.centerPoint = centerPoint;
        }
    }
}
