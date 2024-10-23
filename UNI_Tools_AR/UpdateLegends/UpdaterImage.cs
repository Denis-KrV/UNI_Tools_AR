using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UNI_Tools_AR.UpdateLegends
{
    internal class UpdaterImage
    {
        private Document _doc { get; }
        private IList<LegendViewItem> _legends { get; }
        private Functions _func { get; } = new Functions();

        public UpdaterImage(Document doc, IList<LegendViewItem> legendViewItems)
        {
            _doc = doc;
            _legends = legendViewItems;
        }

        public Result UpdateImages(string parameterName)
        {
            const string turnOffFamParName = "CreateImage";
            _func.createImageFolder();
            ProgressBar progressBar = new ProgressBar(_legends.Count(), "Обработка легенд");
            progressBar.Show();

            using (Transaction t = new Transaction(_doc, "Обновление легенд"))
            {
                t.Start();
                try
                {
                    foreach (LegendViewItem legendViewItem in _legends)
                    {
                        Autodesk.Revit.DB.View legendView = legendViewItem.legendView;

                        Parameter paraemterViewName = legendView.get_Parameter(BuiltInParameter.VIEW_NAME);

                        progressBar.valueChanged();

                        legendViewItem.legendInstance.LookupParameter(turnOffFamParName).Set(0);
                        _doc.Regenerate();

                        string pathImage = CreateImage(legendViewItem.legendView.Id);
                        ImageType elementImageType = LoadImage(pathImage);

                        Parameter parameter = _func.GetImageParameter(legendViewItem.GetTypeFromLegendComponent(), parameterName);
                        parameter.Set(elementImageType.Id);

                        legendViewItem.legendInstance.LookupParameter(turnOffFamParName).Set(1);
                        _doc.Regenerate();
                    }
                    t.Commit();
                    TaskDialog.Show("Информация", $"Обновлено {_legends.Count} картинок из легенд.\n{progressBar.timerLabel.Content}");
                    _func.deleteImageFolder();
                    progressBar.Close();
                    return Result.Succeeded;
                }
                catch
                {
                    t.RollBack();
                    TaskDialog.Show("Ошибка", "Обновление легенд не произошло");
                    _func.deleteImageFolder();
                    progressBar.Close();
                    return Result.Failed;
                }
            }
        }
        public string CreateImage(ElementId viewId)
        {
            IList<ElementId> listViewId = new List<ElementId>() { viewId };

            string prefixImage = "CreateImage";
            string imagePath = Path.Combine(_func.GetImageFolder(), prefixImage);

            ImageExportOptions exportOptions = new ImageExportOptions();

            exportOptions.ZoomType = ZoomFitType.FitToPage;
            exportOptions.PixelSize = 2024;
            exportOptions.HLRandWFViewsFileType = ImageFileType.PNG;
            exportOptions.ImageResolution = ImageResolution.DPI_600;
            exportOptions.ExportRange = ExportRange.SetOfViews;
            exportOptions.FilePath = imagePath;
            exportOptions.SetViewsAndSheets(listViewId);
            _doc.ExportImage(exportOptions);

            string fileImageName = ImageExportOptions.GetFileName(_doc, viewId);
            string resultImagePath = $"{imagePath}{fileImageName}.png";

            return resultImagePath;
        }

        public ImageType LoadImage(string pathImageFile)
        {
            ImageTypeOptions imageTypeOptions = new ImageTypeOptions(pathImageFile, false, ImageTypeSource.Import);
            string nameFileImage = Path.GetFileName(pathImageFile);
            Dictionary<string, ImageType> imageTypes = _func.GetAllImagesInPorject(_doc);

            if (imageTypes.ContainsKey(nameFileImage))
            {
                ImageType imageType = imageTypes[nameFileImage];
                imageType.ReloadFrom(imageTypeOptions);
                return imageType;
            }
            else
            {
                ImageType imageType = ImageType.Create(_doc, imageTypeOptions);
                return imageType;
            }
        }
    }
}
