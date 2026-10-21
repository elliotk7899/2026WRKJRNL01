using WRKJRNL01.Data.Models;

namespace Classes
{
    public class JournalDay
    {
        public DateTime? Day { get; set; }
        public List<Tasque> Tasks { get; set; }

    }
}
