using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace UNI_Tools_AR.BatchRenameSheet
{
    [Transaction(TransactionMode.Manual)]
    public class BatchRenameSheetCommand : IExternalCommand
    {
        // Основной невидимый символ Unicode
        private const char ZwspMain = '\u200B'; // Zero Width Space
        
        // Дополнительный символ для увеличения уникальности в партии
        private const char ZwspAlt = '\u200C'; // Zero Width Non-Joiner
        
        // Символ для отделения партий при сортировке (используется с разным количеством)
        private const char ZwspSort = '\uFEFF'; // Zero Width No-Break Space
        
        // Счетчик сеансов нумерации
        private static int batchCounter = 0;

        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = data.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            
            // Увеличиваем счетчик для этой партии выделения
            batchCounter++;
            
            string baseNumber = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите начальный номер для нумерации (например, 1).",
                "Последовательная нумерация листов",
                "1");
            if (string.IsNullOrWhiteSpace(baseNumber))
                return Result.Cancelled;

            // Пытаемся преобразовать введенное значение в число
            if (!int.TryParse(baseNumber, out int startNumber))
            {
                TaskDialog.Show("Ошибка", "Введите корректное числовое значение!");
                return Result.Cancelled;
            }

            // Получаем выбранные элементы
            ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            
            // Проверка выбраны ли листы
            if (selectedIds.Count == 0)
            {
                TaskDialog.Show("Последовательная нумерация листов", "Выберите листы для нумерации!");
                return Result.Cancelled;
            }
            
            // Получаем выбранные листы
            List<ViewSheet> selectedSheets = new List<ViewSheet>();
            
            foreach (ElementId id in selectedIds)
            {
                Element element = doc.GetElement(id);
                if (element is ViewSheet)
                {
                    selectedSheets.Add(element as ViewSheet);
                }
            }
            
            if (selectedSheets.Count == 0)
            {
                TaskDialog.Show("Последовательная нумерация листов", "Выбранные элементы не содержат листов!");
                return Result.Cancelled;
            }

            // Получаем все используемые номера листов
            HashSet<string> usedNumbers = new HashSet<string>();
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            foreach (ViewSheet sheet in collector.OfClass(typeof(ViewSheet)))
            {
                if (!selectedSheets.Contains(sheet))
                {
                    usedNumbers.Add(sheet.SheetNumber);
                }
            }

            using (Transaction t = new Transaction(doc, "Последовательная нумерация листов"))
            {
                t.Start();
                int sequenceNumber = startNumber;
                
                // Создаем уникальный сортировочный маркер партии
                // Используем разное количество символов для разных партий:
                // Партия 1 - нет маркера
                // Партия 2 - ZwspSort (1 символ)
                // Партия 3 - ZwspSort + ZwspSort (2 символа)
                // И так далее...
                string sortMarker = "";
                if (batchCounter > 1)
                {
                    // Начиная со второй партии добавляем маркер в начало номера
                    sortMarker = new string(ZwspSort, batchCounter - 1);
                }
                
                // Создаем уникальный маркер партии для разрешения коллизий
                string batchMarker = new string(ZwspMain, batchCounter % 5); 
                
                foreach (ViewSheet sheet in selectedSheets)
                {
                    string baseSheetNumber = sequenceNumber.ToString();
                    
                    // Добавляем сортировочный маркер в НАЧАЛО номера для влияния на сортировку
                    // Добавляем маркер партии в КОНЕЦ номера для разрешения коллизий в разных партиях
                    string newSheetNumber = sortMarker + baseSheetNumber + batchMarker;
                    
                    // Если такой номер уже используется, добавляем дополнительные невидимые символы
                    int suffix = 0;
                    while (usedNumbers.Contains(newSheetNumber))
                    {
                        suffix++;
                        // Дополнительные символы добавляем в конец
                        newSheetNumber = sortMarker + baseSheetNumber + batchMarker + new string(ZwspAlt, suffix);
                    }
                    
                    // Устанавливаем новый номер листа
                    sheet.get_Parameter(BuiltInParameter.SHEET_NUMBER).Set(newSheetNumber);
                    usedNumbers.Add(newSheetNumber);
                    
                    // Увеличиваем номер для следующего листа
                    sequenceNumber++;
                }
                
                t.Commit();
            }

            TaskDialog.Show("Последовательная нумерация листов", $"Пронумеровано листов: {selectedSheets.Count}");
            return Result.Succeeded;
        }
    }
}
