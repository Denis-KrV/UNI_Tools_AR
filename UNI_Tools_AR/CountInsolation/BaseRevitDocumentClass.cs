using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNI_Tools_AR.CountInsolation
{

    internal class BaseRevitDocumentClass
    {
        public Autodesk.Revit.UI.UIApplication _uiApplication;
        public Autodesk.Revit.ApplicationServices.Application _application;
        public Autodesk.Revit.UI.UIDocument _uiDocument;
        public Autodesk.Revit.DB.Document _document;

        public Functions _functions =>
            new Functions(_uiApplication, _application, _uiDocument, _document);
    }
}
