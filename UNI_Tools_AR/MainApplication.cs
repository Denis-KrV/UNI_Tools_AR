using Autodesk.Revit.UI;
using UNI_Tools_AR.CopyScheduleFilter;
using UNI_Tools_AR.CountCoefficient;
using UNI_Tools_AR.CreateFinish;
using UNI_Tools_AR.Properties;
using UNI_Tools_AR.UpdateLegends;
using VCRevitRibbonUtil;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using System.Linq;
using Microsoft.VisualBasic;
using Panel = VCRevitRibbonUtil.Panel;


namespace UNI_Tools_AR
{
    public class MainApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            VCRevitRibbonUtil.Tab UNITab = Ribbon
                .GetApplicationRibbon(application).Tab("UNI Tools AR");

            VCRevitRibbonUtil.Panel CoefficientPanel = UNITab.Panel("Расчет коэффициентов");

            CoefficientPanel
                .CreateButton<CreateCoefficentCommand>("Таблица коэффициентов", "Таблица", b => b
                    .SetLargeImage(Resources.CoeffTable_32x32)
                    .SetSmallImage(Resources.CoeffTable_16x16)
                    .SetLongDescription("Создает/Обновляет коэффциенты глобальных параметров")
                );

            CoefficientPanel
                .CreateButton<CountCoefficentCommand>("Перерасчет коэффициенты", "Обновить", b => b
                    .SetLargeImage(Resources.UpdateCoeff_32x32)
                    .SetSmallImage(Resources.UpdateCoeff_16x16)
                    .SetLongDescription("Обновляет коэффциенты глобальных параметров")
                );

            VCRevitRibbonUtil.Panel LegendPanel = UNITab.Panel("Легенды");

            LegendPanel
                .CreateButton<UpdateLegendsCommand>("Обновление легенд", "Обновление легенд", b => b
                    .SetLargeImage(Resources.UpdateLegends_32x32)
                    .SetSmallImage(Resources.UpdateLegends_16x16)
                    .SetLongDescription("Создает/Обновляет легенды из вида легенды где размещено семейство рамки.")
                );

            VCRevitRibbonUtil.Panel FinishPanel = UNITab.Panel("Отделка");

            FinishPanel
                .CreateButton<CreateWallCommand>("Отделка стен", "Отделка стен", b => b
                    .SetLargeImage(Resources.CreateFinishWall_32x32)
                    .SetSmallImage(Resources.CreateFinishWall_16x16)
                    .SetLongDescription("Создает отделку стен по помещениям."))
                
                .CreateButton<CreateFloorCommand>("Отделка полов", "Отделка полов", b => b
                    .SetLargeImage(Resources.FinishFloor_32x32)
                    .SetSmallImage(Resources.FinishFloor_16x16)
                    .SetLongDescription("Создает отделку полов по помещениям."))

                .CreateButton<CreateCeilingCommand>("Отделка потолков", "Отделка потолков", b => b
                    .SetLargeImage(Resources.FinishCeiling_32x32)
                    .SetSmallImage(Resources.FinishCeiling_16x16)
                    .SetLongDescription("Создает отделку потолков по помещениям.")
                );

            VCRevitRibbonUtil.Panel FilterSchedule = UNITab.Panel("Спецификации");

            FilterSchedule
                .CreateButton<CopyScheduleFilterCommand>("Копирование параметров", "Копирование параметров", b => b
                    .SetLargeImage(Resources.UpdateCoeff_32x32)
                    .SetSmallImage(Resources.UpdateCoeff_16x16)
                    .SetLongDescription("Test"));

            Panel SheetPanel = UNITab.Panel("Листы");
            SheetPanel.CreateButton<BatchRenameSheetsCommand>(
                "Одинаковые номера",
                "Одинаковые\nномера",
                b => b.SetLongDescription("Выдаёт всем выделенным листам одинаковый видимый номер (Sheet Number)."));


            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class BatchRenameSheetsCommand : IExternalCommand
    {
        private const char Zwsp = '\u200B';

        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = data.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            string baseNumber = Interaction.InputBox(
                "Введите номер, который должен отображаться у всех выделенных листов.",
                "Одинаковые номера листов",
                "A-100");

            if (string.IsNullOrWhiteSpace(baseNumber))
                return Result.Cancelled;

            var sheets = uiDoc.Selection.GetElementIds()
                                       .Select(id => doc.GetElement(id))
                                       .OfType<ViewSheet>()
                                       .ToList();

            if (sheets.Count == 0)
            {
                TaskDialog.Show("Одинаковые номера листов", "Сначала выделите листы в диспетчере!");
                return Result.Cancelled;
            }

            var usedNumbers = new FilteredElementCollector(doc)
                                .OfClass(typeof(ViewSheet))
                                .Cast<ViewSheet>()
                                .Where(vs => !sheets.Contains(vs))
                                .Select(vs => vs.SheetNumber)
                                .ToHashSet();

            int suffix = 0;
            using (Transaction t = new Transaction(doc, "Set identical visible sheet numbers"))
            {
                t.Start();
                foreach (var sheet in sheets)
                {
                    string candidate;
                    do
                    {
                        candidate = suffix == 0 ? baseNumber : baseNumber + new string(Zwsp, suffix);
                        suffix++;
                    } while (usedNumbers.Contains(candidate));

                    sheet.get_Parameter(BuiltInParameter.SHEET_NUMBER).Set(candidate);
                    usedNumbers.Add(candidate);
                }
                t.Commit();
            }

            TaskDialog.Show("Одинаковые номера листов", $"Изменено листов: {sheets.Count}");
                     return Result.Succeeded;
        }
    }

}
