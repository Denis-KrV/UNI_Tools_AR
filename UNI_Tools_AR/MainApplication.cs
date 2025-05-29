using Autodesk.Revit.UI;
using System;
using System.Windows.Media.Imaging;
using UNI_Tools_AR.BatchRenameSheet;
using UNI_Tools_AR.CopyScheduleFilter;
using UNI_Tools_AR.CountCoefficient;
using UNI_Tools_AR.CreateFinish;
using UNI_Tools_AR.Properties;
using UNI_Tools_AR.UpdateLegends;
using VCRevitRibbonUtil;
using BatchRenameSheets; // Добавьте ссылку на пространство имен BatchRenameSheets

namespace UNI_Tools_AR
{
    public class MainApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // Используем имя "UNI_Tools_AR" вместо "UNI Tools"
            string tabName = "UNI_Tools_AR";
            
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Вкладка уже существует, продолжаем настройку
            }

            VCRevitRibbonUtil.Tab UNITab = Ribbon
                .GetApplicationRibbon(application).Tab(tabName);  // Используем ту же переменную tabName

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

            Panel FilterSchedule = UNITab.Panel("Спецификации");

            FilterSchedule
                .CreateButton<CopyScheduleFilterCommand>("Копирование параметров", "Копирование параметров", b => b
                    .SetLargeImage(Resources.UpdateCoeff_32x32)
                    .SetSmallImage(Resources.UpdateCoeff_16x16)
                    .SetLongDescription("Test"));

            // Добавляем новую панель "Листы"
            Panel SheetsPanel = UNITab.Panel("Листы");

            // Добавляем кнопку "Одинаковые номера" для плагина BatchRenameSheets
            SheetsPanel
                .CreateButton<BatchRenameSheetsCommand>("Одинаковые номера", "Одинаковые\nномера", b => b
                    .SetLargeImage(Resources.SameSheetNumber_32х32)  // Используем иконку для одинаковых номеров
                    .SetSmallImage(Resources.SameSheetNumber_16х16)
                    .SetLongDescription("Выдаёт всем выделенным листам одинаковый видимый номер (Sheet Number).")
                )
                // Добавляем новую кнопку "Последовательные номера"
                .CreateButton<BatchRenameSheetCommand>("Последовательные номера", "Последовательные\nномера", b => b
                    .SetLargeImage(Resources.SameSheetNumber_32х32) // Используем ту же иконку
                    .SetSmallImage(Resources.SameSheetNumber_16х16)
                    .SetLongDescription("Назначает выделенным листам последовательную нумерацию.")
                );

            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
