using Autodesk.Revit.Creation;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace UNI_Tools_AR.CopyScheduleFilter
{
    internal class ScheduleCopyFilter
    {
        private UIApplication uiApplication;
        //private Application application;
        private UIDocument uiDocument;
        private Autodesk.Revit.DB.Document document;

        public ScheduleCopyFilter(
            UIApplication uiApplication,
            //Application application,
            UIDocument uiDocument,
            Autodesk.Revit.DB.Document documen
        )
        {
            this.uiApplication = uiApplication;
            //this.application = application;
            this.uiDocument = uiDocument;
            document = documen;
        }

        private IList<ViewSchedule> GetAllViewSchedules()
        {
            return new FilteredElementCollector(document)
                .OfClass(typeof(ViewSchedule))
                .Select(elements => elements as ViewSchedule)
                .ToList();
        }

        private Dictionary<int, SchedulableField> GetFieldsInSchedule(ViewSchedule viewSchedule)
        {
            ScheduleDefinition scheduleDefinition = viewSchedule.Definition;
            Dictionary<int, SchedulableField> resutlScheduleFields = new Dictionary<int, SchedulableField>();
            foreach (SchedulableField schedulableField in scheduleDefinition.GetSchedulableFields())
            {
                int idField = schedulableField.ParameterId.IntegerValue;
                if (!resutlScheduleFields.ContainsKey(idField))
                {
                    resutlScheduleFields.Add(idField, schedulableField);
                }
            }
            return resutlScheduleFields;
        }

        private IList<SchedulableField> GetIntersecitonFieldsInSchedules(IList<ViewSchedule> selectionSchedule)
        {
            Dictionary<int, SchedulableField> intersecitonFields = new Dictionary<int, SchedulableField>();
            IEnumerable<int> intersectionResult = GetFieldsInSchedule(selectionSchedule.First()).Keys;

            foreach (ViewSchedule schedule in selectionSchedule)
            {
                Dictionary<int, SchedulableField> fieldsInSchedule = GetFieldsInSchedule(schedule);
                intersectionResult = intersectionResult.Intersect(fieldsInSchedule.Keys);

                foreach (int fieldId in fieldsInSchedule.Keys)
                {
                    if (!intersecitonFields.ContainsKey(fieldId))
                    {
                        intersecitonFields.Add(fieldId, fieldsInSchedule[fieldId]);
                    }
                }
            }
            return intersectionResult
                .Select(num => intersecitonFields[num])
                .ToList();
        }

        public ScheduleField GetFieldInSchedule(ViewSchedule schedule, SchedulableField selectField)
        {
            int parameterId = selectField.ParameterId.IntegerValue;
            ScheduleDefinition scheduleDefinition = schedule.Definition;
            IList<ScheduleFieldId> fieldsOrder = scheduleDefinition.GetFieldOrder();

            Dictionary<int, ScheduleField> fieldsToIds = new Dictionary<int, ScheduleField>();
            foreach (ScheduleFieldId fieldId in fieldsOrder)
            {
                ScheduleField scheduleField = scheduleDefinition.GetField(fieldId);
                fieldsToIds[scheduleField.ParameterId.IntegerValue] = scheduleField;
            }
            if (fieldsToIds.ContainsKey(selectField.ParameterId.IntegerValue))
            {
                return fieldsToIds[selectField.ParameterId.IntegerValue];
            }
            return null;
        } 

        public void CreateFilterFields(
            IList<ViewSchedule> selectedSchedules, SchedulableField selectField, string filterValue)
        {
            foreach (ViewSchedule viewSchedule in selectedSchedules)
            {
                ScheduleDefinition scheduleDefinition = viewSchedule.Definition;

                ScheduleField scheduleField = GetFieldInSchedule(viewSchedule, selectField);
                if (scheduleField is null)
                {
                    scheduleField = scheduleDefinition.AddField(selectField);
                    scheduleField.IsHidden = true;
                }

                ElementId parameterId = scheduleField.ParameterId;
                ParameterElement parameterElement = 
                    document.GetElement(parameterId) as ParameterElement;

                Definition definition = parameterElement.GetDefinition();

                if (definition.ParameterType is ParameterType.Text)
                {
                    ScheduleFilter scheduleFilter =
                        new ScheduleFilter(scheduleField.FieldId, ScheduleFilterType.Equal, filterValue);

                    scheduleDefinition.AddFilter(scheduleFilter);
                }
                else
                {
                    TaskDialog.Show("Ошибка", "Параметр не текстовый");
                }
            }
        }

        public bool StartCopy()
        {
            IList<ViewSchedule> allSchedules = GetAllViewSchedules();

            SelectSchedule_Form selectSchedule_Form = new SelectSchedule_Form(allSchedules);
            selectSchedule_Form.ShowDialog();

            if (selectSchedule_Form.Cancel) return false;

            IList<ViewSchedule> selectedSchedules = selectSchedule_Form.GetSelectedSchedule();

            if (selectedSchedules is null) return false;
            if (selectedSchedules.Count() < 1) return false;

            IList<SchedulableField> intersectionFields = 
                GetIntersecitonFieldsInSchedules(selectedSchedules);

            SelectParameters_Form selectionParameters = 
                new SelectParameters_Form(document, intersectionFields);
            selectionParameters.ShowDialog();
            if (selectionParameters.Cancel) return false;

            SchedulableField selectField = 
                selectionParameters.GetSelectedField();

            if (selectField is null) return false;

            ValueForFilterField_Form valueForFilterField = 
                new ValueForFilterField_Form(selectField, document);
            valueForFilterField.ShowDialog();

            if (valueForFilterField.Cancel) return false;

            string filterValue = valueForFilterField.LabelValue.Text;

            using (Transaction t = new Transaction(document, "Добавление фильтров в спецификации"))
            {
                t.Start();
                CreateFilterFields(selectedSchedules, selectField, filterValue);
                t.Commit();
            }
            return true;
        }
    }
}
