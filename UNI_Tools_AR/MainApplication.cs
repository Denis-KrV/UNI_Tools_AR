using Autodesk.Revit.UI;
using UNI_Tools_AR.CountCoefficient;
using UNI_Tools_AR.CreateFinish;
using UNI_Tools_AR.Properties;
using UNI_Tools_AR.UpdateLegends;
using VCRevitRibbonUtil;

namespace UNI_Tools_AR
{
    public class MainApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            VCRevitRibbonUtil.Tab UNITab = Ribbon.GetApplicationRibbon(application).Tab("UNI Tools AR");

            Panel CoefficientPanel = UNITab.Panel("Расчет коэффициентов");
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

            Panel LegendPanel = UNITab.Panel("Легенды");

            LegendPanel
                .CreateButton<UpdateLegendsCommand>("Обновление легенд", "Обновление легенд", b => b
                    .SetLargeImage(Resources.UpdateLegends_32x32)
                    .SetSmallImage(Resources.UpdateLegends_16x16)
                    .SetLongDescription("Создает/Обновляет легенды из вида легенды где размещено семейство рамки.")
                );

            Panel FinishPanel = UNITab.Panel("Отделка");

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

            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
