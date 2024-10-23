using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace UNI_Tools_AR.UpdateLegends
{
    internal class Legends
    {
        const string familyNameLegend = "DYNAMO_CreateImage_R21";
        const string familyFileLegend = "DYNAMO_CreateImage_R21.rfa";
        private ElementId componentCetegoryId { get; } = new ElementId(-2000575);

        private Document _doc { get; }
        private Functions _func { get; } = new Functions();

        public Legends(Document doc)
        {
            _doc = doc;
        }
        public IList<View> AllLegendsView()
        {
            ElementClassFilter filterLegendsView = new ElementClassFilter(typeof(View));
            FilteredElementCollector collector = new FilteredElementCollector(_doc);
            IList<Element> viewsType = collector.OfClass(typeof(ViewFamilyType)).ToElements();

            ViewFamilyType legendViewType = null;
            foreach (ViewFamilyType viewType in viewsType)
            {
                if (viewType.ViewFamily == ViewFamily.Legend)
                {
                    legendViewType = viewType;
                    break;
                }
            }
            IList<ElementId> legendViewsId = legendViewType.GetDependentElements(filterLegendsView);

            IList<View> legendViews = new List<View>();
            foreach (ElementId viewId in legendViewsId)
            {
                Element view = _doc.GetElement(viewId);
                if (view is View)
                {
                    legendViews.Add((View)view);
                }
            }
            if (legendViews.Count == 0)
            {
                return null;
            }
            return legendViews;
        }
        public Family GetFamilyCreateImage()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(Family));
            foreach (Element family in collector)
            {
                if (family.Name == familyNameLegend)
                {
                    return (Family)family;
                }
            }
            return null;
        }

        public void LoadFamilyCreateImage()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"UNI_Tools_AR.Resources.{familyFileLegend}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                string tempFilePath = Path.Combine(Path.GetTempPath(), familyFileLegend);

                using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
                using (Transaction t = new Transaction(_doc, "Загрузка семейства рамки"))
                {
                    t.Start();
                    _doc.LoadFamily(tempFilePath);
                    TaskDialog.Show("Информация",
                        $"Семейство {familyFileLegend} было подгружено в проект.");
                    t.Commit();
                }
                File.Delete(tempFilePath);
            }
        }

        public IList<FamilyInstance> AllLegendsInstance()
        {
            ElementClassFilter filterFamilyInstance = new ElementClassFilter(typeof(FamilyInstance));
            Family legendFamily = GetFamilyCreateImage();

            IList<ElementId> instanceLegendIds = legendFamily.GetDependentElements(filterFamilyInstance);

            IList<FamilyInstance> instanceLegend = new List<FamilyInstance>();
            foreach (ElementId elementId in instanceLegendIds)
            {
                FamilyInstance famInstance = (FamilyInstance)_doc.GetElement(elementId);
                instanceLegend.Add(famInstance);
            }
            if (instanceLegendIds.Count > 0)
            {
                return instanceLegend;
            }
            return null;
        }

        public IList<Element> AllLegendsComponent()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc);
            IList<Element> legendComponents = collector.OfCategoryId(componentCetegoryId).ToElements();
            if (legendComponents.Count > 0)
            {
                return legendComponents;
            }
            return null;
        }
        public IList<LegendViewItem> CreateLegendViewItems()
        {
            IList<View> legendsView = AllLegendsView();
            IList<FamilyInstance> legendsInstance = AllLegendsInstance();
            IList<Element> legendsComponent = AllLegendsComponent();

            Dictionary<ElementId, View> viewItems = new Dictionary<ElementId, View>();
            foreach (View legend in legendsView)
            {
                viewItems[legend.Id] = legend;
            }

            Dictionary<ElementId, FamilyInstance> instanceItems = new Dictionary<ElementId, FamilyInstance>();
            foreach (FamilyInstance legendInstance in legendsInstance)
            {
                ElementId viewId = legendInstance.OwnerViewId;
                if (!instanceItems.ContainsKey(viewId))
                {
                    instanceItems[viewId] = legendInstance;
                }
                else { continue; }
            }

            Dictionary<ElementId, Element> componentItems = new Dictionary<ElementId, Element>();
            foreach (Element component in legendsComponent)
            {
                ElementId viewId = component.OwnerViewId;
                if (!componentItems.ContainsKey(viewId))
                {
                    componentItems[viewId] = component;
                }
                else { continue; }
            }
            List<LegendViewItem> legendViewItems = new List<LegendViewItem>();
            foreach (var item in viewItems)
            {
                if (instanceItems.ContainsKey(item.Key) & componentItems.ContainsKey(item.Key))
                {
                    LegendViewItem viewsItem = new LegendViewItem(
                        _doc, item.Value, instanceItems[item.Key], componentItems[item.Key]);
                    legendViewItems.Add(viewsItem);
                }
            }
            return legendViewItems;
        }
    }

    internal class LegendViewItem
    {
        static Document _doc;
        public View legendView;
        public FamilyInstance legendInstance;
        public Element legendComponent;

        public LegendViewItem(Document doc, View legendView, FamilyInstance legendInstance, Element legendComponent)
        {
            _doc = doc;
            this.legendView = legendView;
            this.legendInstance = legendInstance;
            this.legendComponent = legendComponent;
        }
        public Element GetTypeFromLegendComponent()
        {
            Parameter parameterElTypeFromLegendComponent = legendComponent
                .get_Parameter(BuiltInParameter.LEGEND_COMPONENT);
            return _doc.GetElement(parameterElTypeFromLegendComponent.AsElementId());
        }
    }
}
