using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UNI_Tools_AR.UpdateLegends
{
    internal class UpdaterImage
    {
        static Document _doc;
        static IList<LegendViewItem> _legends;
        static Functions _func;

        public UpdaterImage(Document doc, IList<LegendViewItem> legendViewItems)
        {
            _doc = doc;
            _legends = legendViewItems;
            _func = new Functions();
        }

        public Result updateImages(string parameterName)
        {
            const string turnOffFamParName = "CreateImage";
            _func.createImageFolder();

            using (Transaction t = new Transaction(_doc, "Загрузка семейства рамки"))
            {
                t.Start();
                try
                {
                    ProgressBar progressBar = new ProgressBar(_legends.Count());
                    progressBar.Show();
                    foreach (LegendViewItem legendViewItem in _legends)
                    {
                        Autodesk.Revit.DB.View legendView = legendViewItem.legendView;

                        Parameter paraemterViewName = legendView.get_Parameter(BuiltInParameter.VIEW_NAME);

                        progressBar.valueChanged();

                        legendViewItem.legendInstance.LookupParameter(turnOffFamParName).Set(0);
                        _doc.Regenerate();

                        string pathImage = createImage(legendViewItem.legendView.Id);
                        ImageType elementImageType = loadImage(pathImage);

                        Parameter parameter = _func.getImageParameter(legendViewItem.getTypeFromLegendComponent(), parameterName);
                        parameter.Set(elementImageType.Id);

                        legendViewItem.legendInstance.LookupParameter(turnOffFamParName).Set(1);
                        _doc.Regenerate();
                    }
                    t.Commit();
                    TaskDialog.Show("Информация", $"Обновлено {_legends.Count} картинок из легенд.");
                    _func.deleteImageFolder();
                    progressBar.Close();
                    return Result.Succeeded;
                }
                catch
                {
                    t.RollBack();
                    TaskDialog.Show("Ошибка", "Обновление легенд не произошло");
                    _func.deleteImageFolder();
                    return Result.Failed;
                }
            }
        }
        public string createImage(ElementId viewId)
        {
            IList<ElementId> listViewId = new List<ElementId>();
            listViewId.Add(viewId);

            string prefixImage = "CreateImage";
            string imagePath = Path.Combine(_func.getImageFolder(), prefixImage);

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

        public ImageType loadImage(string pathImageFile)
        {
            ImageTypeOptions imageTypeOptions = new ImageTypeOptions(pathImageFile, false, ImageTypeSource.Import);
            string nameFileImage = Path.GetFileName(pathImageFile);
            Dictionary<string, ImageType> imageTypes = _func.getAllImagesInPorject(_doc);

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
