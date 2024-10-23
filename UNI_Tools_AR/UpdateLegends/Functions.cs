using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UNI_Tools_AR.UpdateLegends
{
    internal class Functions
    {
        const string nameFolderImage = "images";
        const string localSettingsPath = "\\Resources\\Settings.ini";

        public Parameter GetImageParameter(Element elementType, string parameterName)
        {
            string exceptTitle = "Сообщение об ошибке.";

            var parameter = elementType.LookupParameter(parameterName);
            if (!(parameter is Parameter))
            {
                string categoryName = elementType.Category.Name;
                TaskDialog.Show(exceptTitle, $"У типоразмеров категории '{categoryName}' нет параметра '{parameterName}'.");
                return null;
            }
            else if (parameter.Definition.ParameterType != ParameterType.Image)
            {
                TaskDialog.Show(exceptTitle, $"Тип данных в параметре '{parameterName}' не является изоображением.");
                return null;
            }
            return parameter;
        }

        public IList<Element> GetAllElementsType(Autodesk.Revit.DB.Document _doc)
        {
            return new FilteredElementCollector(_doc).WhereElementIsElementType().ToElements();
        }

        public void ClearImageParameter(Autodesk.Revit.DB.Document _doc, IList<Element> filterElmentsType, string parameterName)
        {
            IList<Element> allElmentsType = GetAllElementsType(_doc);
            IList<Parameter> filteredParameter = new List<Parameter>();

            ElementId nullElementId = new ElementId(-1);

            foreach (Element el in allElmentsType)
            {
                Parameter parameter = el.LookupParameter(parameterName);
                if ((parameter is Parameter) & !(filterElmentsType.Contains(el)))
                {
                    if (!(parameter.IsReadOnly) & (parameter.AsElementId() != nullElementId))
                    {
                        filteredParameter.Add(parameter);
                    }
                }
            }

            string nameTask = "Чистка параметров";

            ProgressBar progressBar = new ProgressBar(filteredParameter.Count(), nameTask);

            using (Transaction t = new Transaction(_doc, nameTask))
            {
                t.Start();
                progressBar.Show();
                foreach (Parameter parameter in filteredParameter)
                {
                    if (parameter.AsElementId().IntegerValue != -1)
                    {
                        _doc.Delete(parameter.AsElementId());
                    }
                    parameter.Set(nullElementId);
                    progressBar.valueChanged();
                }
                progressBar.Close();
                t.Commit();
            }
        }

        public bool IsMeCheckoutElement(
            Autodesk.Revit.DB.Document doc, Autodesk.Revit.ApplicationServices.Application app, IList<ElementId> elementIds)
        {
            foreach (ElementId elementId in elementIds)
            {
                WorksharingTooltipInfo worksharingInfo = WorksharingUtils.GetWorksharingTooltipInfo(doc, elementId);
                if ((worksharingInfo.Owner != "") & (worksharingInfo.Owner != app.Username))
                {
                    TaskDialog.Show("Ошибка", $"Елементы заняты пользователем {worksharingInfo.Owner}" +
                        $" попросите его освободить элементы");
                    return false;
                }
            }
            return true;
        }

        public IList<Element> GetAllViewSchedules(Autodesk.Revit.DB.Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            return collector.OfClass(typeof(ViewSchedule)).ToElements();
        }

        private void SetPointScheduleSheetInstance(IList<ScheduleSheetInstance> scheduleInstances, XYZ value)
        {
            foreach (ScheduleSheetInstance scheduleInstance in scheduleInstances)
            {
                XYZ Point = scheduleInstance.Point;
                scheduleInstance.Point = Point - value;
            }
        }
        public void UpdateAllSchedules(Autodesk.Revit.DB.Document doc)
        {
            IList<Element> viewSchedules = GetAllViewSchedules(doc);

            string nameTask = "Обновление спек.";

            ProgressBar progressBar = new ProgressBar(viewSchedules.Count(), nameTask);
            using (Transaction t = new Transaction(doc, nameTask))
            {
                t.Start();
                progressBar.Show();
                foreach (Element schedule in viewSchedules)
                {
                    ViewSchedule viewSchedule = (ViewSchedule)schedule;
                    ScheduleDefinition scheduleDefinition = viewSchedule.Definition;
                    try
                    {
                        ElementCategoryFilter categoryFilter =
                            new ElementCategoryFilter(BuiltInCategory.OST_ScheduleGraphics);

                        IList<ElementId> scheduleSheetInstancesId =
                            viewSchedule.GetDependentElements(categoryFilter);
                        IList<ScheduleSheetInstance> scheduleSheetInstances = new List<ScheduleSheetInstance>();

                        foreach (ElementId scheduleSheetId in scheduleSheetInstancesId)
                        {
                            Element scheduleSheetInstance = doc.GetElement(scheduleSheetId);
                            scheduleSheetInstances.Add((ScheduleSheetInstance)scheduleSheetInstance);
                        }

                        bool valueItemized = !(scheduleDefinition.IsItemized);
                        scheduleDefinition.IsItemized = valueItemized;
                        SetPointScheduleSheetInstance(scheduleSheetInstances, XYZ.BasisX);
                        doc.Regenerate();
                        scheduleDefinition.IsItemized = !(valueItemized);
                        SetPointScheduleSheetInstance(scheduleSheetInstances, -XYZ.BasisX);
                        progressBar.valueChanged();
                    }
                    catch { continue; }
                }
                progressBar.Close();
                t.Commit();
            }

        }
        public string GetImageFolder()
        {
            string pathMyDocument = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(pathMyDocument, nameFolderImage);
        }
        public void createImageFolder()
        {
            bool isDirectory = Directory.Exists(GetImageFolder());
            if (!isDirectory) { Directory.CreateDirectory(GetImageFolder()); }
        }
        public void deleteImageFolder()
        {
            bool isDirectory = Directory.Exists(GetImageFolder());
            if (isDirectory) { Directory.Delete(GetImageFolder(), true); }
        }

        public Dictionary<string, ImageType> GetAllImagesInPorject(Autodesk.Revit.DB.Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Dictionary<string, ImageType> resultImageType = new Dictionary<string, ImageType>();
            foreach (Element imageType in collector.OfClass(typeof(ImageType)).ToElements())
            {
                string imageName = imageType.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString();
                resultImageType[imageName] = (ImageType)imageType;
            }
            return resultImageType;
        }

        public string SettingsLineValue(int numberLine)
        {
            string iniPath = File.ReadAllLines(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + localSettingsPath).ElementAt(numberLine);
            return iniPath;
        }
        public void replaceSettingsLineValue(string oldString, string newString)
        {
            string oldText = File.ReadAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + localSettingsPath);
            string newText = oldText.Replace(oldString, newString);
            File.WriteAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + localSettingsPath, newText);
        }

        public bool CheckElementsForParmaeter(IList<LegendViewItem> elements, string parameterName)
        {
            foreach (LegendViewItem element in elements)
            {
                Functions func = new Functions();
                Parameter parameter = func.GetImageParameter(element.GetTypeFromLegendComponent(), parameterName);
                if (parameter is null)
                {
                    return false;
                }
            }
            return true;
        }
        public bool CheckUniqueTypeElmenetsFromLegendComponent(
            Autodesk.Revit.DB.Document document, UIDocument uiDocument, IList<LegendViewItem> legendsViewItems)
        {
            IList<View> notUniqueTypeFromView = new List<View>();

            foreach (LegendViewItem legendViewItem in legendsViewItems)
            {
                View legendView = legendViewItem.legendView;

                ElementCategoryFilter categoryFilter = new ElementCategoryFilter(
                    BuiltInCategory.OST_LegendComponents);
                IList<ElementId> componentLegendsId = legendView.GetDependentElements(categoryFilter);

                HashSet<int> elementTypes = new HashSet<int>();

                foreach (ElementId componentLegendId in componentLegendsId)
                {
                    Element componentLegent = document.GetElement(componentLegendId);
                    int elementIdInt = componentLegent
                        .get_Parameter(BuiltInParameter.LEGEND_COMPONENT).AsElementId().IntegerValue;
                    elementTypes.Add(elementIdInt);
                }
                if (elementTypes.Count > 1)
                {
                    notUniqueTypeFromView.Add(legendView);
                }
            }
            if (notUniqueTypeFromView.Count > 0)
            {
                IList<string> namesView = new List<string>();
                foreach (View view in notUniqueTypeFromView)
                {
                    string nameView = view.get_Parameter(BuiltInParameter.VIEW_NAME).AsString();
                    namesView.Add(nameView);
                }
                TaskDialog.Show("Ошибка", "Необходимо исправить: Сейчас откроются те виды на которых размещено более " +
                    $"одного типоразмера в компонентах легенд:\n{string.Join("\n", namesView)}");
                foreach (View view in notUniqueTypeFromView)
                {
                    uiDocument.ActiveView = view;
                }
                return false;
            }
            return true;

        }
        public void SynchronizeRevitDocument(Autodesk.Revit.DB.Document doc)
        {
            RelinquishOptions relinquishOptions = new RelinquishOptions(true);
            relinquishOptions.UserWorksets = true;
            relinquishOptions.CheckedOutElements = true;
            relinquishOptions.StandardWorksets = true;

            TransactWithCentralOptions transactWithCentralOptions = new TransactWithCentralOptions();

            SynchronizeWithCentralOptions synchronizeWithCentralOptions = new SynchronizeWithCentralOptions();
            synchronizeWithCentralOptions.Comment = "Синхронизация после обновления легенд";
            synchronizeWithCentralOptions.Compact = true;
            synchronizeWithCentralOptions.SetRelinquishOptions(relinquishOptions);

            doc.SynchronizeWithCentral(transactWithCentralOptions, synchronizeWithCentralOptions);
        }
    }
}
