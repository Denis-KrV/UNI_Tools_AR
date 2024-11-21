using Autodesk.Revit.Attributes;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNI_Tools_AR.CopyScheduleFilter
{
    [Transaction(TransactionMode.Manual)]
    internal class CopyScheduleFilterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Application app = uiapp.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            ScheduleCopyFilter copyfilter = new ScheduleCopyFilter(uiapp, uidoc, doc);
            copyfilter.StartCopy();

            return Result.Succeeded;
        }
    }
}
