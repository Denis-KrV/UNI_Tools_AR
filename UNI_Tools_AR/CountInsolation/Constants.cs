using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNI_Tools_AR.CountInsolation
{
    internal class Constants
    {
        public const string exceptionTitle = "Возникла ошибка";

        public const string exceptionActiveViewNotThreeD = 
            "Активный вид должен быть 3Д видом";
        public const string exceptionNotSearchSunInActiveView = 
            "На активном виде не найдено солнце.";

        public const string nameColorThreeDView = 
            "Расчет инсоляции: Цветовая схема.";

        public const string nameTransaction = "Подсчет инсоляции.";
        public const string nameProcess = "Обработка окон:";

        public const string nameTimeParameter = "UNI_Инсоляция_Время";

        public const double confirmTimeSeconds = 2.5 * 60 * 60;

        public const string nameTypeParameter = "UNI_Инсоляция_Тип";
        
        public const double noTimeType = 1;
        public const double averageTypeTime = 2;
        public const double confirmTypeTime = 3;

        public const double angleOneHour = 15;
        public const double angleOneMinute = angleOneHour / 60;
        public const double angleOneSecond = angleOneMinute / 60;
        public const double countRotation = 360 / angleOneMinute;
        public const double angleRotation = (2 * Math.PI) / countRotation;

        public const int windowCategoryIntId = -2000014;

        public const string insolationPointFamilyName = "InsolationPoint";
        public const int insolationPointCategoryIntId = -2001360;
    }
}
