using System.ComponentModel.DataAnnotations.Schema;

namespace QueryMapper.Examples.Core
{
    public abstract class EntityBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public bool IsActive { get; private set; } = true;

        public void Activate()
        {
            IsActive = true;
        }

        public void DeActivate()
        {
            IsActive = false;
        }
    }
}
