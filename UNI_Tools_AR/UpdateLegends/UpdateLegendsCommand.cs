using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

namespace UNI_Tools_AR.UpdateLegends
{
    [Transaction(TransactionMode.Manual)]

    internal class UpdateLegendsCommand : IExternalCommand
    {
        const string exceptionTitle = "Возникла ошибка";
        const string nameTimeParameter = "";
        const string nameTypeParameter = "";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Autodesk.Revit.UI.UIApplication uiapp = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Autodesk.Revit.UI.UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.DB.Document doc = uidoc.Document;

            Functions func = new Functions();

            Legends legends = new Legends(doc);

            Family family = legends.GetFamilyCreateImage();
            if (family is null)
            {
                TaskDialog.Show(exceptionTitle,
                    "Семейство рамки будет загружено, перед следующим запуском "
                    + "программы расставьте эти рамки на необходимых легендах.");
                legends.LoadFamilyCreateImage();
                return Result.Succeeded;
            }

            IList<View> legendsView = legends.AllLegendsView();
            IList<FamilyInstance> legendsInstance = legends.AllLegendsInstance();
            IList<Element> legendsComponent = legends.AllLegendsComponent();

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
            else if (!func.CheckUniqueTypeElmenetsFromLegendComponent(doc, uidoc, legends.CreateLegendViewItems()))
            {
                return Result.Failed;
            }
            UpdateLegends_Form form = new UpdateLegends_Form(doc, app);
            form.ShowDialog();

            return Result.Succeeded;
        }
    }
}
