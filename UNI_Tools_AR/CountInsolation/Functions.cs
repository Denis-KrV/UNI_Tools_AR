using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNI_Tools_AR.CountInsolation
{
    internal class Functions
    {
        private UIApplication _uiApplication;
        private Application _application;
        private UIDocument _uiDocument;
        private Document _document;

        private Options _options => new Options 
        { 
            IncludeNonVisibleObjects = true, 
            DetailLevel = ViewDetailLevel.Fine 
        };

        public Functions(
            UIApplication uiApplication,
            Application application,
            UIDocument uiDocument,
            Document documen
        )
        {
            _uiApplication = uiApplication;
            _application = application;
            _uiDocument = uiDocument;
            _document = documen;
        }

        public DirectShape CreateDirectShape(Document document, IList<GeometryObject> gElements)
        {
            DirectShape directShape = DirectShape.CreateElement(document, new ElementId(-2000011));
            if (directShape.IsValidShape(gElements))
            {
                directShape.SetShape(gElements);
            }
            return directShape;
        }

        public ElementId GetElementIdForNameParameter(string parameterName)
        {
            foreach (KeyValuePair<Definition, Binding> keyValuePair in _document.ParameterBindings)
            {
                InternalDefinition definition = keyValuePair.Key as InternalDefinition;
                if (definition.Name == parameterName)
                {
                    return definition.Id;
                }
            }
            return null;
        }

        public bool CheckParameterPorjectInDocument(string nameParameter)
        {
            Element window = new FilteredElementCollector(_document)
                .OfCategoryId(new ElementId(Constants.windowCategoryIntId))
                .WhereElementIsNotElementType()
                .First();

            DefinitionBindingMapIterator definitionBindingMapIterator = 
                _document.ParameterBindings.ForwardIterator();

            string exceptionMessage;
            while (definitionBindingMapIterator.MoveNext())
            {
                Binding binding = definitionBindingMapIterator.Current as Binding;
                InternalDefinition definition = definitionBindingMapIterator.Key as InternalDefinition;
                if (definition.Name == nameParameter)
                {
                    if (!(binding is InstanceBinding))
                    {
                        exceptionMessage =
                            $"Параметр {nameParameter} должен быть " +
                            $"параметром экземпляра.";
                        TaskDialog.Show(Constants.exceptionTitle, exceptionMessage);
                        return false;
                    }
                    else if (!definition.VariesAcrossGroups)
                    {
                        exceptionMessage =
                            $"Параметр {nameParameter} не должен " +
                            $"выравнивать значения относительно групп.";
                        TaskDialog.Show(Constants.exceptionTitle, exceptionMessage);
                        return false;
                    }
                    else if (window.LookupParameter(nameParameter) is null)
                    {
                        exceptionMessage =
                            $"Параметр {nameParameter} должен быть " +
                            $"назначен категории окно.";
                        TaskDialog.Show(Constants.exceptionTitle, exceptionMessage);
                        return false;
                    }
                    return true;
                }
            }
            exceptionMessage = 
                $"Параметр {nameParameter} не найден в проекте, необходимо " +
                $"его создать он должен быть параметром экземпляра, и не " +
                $"должен выравниваться относительно групп.";
            TaskDialog.Show(Constants.exceptionTitle, exceptionMessage);
            return false;
        }

        public SunAndShadowSettings GetSunAndShadowSettings()
        {
            Element sunAndShadowElement = 
                new FilteredElementCollector(_document, _document.ActiveView.Id)
                    .OfClass(typeof(SunAndShadowSettings))
                    .FirstElement();

            if (sunAndShadowElement is SunAndShadowSettings) 
            {
                return sunAndShadowElement as SunAndShadowSettings;
            }
            return null;
        }
        public IList<Element> GetAllWindows()
        {
            IList<Element> windows = 
                new FilteredElementCollector(_document, _document.ActiveView.Id)
                    .OfCategoryId(new ElementId(Constants.windowCategoryIntId))
                    .WhereElementIsNotElementType()
                    .ToElements();

            return windows;
        }
        
        public IList<Element> GetAllInsolationPoints()
        {
            FilterStringRule stringRule = new FilterStringRule(
                new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_FAMILY_PARAM)),
                new FilterStringEquals(),
                Constants.insolationPointFamilyName);

            ElementParameterFilter parameterFilter = new ElementParameterFilter(stringRule);

            IList<Element> insolationPoints = 
                new FilteredElementCollector(_document, _document.ActiveView.Id)
                    .OfClass(typeof(FamilyInstance))
                    .WherePasses(parameterFilter)
                    .ToElements();

            if (insolationPoints.Count == 0) return null; 
            return insolationPoints;
        }

        public XYZ GetCenterPointElement(Element element)
        {
            BoundingBoxXYZ boundingBoxXYZ = element.get_Geometry(_options).GetBoundingBox();
            return (boundingBoxXYZ.Max + boundingBoxXYZ.Min) / 2; 
        }

        public double ConvertRadToDegree(double radAngle)
        {
            return 180 * radAngle / Math.PI;
        }

        public double GetSumAngleInPoints(IList<XYZ> points)
        {
            XYZ fstPoint = null;

            double angle = 0;

            foreach (XYZ point in points)
            {
                if (fstPoint is null) 
                { 
                    fstPoint = point; 
                    continue; 
                }

                double radAngle = fstPoint.AngleTo(point);
                angle += ConvertRadToDegree(radAngle);

                fstPoint = point;
            }
            return angle;
        }

        public ParameterFilterElement getOrCreateParameterFilterElements(
            string nameParameter, double ruleValue)
        {
            ICollection<ElementId> elementIds =
                new List<ElementId> { new ElementId(Constants.windowCategoryIntId) };

            FilteredElementCollector collector = new FilteredElementCollector(_document);
            IList<ParameterFilterElement> parameters = collector
                .Select(elementFilter => elementFilter as ParameterFilterElement)
                .Where(elementFilter => elementFilter.Name == nameParameter)
                .ToList();

            if (parameters.Count > 1) { return parameters.First(); }

            double epsilon = 0;

            ElementId parameterId = GetElementIdForNameParameter(nameParameter);
            ParameterValueProvider parameterValueProvider = new ParameterValueProvider(parameterId);
            FilterDoubleRule logicElementFilter = new FilterDoubleRule(
                parameterValueProvider,
                new FilterNumericEquals(),
                ruleValue,
                epsilon
            );
            ElementFilter elementParameterFilter = new ElementParameterFilter(logicElementFilter);
            IList<ElementFilter> filters = new List<ElementFilter> { elementParameterFilter };

            ElementFilter logicalAndFilter = new LogicalAndFilter(filters);

            ParameterFilterElement parameterFilterElement = ParameterFilterElement
                .Create(_document, nameParameter, elementIds, logicalAndFilter);

            return parameterFilterElement;
        }

        public View3D GetORCreateColor3DViewForCountInsolation(string nameParameter)
        {

            FilteredElementCollector collector = new FilteredElementCollector(_document);

            ViewFamilyType viewFamilyType = collector
                .OfClass(typeof(ViewFamilyType))
                .Select(viewType => viewType as ViewFamilyType)
                .Where(viewType => viewType.ViewFamily == ViewFamily.ThreeDimensional)
                .First();

            IList<View3D> view3Ds = collector
                .OfClass(typeof(View3D))
                .Where(view => view.get_Parameter(BuiltInParameter.VIEW_NAME).AsString() ==
                                Constants.exceptionActiveViewNotThreeD)
                .Select(view => view as View3D)
                .ToList();

            if (view3Ds.Count > 0) { return view3Ds.First(); } 

            View3D view3D = View3D.CreateIsometric(_document, viewFamilyType.Id);
            ParameterFilterElement parameterFilterElementRedColor =
                getOrCreateParameterFilterElements(nameParameter, 1);
            ParameterFilterElement parameterFilterElementOrangeColor =
                getOrCreateParameterFilterElements(nameParameter, 2);
            ParameterFilterElement parameterFilterElementGreenColor = 
                getOrCreateParameterFilterElements(nameParameter, 3);

            return view3D;
        }
    }
}
