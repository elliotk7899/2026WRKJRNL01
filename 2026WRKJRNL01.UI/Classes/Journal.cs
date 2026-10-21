using _2026WRKJRNL01.UI.Components.Settings;
using MudBlazor;
using Swashbuckle.AspNetCore.SwaggerUI;
using WRKJRNL01.Data;
using WRKJRNL01.Data.Models;

namespace Classes
{
    public class Journal
    {
        public WorkLocation WorkLocation { get; set; } = WorkLocation.None;
        public List<JournalDay> DocumentedDays { get; set; } = new List<JournalDay>();
        public List<Category> Categories { get; set; } = new List<Category>();

        public bool CheckIfDayExists(DateTime? ChosenDay)
        {
            foreach(JournalDay DocDay in DocumentedDays)
            {
                if (DocDay.Day == ChosenDay)
                {
                    return true;
                }
            }

            return false;
        }

        public JournalDay? FindJournalDay(DateTime? ChosenDay)
        {
            foreach (var DocDay in DocumentedDays)
            {
                if (DocDay.Day == ChosenDay)
                {
                    return DocDay;
                }
            }

            return null;
        }

        public JournalDay? FindJournalDay(Tasque task)
        {
            return DocumentedDays.FirstOrDefault(t => t.Tasks.Contains(task));
        }

        public double HoursInCategoryInRange(Category Chosen, DateRange? Range)
        {
            double Hours = 0;

            if (Range is null)
            {
                return Hours;
            }

            double DaysBetween = (Range.End.GetValueOrDefault() - Range.Start.GetValueOrDefault()).TotalDays;
            DateTime StartDay = Range.Start.GetValueOrDefault();

            for (int i = 0; i <= DaysBetween; i++)
            {
                if (CheckIfDayExists(StartDay.AddDays(i)))
                {
                    foreach (var Item in FindJournalDay(StartDay.AddDays(i)).Tasks)
                    {
                        if (Item.TaskCategory == Chosen)
                        {
                            Hours += (double)Item.Hours;
                        }
                    }
                }
            }

            return Hours;
        }

        public double HoursInCategoryInRange(Category Chosen, List<Tasque> taskList)
        {
            double Hours = 0;
            
            foreach (var Item in taskList)
            {
                if (Item.CatId == Chosen.PkId)
                {
                    Hours += (double)Item.Hours;
                }
            }

            return Hours;
        }

        public List<Tasque> TasksInDateRange(DateRange? Range)
        {
            if (Range is null)
            {
                return new List<Tasque>(); //return empty list
            }

            DateTime StartDay = Range.Start.GetValueOrDefault();

            double DaysBetween = (Range.End.GetValueOrDefault() - Range.Start.GetValueOrDefault()).TotalDays;
            
            //temporary list to store our tasks
            List<Tasque> FoundTasksList = new List<Tasque>();

            //dreaded for i loop
            for (int i = 0; i <= DaysBetween; i++)
            {
                if (CheckIfDayExists(StartDay.AddDays(i)))
                {
                    //get tasks from those days
                    var results = FindJournalDay(StartDay.AddDays(i))?.Tasks;

                    //if there are any tasks, return them!
                    if (results != null)
                        FoundTasksList.AddRange(results);
                }
            }

            return FoundTasksList;
        }

        public DateTime? FindTaskDate(Tasque Chosen)
        {
            foreach (var Day in DocumentedDays)
            {
                foreach (var task in Day.Tasks)
                {
                    if (task == Chosen)
                    {
                        return Day.Day;
                    }
                }
            }

            return null;
        }

        public void OrganizeTasks(List<Tasque> taskList)
        {
            foreach (var task in taskList)
            {
                if (!CheckIfDayExists(task.Date))
                {
                    JournalDay NewJournalDay = new JournalDay()
                    {
                        Day = task.Date,
                        Tasks = new List<Tasque>()
                    };

                    NewJournalDay.Tasks.Add(task);
                    DocumentedDays.Add(NewJournalDay);
                }
                else
                {
                    FindJournalDay(task.Date).Tasks.Add(task);
                }
            }
        }
    }
}
//e