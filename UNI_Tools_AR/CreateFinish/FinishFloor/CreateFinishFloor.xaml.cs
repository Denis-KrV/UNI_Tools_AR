using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UNI_Tools_AR.CreateFinish.FinishWall;

namespace UNI_Tools_AR.CreateFinish.FinishFloor
{
    /// <summary>
    /// Interaction logic for CreateFinishFloor.xaml
    /// </summary>
    public partial class CreateFinishFloor : Window
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

        private Dictionary<string, Level> levels => func.GetAllLevelsCollection(_document);

        private IList<FinishFloorType> allFloorTypeInProject =>
            func.GetAllFloorType(_document)
            .Select(
                floorType => new FinishFloorType
                {
                    nameType = floorType.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString(),
                    floorType = floorType,
                }
            )
            .OrderBy(floorType => floorType.nameType)
            .ToList();

        public CreateFinishFloor(
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

            BindRadioButtons();
        }

        private void BindRadioButtons()
        {
            IList<string> allParametersNameInRoom =
                func.AllParametersInElement(allRoomsInProject.First())
                .Select(parameter => parameter.Definition.Name)
                .OrderBy(parameter => parameter)
                .ToList();

            SelectParameter_CB.ItemsSource = allParametersNameInRoom;
            SelectParameter_CB.SelectedValue = "Номер";

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

            AllRooms_RB.IsChecked = true;

            FinishFloorType.ItemsSource = allFloorTypeInProject;
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

        private void MakeFloor_Bt_Click(object sender, RoutedEventArgs e)
        {
            IList<RoomFinishItem> dataItems = FloorDataGrid.ItemsSource as List<RoomFinishItem>;

            string valueOffsetFloor = OffsetFloor.Text;
            double offsetFloor;

            bool resultConvertOffset = double.TryParse(valueOffsetFloor, out offsetFloor);

            IList<ElementId> roomsId = allRoomsInProject
                .Select(room => room.Id)
                .ToList();

            IList<ElementId> floorTypeId = allFloorTypeInProject
                .Select(floorType => floorType.floorType.Id)
                .ToList();

            if (!resultConvertOffset)
            {
                MessageBox.Show(
                    $"Данные в поле \"{OffsetFloorl_TB.Text}\" не являются числом.",
                    "Предупреждение"
                );
            }
            else if (func.IsMeCheckoutElement(_document, _application, roomsId) &&
                     func.IsMeCheckoutElement(_document, _application, floorTypeId))
            {
                //IList<BuilderFloor> buildings = new List<BuilderFloor>();

                foreach (RoomFinishItem roomFinishItem in dataItems)
                {
                    if (roomFinishItem.floorType is null) { continue; }
                    if (roomFinishItem.floorType.floorType is null) { continue; }

                    FloorType currentFloorType = roomFinishItem.floorType.floorType;

                    foreach (Room room in roomFinishItem.rooms)
                    {
                        BuilderFloor builderFloor = new BuilderFloor(_document, room);
                    
                        string transactionName = $"Создание отделки пола, Помещение id {room.Id.IntegerValue}";

                        using (Transaction transaction = new Transaction(_document, transactionName))
                        {
                            transaction.Start();

                            func.SetWarningResolver(transaction, _document);

                            builderFloor.CreateFinishFloor(currentFloorType, offsetFloor, roomFinishItem.hasGround);

                            transaction.Commit();
                        }
                    }    
                }
                using (Transaction transaction = new Transaction(_document, "Регенерация документа"))
                {
                    transaction.Start();
                    _document.Regenerate();
                    transaction.Commit();
                }
            }
            Close();
        }

        private void UpdateGrid(IList<Room> rooms)
        {
            string parameterName;

            if (SelectParameter_CB.SelectedValue is null) 
            {
                parameterName = "Номер";
            }
            else 
            {
                parameterName = SelectParameter_CB.SelectedValue.ToString();
            }

            IList<RoomFinishItem> finishFloorTypes = new List<RoomFinishItem>();

            IEnumerable<IGrouping<string, Room>> groupRooms = rooms
                .GroupBy(room => func.GetStrValueForParameterName(room, parameterName));

            foreach (var groupRoom in groupRooms)
            {
                RoomFinishItem finishFloorType = new RoomFinishItem
                {
                    parameterName = parameterName,
                    parameterValue = groupRoom.Key,
                    rooms = groupRoom.ToList(),
                    floorType = null,
                    hasGround = true,
                };
                finishFloorTypes.Add(finishFloorType);
            }
            FloorDataGrid.ItemsSource = finishFloorTypes
                .OrderBy(finishType => finishType.parameterValue)
                .ToList();
        }

        private void FilterFinishFloor_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void SelectParameter_CB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IList<Room> rooms;

            if (AllRooms_RB.IsChecked is true)
            {
                rooms = allRoomsInProject;
            }
            else if (RoomInActiveView_RB.IsEnabled is true)
            {
                rooms = allRoomsInActiveView;
            }
            else if (SelectRooms_RB.IsEnabled is true)
            {
                rooms = selectionRooms;
            }
            else
            {
                rooms = allRoomsInLevel;
            }

            UpdateGrid(rooms);
        }
    }

    class RoomFinishItem
    {
        public string parameterName {  get; set; }
        public string parameterValue { get; set; }
        public IList<Room> rooms { get; set; }
        public FinishFloorType floorType { get; set; }
        public bool hasGround { get; set; }
    }

    class FinishFloorType
    {
        public string nameType { get; set; }
        public FloorType floorType { get; set; }
        public override string ToString() => nameType;
    }
}
