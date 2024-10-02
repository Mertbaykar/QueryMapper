using System.ComponentModel.DataAnnotations.Schema;

namespace QueryMapper.Examples.Core
{
    [Table("FriendShip")]
    public class UserFriend
    {
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public int FriendId { get; set; }
        [ForeignKey(nameof(FriendId))]
        public User Friend { get; set; }
    }
}
