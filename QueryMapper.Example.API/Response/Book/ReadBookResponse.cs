

namespace QueryMapper.Examples.Core
{
    public class ReadBookResponse
    {
        public string Name { get;  set; }
        public string Summary { get;  set; }
        public int PublishYear { get;  set; }
        
        public string ShelfLocation { get;  set; }
        public DateTime CreatedDate { get;  set; }
       
        public string CoverImagePath { get;  set; }

        public int AuthorId { get;  set; }
        public string AuthorName { get;  set; }

        public int CreatedById { get;  set; }
        public string CreatedByName { get;  set; }

        public List<ReadNoteResponse> Notes { get; set; } = new();
    }

    public class ReadNoteResponse
    {
        public string Text { get; set; }
        public bool IsShared { get; set; }
        public int ShareId { get; set; }
        //[JsonIgnore]
        //public NoteShareSetting ShareSetting => Enum.GetValues<NoteShareSetting>().First(x => (int)x == ShareId);
        public DateTime CreatedDate { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; }
    }
}
