using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UNI_Tools_AR.UpdateLegends
{
    internal class Functions
    {
        string nameFolderImage = "images";
        string localSettingsPath = "\\Resources\\Settings.ini";

        public Parameter getImageParameter(Element elementType, string parameterName)
        {
            string exceptTitle = "Сообщение об ошибке.";

            var parameter = elementType.LookupParameter(parameterName);
            if (! (parameter is Parameter))
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

        public string getImageFolder()
        {
            string pathMyDocument = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(pathMyDocument, nameFolderImage);
        }
        public void createImageFolder()
        {
            bool isDirectory = Directory.Exists(getImageFolder());
            if (!isDirectory) { Directory.CreateDirectory(getImageFolder()); }
        }
        public void deleteImageFolder()
        {
            bool isDirectory = Directory.Exists(getImageFolder());
            if (isDirectory) { Directory.Delete(getImageFolder(), true); }
        }

        public Dictionary<string, ImageType> getAllImagesInPorject(Autodesk.Revit.DB.Document doc)
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

        public string settingsLineValue(int numberLine)
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

        public bool checkElementsForParmaeter(IList<LegendViewItem> elements, string parameterName)
        {
            foreach (LegendViewItem element in elements)
            {
                Functions func = new Functions();
                Parameter parameter = func.getImageParameter(element.getTypeFromLegendComponent(), parameterName);
                if (parameter is null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
