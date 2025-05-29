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
        // Невидимый символ Unicode для создания уникальных номеров
        private const char Zwsp = '\u200B';
        
        // Автоматический выбор режима (true = использовать невидимые символы)
        private const bool AUTO_MODE_INVISIBLE_CHARS = true;

        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = data.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            
            // Автоматически используем режим с невидимыми символами
            bool useInvisibleChars = AUTO_MODE_INVISIBLE_CHARS;
            
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

            var sheets = uiDoc.Selection.GetElementIds()
                                       .Select(id => doc.GetElement(id))
                                       .OfType<ViewSheet>()
                                       .OrderBy(sheet => sheet.Name) // Сортировка листов
                                       .ToList();
            if (sheets.Count == 0)
            {
                TaskDialog.Show("Последовательная нумерация листов", "Выберите листы для нумерации!");
                return Result.Cancelled;
            }

            // Получаем все используемые номера листов (кроме выбранных)
            var usedNumbers = new FilteredElementCollector(doc)
                                .OfClass(typeof(ViewSheet))
                                .Cast<ViewSheet>()
                                .Where(vs => !sheets.Contains(vs))
                                .Select(vs => vs.SheetNumber)
                                .ToHashSet();

            using (Transaction t = new Transaction(doc, "Последовательная нумерация листов"))
            {
                t.Start();
                int sequenceNumber = startNumber;
                
                foreach (var sheet in sheets)
                {
                    string newSheetNumber;
                    
                    if (useInvisibleChars)
                    {
                        // Вариант с невидимыми символами
                        string baseSheetNumber = sequenceNumber.ToString();
                        int suffix = 0;
                        
                        do
                        {
                            newSheetNumber = suffix == 0 ? baseSheetNumber : baseSheetNumber + new string(Zwsp, suffix);
                            suffix++;
                        } while (usedNumbers.Contains(newSheetNumber));
                    }
                    else
                    {
                        // Вариант с пропуском занятых номеров
                        do
                        {
                            newSheetNumber = sequenceNumber.ToString();
                            sequenceNumber++;
                        } while (usedNumbers.Contains(newSheetNumber));
                    }
                    
                    sheet.get_Parameter(BuiltInParameter.SHEET_NUMBER).Set(newSheetNumber);
                    usedNumbers.Add(newSheetNumber);
                    
                    // Увеличиваем номер только для варианта с невидимыми символами
                    if (useInvisibleChars)
                        sequenceNumber++;
                }
                
                t.Commit();
            }

            TaskDialog.Show("Последовательная нумерация листов", $"Пронумеровано листов: {sheets.Count}");
            return Result.Succeeded;
        }
    }
}
