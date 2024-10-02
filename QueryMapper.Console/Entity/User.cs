using System.ComponentModel.DataAnnotations.Schema;

namespace QueryMapper.Console
{
    public class User : EntityBase
    {

        private User()
        {
            
        }

        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        [NotMapped]
        public string FullName => FirstName + " " + LastName;

        public string Email { get; private set; }
        public string Password { get; private set; }

        public int ShareId { get; private set; }
        [NotMapped]
        public NoteShareSetting ShareSetting => Enum.GetValues<NoteShareSetting>().First(x => (int)x == ShareId);

        public ICollection<Note> Notes { get; private set; } = new List<Note>();
        public ICollection<User> Friends { get; private set; } = new List<User>();


        //public static User Create(RegisterRequest registerRequest)
        //{
        //    User user = new User();
        //    user.FirstName = registerRequest.FirstName;
        //    user.LastName = registerRequest.LastName;
        //    user.Email = registerRequest.Email;
        //    user.Password = registerRequest.Password;
        //    return user;
        //}

        //public void UpdateShareSetting(int shareId)
        //{
        //    if (!Enum.GetValues<NoteShareSetting>().Select(x=> Convert.ToInt32(x)).Contains(shareId))
        //       throw new Exception("Geçerli bir paylaşım tipi seçilmeli");
        //    ShareId = shareId;
        //}
    }

    public enum NoteShareSetting
    {
        Public,
        FriendsOnly,
        Private
    }
}
