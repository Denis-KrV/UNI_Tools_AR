using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VCRevitRibbonUtil;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using UNI_Tools_AR.UpdateLegends;
using UNI_Tools_AR.Properties;

namespace UNI_Tools_AR
{
    public class MainApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            Ribbon.GetApplicationRibbon(application).Tab("UNI Tools AR").Panel("Легенды")
                .CreateButton<UpdateLegendsCommand>("Обновление легенд", "Обновление легенд", b=> 
                    b.SetLargeImage(Resources.UpdateLegends_32x32)
                    .SetSmallImage(Resources.UpdateLegends_16x16)
                    .SetLongDescription("Создает/Обновляет легенды из вида легенды где размещено семейство рамки."));
            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
