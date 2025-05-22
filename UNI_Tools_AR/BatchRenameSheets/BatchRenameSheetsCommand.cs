using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BatchRenameSheets
{
    [Transaction(TransactionMode.Manual)]
    public class BatchRenameSheetsCommand : IExternalCommand
    {
        private const char Zwsp = '\u200B';

        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = data.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            string baseNumber = Microsoft.VisualBasic.Interaction.InputBox(
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