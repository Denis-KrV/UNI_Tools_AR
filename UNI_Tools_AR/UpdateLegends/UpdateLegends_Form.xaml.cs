using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Windows;

namespace UNI_Tools_AR.UpdateLegends
{
    public partial class UpdateLegends_Form : Window
    {
        private Functions _func;
        private Document _doc;
        private Autodesk.Revit.ApplicationServices.Application _app;
        public UpdateLegends_Form(Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            _func = new Functions();
            _doc = doc;
            _app = app;

            InitializeComponent();
            tbParameterName.Text = _func.SettingsLineValue(0);
        }

        private void runButton(object sender, RoutedEventArgs e)
        {
            string oldParameterName = _func.SettingsLineValue(0);
            _func.replaceSettingsLineValue(oldParameterName, tbParameterName.Text);

            Legends legends = new Legends(_doc);
            IList<LegendViewItem> legendsViewItems = legends.CreateLegendViewItems();
            IList<Element> filteredElementsType = new List<Element>();
            foreach (LegendViewItem legendViewItem in legendsViewItems)
            {
                Element elementType = legendViewItem.GetTypeFromLegendComponent();
                filteredElementsType.Add(elementType);
            }


            IList<ElementId> elementIds = new List<ElementId>();
            if ((bool)updateShedules.IsChecked)
            {
                foreach (Element element in _func.GetAllViewSchedules(_doc))
                {
                    elementIds.Add(element.Id);
                }
            }
            foreach (LegendViewItem legendItem in legendsViewItems)
            {
                elementIds.Add(legendItem.legendInstance.Id);
                elementIds.Add(legendItem.legendView.Id);
                elementIds.Add(legendItem.legendComponent.Id);
                elementIds.Add(legendItem.GetTypeFromLegendComponent().Id);
            }
            bool resultCheckoutElt = _func.IsMeCheckoutElement(_doc, _app, elementIds);
            if (resultCheckoutElt)
            {
                if (!(_func.CheckElementsForParmaeter(legendsViewItems, tbParameterName.Text)))
                {
                    Close();
                }
                if ((bool)deleteImageParameter.IsChecked)
                {
                    _func.ClearImageParameter(_doc, filteredElementsType, _func.SettingsLineValue(0));
                }
                if ((bool)isRenameView.IsChecked)
                {
                    int countRenameView = 0;

                    foreach (LegendViewItem legendViewItem in legendsViewItems)
                    {
                        View legend = legendViewItem.legendView;
                        Element element = legendViewItem.GetTypeFromLegendComponent();
                        Parameter legendNameParameter = legend.get_Parameter(BuiltInParameter.VIEW_NAME);
                        Parameter typeNameParameter = element.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME);
                        string legendName = legendNameParameter.AsString();
                        string typeName = typeNameParameter.AsString();

                        using (Transaction t = new Transaction(_doc, $"Переименование легенды id {legend.Id}"))
                        {
                            t.Start();
                            if (legendName != typeName) { legendNameParameter.Set(typeName); countRenameView++; }
                            t.Commit();
                        }
                    }
                    if (countRenameView != 0)
                    {
                        TaskDialog.Show("Информация", $"Переименовано {countRenameView} легенд");
                    }
                }

                UpdaterImage updaterImage = new UpdaterImage(_doc, legendsViewItems);
                updaterImage.UpdateImages(tbParameterName.Text);

                if ((bool)updateShedules.IsChecked)
                {
                    _func.UpdateAllSchedules(_doc);
                }
                if ((bool)synchronizeDocument.IsChecked & !(_doc.IsWorkshared))
                {
                    _func.SynchronizeRevitDocument(_doc);
                }
            }

            Close();
        }

        private void cancelButton(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
