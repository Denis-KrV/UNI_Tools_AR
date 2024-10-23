using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UNI_Tools_AR.CreateFinish.FinishWall
{
    /// <summary>
    /// Interaction logic for CreateFinishWalls.xaml
    /// </summary>
    public partial class CreateFinishWalls : Window
    {
        private Autodesk.Revit.UI.UIApplication _uiApplication { get; }
        private Autodesk.Revit.ApplicationServices.Application _application { get; }
        private Autodesk.Revit.UI.UIDocument _uiDocument { get; }
        private Autodesk.Revit.DB.Document _document { get; }

        private Funcitons func = new Funcitons();

        private IList<Room> selectionRooms => func.GetRoomSelection(_uiDocument, _document);
        private IList<Room> allRoomsInProject => func.GetAllRoomsInProject(_document);
        private IList<Room> allRoomsInActiveView => func.GetAllRoomsInActiveView(_document);
        private IList<Room> allRoomsInLevel { get; set; }

        private IList<FinishWallType> allWallTypeInProject => (
            from wallType in func.GetAllWallType(_document)
            select new FinishWallType
            {
                nameType = wallType.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString(),
                wallType = wallType
            }
        ).ToList();

        private Dictionary<string, Level> levels => func.GetAllLevelsCollection(_document);

        private IList<FinishWallItem> AllBaseSurfacesInProject { get; set; }

        private BaseElement otherBaseElement = new BaseElement
        {
            categoryName = "Нет",
            nameType = "Остальные поверхности",
            elementType = null,
            category = null
        };

        public CreateFinishWalls(
            Autodesk.Revit.UI.UIApplication uiApplication,
            Autodesk.Revit.ApplicationServices.Application application,
            Autodesk.Revit.UI.UIDocument uiDocument,
            Autodesk.Revit.DB.Document document
        )
        {
            _uiApplication = uiApplication;
            _application = application;
            _uiDocument = uiDocument;
            _document = document;

            InitializeComponent();

            BindAllSurfaceInProject();

            BindRadioButtons();

            AllRooms_RB.IsChecked = true;
            WallDataGrid.Items.Refresh();
        }

        private void BindAllSurfaceInProject()
        {
            IList<Element> groundElements = func.GetAllElementsTypeFromGroundRoom(_document, allRoomsInProject);

            IList<FinishWallItem> finishWallItems = new List<FinishWallItem>();
            foreach (Element element in groundElements)
            {
                Parameter elementTypeName_Par = element.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME);
                string elementTypeName = elementTypeName_Par.AsString();

                Category categoryElement = element.Category;
                string categoryName = categoryElement.Name;

                BaseElement baseElement = new BaseElement
                {
                    categoryName = categoryName,
                    nameType = elementTypeName,
                    elementType = element,
                    category = categoryElement,
                };

                FinishWallItem finishWallItem = new FinishWallItem
                {
                    baseElement = baseElement,
                    finishWall = null,
                    hasGround = true,
                };
                finishWallItems.Add(finishWallItem);
            }

            FinishWallItem baseOtherSurface = new FinishWallItem
            {
                baseElement = otherBaseElement,
                finishWall = null,
                hasGround = false,
            };

            finishWallItems.Add(baseOtherSurface);

            //FinishWallType.ItemsSource = allWallTypeInProject;

            AllBaseSurfacesInProject = finishWallItems;

            WallDataGrid.ItemsSource = finishWallItems;

            WallDataGrid.Items.Refresh();
        }

        private void BindRadioButtons()
        {
            if (selectionRooms.Count == 0)
            {
                SelectRooms_RB.IsEnabled = false;
            }

            IList<string> levelNames = new List<string>();
            foreach (string levelName in levels.Keys)
            {
                levelNames.Add(levelName);
            }
            allLevels.ItemsSource = levelNames;
            allLevels.SelectedIndex = 0;

            AutoCreate_RB.IsChecked = true;

            AllRooms_RB.IsChecked = true;
        }

        private void RoomInLevel_RB_Checked(object sender, RoutedEventArgs e)
        {
            allLevels.IsEnabled = true;

            Level level = levels[(string)allLevels.SelectedValue];

            allRoomsInLevel = func.GetAllRoomsInLevel(_document, level);
            UpdateGrid(allRoomsInLevel);
        }

        private void AllRooms_RB_Checked(object sender, RoutedEventArgs e)
        {
            allLevels.IsEnabled = false;
            UpdateGrid(allRoomsInProject);
        }

        private void allLevels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Level level = levels[(string)allLevels.SelectedValue];

            allRoomsInLevel = func.GetAllRoomsInLevel(_document, level);
            UpdateGrid(allRoomsInLevel);
        }

        private void RoomInActiveView_RB_Checked(object sender, RoutedEventArgs e)
        {
            allLevels.IsEnabled = false;

            UpdateGrid(allRoomsInActiveView);
        }

        private void SelectRooms_RB_Checked(object sender, RoutedEventArgs e)
        {
            allLevels.IsEnabled = false;

            IList<Room> rooms = func.GetRoomSelection(_uiDocument, _document);
            UpdateGrid(rooms);
        }

        private void HeigthWallForRoom_Checked(object sender, RoutedEventArgs e)
        {
            HeigthhWall.IsEnabled = false;
            VarCreationRoom.Visibility = System.Windows.Visibility.Visible;
        }

        private void AutoCreate_Checked(object sender, RoutedEventArgs e)
        {
            HeigthhWall.IsEnabled = false;
            VarCreationRoom.Visibility = System.Windows.Visibility.Hidden;
        }

        private void HeigthWall_Checked(object sender, RoutedEventArgs e)
        {
            HeigthhWall.IsEnabled = true;
            VarCreationRoom.Visibility = System.Windows.Visibility.Visible;
            HeigthhWall.Focus();
        }

        private void MakeWall_Bt_Click(object sender, RoutedEventArgs e)
        {
            string valueHeigthWall = HeigthhWall.Text;
            string valueOffsetWall = OffsetWall.Text;
            double heigthWall;
            double offsetWall;

            bool resultConvertHeigth = double.TryParse(valueHeigthWall, out heigthWall);
            bool resultConvertOffset = double.TryParse(valueOffsetWall, out offsetWall);

            if (!resultConvertHeigth)
            {
                MessageBox.Show(
                    $"Данные в поле \"{HeigthWall_RB.Content}\" не являются числом.",
                    "Предупреждение"
                );
            }
            else if (!resultConvertOffset)
            {
                MessageBox.Show(
                    $"Данные в поле \"{OffsetWall_TB.Text}\" не являются числом.",
                    "Предупреждение"
                );
            }
            else
            {
                IList<Room> rooms;

                if (AllRooms_RB.IsChecked is true)
                {
                    rooms = allRoomsInProject;
                }
                else if (RoomInActiveView_RB.IsChecked is true)
                {
                    rooms = allRoomsInActiveView;
                }
                else if (SelectRooms_RB.IsChecked is true)
                {
                    rooms = selectionRooms;
                }
                else
                {
                    rooms = allRoomsInLevel;
                }

                IList<ElementId> roomsId = rooms.Where(room => room.Location != null).Select(room => room.Id).ToList();
                IList<ElementId> typeIds = allWallTypeInProject.Select(wallType => wallType.wallType.Id).ToList();

                if (func.IsMeCheckoutElement(_document, _application, roomsId) &&
                    func.IsMeCheckoutElement(_document, _application, typeIds))
                {
                    IList<FinishWallItem> wallItems = (WallDataGrid.ItemsSource as IList<FinishWallItem>)
                        .Where(wallItem => !(wallItem.finishWall is null))
                        .Select(wallItem => wallItem)
                        .ToList();

                    BuilderWalls createWalls = new BuilderWalls(_document, rooms, wallItems);

                    if (HeigthWallForRoom_RB.IsChecked is true)
                    {
                        createWalls.MakeWallForRoomHeigth(offsetWall);
                    }
                    else if (HeigthWall_RB.IsChecked is true)
                    {
                        createWalls.MakeWallForHeigth(offsetWall, heigthWall);
                    }
                    else
                    {
                        createWalls.MakeWallAutomate(offsetWall);
                    }
                }
                Close();
            }
        }

        private void UpdateGrid(IList<Room> rooms)
        {
            IList<Element> groundElements =
                func.GetAllElementsTypeFromGroundRoom(_document, rooms);

            IList<int> groundElementsId = groundElements
                .Select(groundElement => groundElement.Id.IntegerValue)
                .ToList(); 

            IList<FinishWallItem> finishWallItems = new List<FinishWallItem>();

            foreach (FinishWallItem finishWallItem in AllBaseSurfacesInProject)
            {
                if (finishWallItem.baseElement.elementType is null)
                {
                    finishWallItems.Add(finishWallItem);
                    continue;
                }

                int groundElementId = finishWallItem.baseElement.elementType.Id.IntegerValue;
                if (groundElementsId.Contains(groundElementId))
                {
                    finishWallItems.Add(finishWallItem);
                }
            }

            if (rooms.Count != 0)
            {
                WallDataGrid.ItemsSource = finishWallItems;
            }
            else
            {
                WallDataGrid.ItemsSource = null;
            }
            WallDataGrid.Items.Refresh();
        }

        private void FilterFinishWall_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filterDepthFinishWall = FilterFinishWall.Text;
            double widthWall;

            bool resultConvertDepthFinishWall = double.TryParse(filterDepthFinishWall, out widthWall);
            if (!resultConvertDepthFinishWall)
            {
                string messageDialog = $"Данные в поле \"{FilterFinshWall_TB.Text}\" не являются числом.";
                System.Windows.MessageBox.Show(messageDialog, "Предупреждение");

                widthWall = 100;
                FilterFinishWall.Text = widthWall.ToString();
            }

            IList<FinishWallType> finishWallTypes = new List<FinishWallType>();

            foreach (FinishWallType finishWallType in allWallTypeInProject)
            {
                Parameter widthWall_Par = finishWallType.wallType.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);

                if (widthWall_Par is null)
                {
                    finishWallTypes.Add(finishWallType);
                    continue;
                }

                double convertFilterWidthWall = func.UnitConverter(widthWall, true, widthWall_Par.GetUnitTypeId());

                if (widthWall_Par.AsDouble() <= convertFilterWidthWall)
                {
                    finishWallTypes.Add(finishWallType);
                }
            }
            FinishWallType.ItemsSource = finishWallTypes;
        }
    }

    class FinishWallItem
    {
        public BaseElement baseElement { get; set; }
        public FinishWallType finishWall { get; set; }
        public bool hasGround { get; set; }
    }

    class BaseElement
    {
        public string categoryName { get; set; }
        public string nameType { get; set; }
        public Element elementType { get; set; }
        public Category category { get; set; }
    }

    class FinishWallType
    {
        public string nameType { get; set; }
        public WallType wallType { get; set; }
        public override string ToString() => nameType;
    }
}
