using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace UNI_Tools_AR.UpdateLegends
{
    [Transaction(TransactionMode.Manual)]

    internal class UpdateLegendsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            
            Legends legends = new Legends(doc);

            string exceptionTitle = "Возникла ошибка";

            Family family = legends.getFamilyCreateImage();
            if (family is null)
            {
                TaskDialog.Show(exceptionTitle, 
                    "Семейство рамки будет загружено, перед следующим запуском " 
                    + "программы расставьте эти рамки на необходимых легендах.");
                legends.loadFamilyCreateImage();
                return Result.Succeeded;
            }

            IList<View> legendsView = legends.allLegendsView();
            IList<FamilyInstance> legendsInstance = legends.allLegendsInstance();
            IList<Element> legendsComponent = legends.allLegendsComponent();

            if (legendsView is null)
            {
                string exceptMessage = "В проекте нет ни одного вида легенды";
                TaskDialog.Show(exceptionTitle, exceptMessage);
                return Result.Failed;
            }
            else if (legendsInstance is null)
            {
                string exceptMessage = "В проекте не размещено ни одного элемента семейства рамки";
                TaskDialog.Show(exceptionTitle, exceptMessage);
                return Result.Failed;
            }
            else if (legendsComponent is null)
            {
                string exceptMessage = "В проекте не размещено ни одного компонента легенды на виде легенд";
                TaskDialog.Show(exceptionTitle, exceptMessage);
                return Result.Failed;
            }

            UpdateLegends_Form form = new UpdateLegends_Form(doc);
            form.ShowDialog();

            return Result.Succeeded;
        }
    }
}
