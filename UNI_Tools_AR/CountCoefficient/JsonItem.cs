using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace UNI_Tools_AR.CountCoefficient
{
    internal class JsonItem
    {
        static string _documentTitle;
        //static string _jsonCoeficientItemFile;

        public JsonItem(string documentTitle)
        {
            _documentTitle = documentTitle;
            //_jsonCoeficientItemFile = jsonCoeficientItemFile;
        }

        public string JsonFileProjectPaths()
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resourcePath = Path.Combine(baseDir, "Resources");
            string nameJson = "project_path.json";
            return GetOrCreateFile(Path.Combine(resourcePath, nameJson));
        }

        public IList<ProjectPaths> GetJsonProjectData()
        {
            string jsonPath = JsonFileProjectPaths();
            string textFromJsonFile = File.ReadAllText(jsonPath);
            IList<ProjectPaths> projectPaths =
                JsonConvert.DeserializeObject<IList<ProjectPaths>>(textFromJsonFile);
            if (projectPaths is null) { return new List<ProjectPaths>(); }
            return projectPaths;
        }

        public void SaveJsonProjectData(ProjectPaths newItemProjectPaths)
        {
            IList<ProjectPaths> projectPaths = GetJsonProjectData();
            IList<ProjectPaths> newProjectPaths = new List<ProjectPaths>();

            foreach (ProjectPaths projectPath in projectPaths)
            {
                if (!(projectPath.DocumentTitle == newItemProjectPaths.DocumentTitle))
                {
                    newProjectPaths.Add(projectPath);
                }
            }
            newProjectPaths.Add(newItemProjectPaths);

            string jsonData = JsonConvert.SerializeObject(newProjectPaths);
            File.WriteAllText(JsonFileProjectPaths(), jsonData);
        }

        public string GetCoefficientFile()
        {
            IList<ProjectPaths> projectPaths = GetJsonProjectData();
            foreach (ProjectPaths projectPath in projectPaths)
            {
                if (projectPath.DocumentTitle == _documentTitle)
                {
                    return GetOrCreateFile(projectPath.PathJsonCoeff);
                }
            }
            return null;
        }

        public string GetOrCreateFile(string pathFile)
        {
            bool isExixs = File.Exists(pathFile);
            if (isExixs)
            {
                return pathFile;
            }
            FileStream newFile = File.Create(pathFile);
            newFile.Close();
            return pathFile;
        }

        public string ChangePathJsonCoefficientFile()
        {
            string nameJsonFilePath = $"{_documentTitle}.json";

            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog();

            openFolderDialog.Description =
                "Выберите папку с json файлом коэффициентов, или папку где будет создан файл с " +
                $"коэффициентами, для проекта {_documentTitle}";

            openFolderDialog.ShowDialog();

            string folderPath = openFolderDialog.SelectedPath;
            if (!(folderPath == ""))
            {
                string pathJsonCoeff = GetOrCreateFile(Path.Combine(folderPath, nameJsonFilePath));
                ProjectPaths newProjectPaths = new ProjectPaths
                {
                    DocumentTitle = _documentTitle,
                    PathJsonCoeff = pathJsonCoeff
                };
                SaveJsonProjectData(newProjectPaths);
                return pathJsonCoeff;
            }
            string resultPath = GetCoefficientFile();
            if (resultPath is null) { return null; }
            return resultPath;
        }

        public IList<CountItemTable> GetJsonCountItemData()
        {
            string textFromJsonFile = File.ReadAllText(GetCoefficientFile());
            IList<CountItemTable> projectPaths =
                JsonConvert.DeserializeObject<IList<CountItemTable>>(textFromJsonFile);
            if (projectPaths is null) { return new List<CountItemTable>(); }
            return projectPaths;
        }

        public void SaveItemsTableInJson(IList<CountItemTable> countItemTables)
        {
            string jsonData = JsonConvert.SerializeObject(countItemTables); ;
            File.WriteAllText(GetCoefficientFile(), jsonData);
        }
    }

    class ProjectPaths
    {
        public string DocumentTitle { get; set; }
        public string PathJsonCoeff { get; set; }
    }
}
