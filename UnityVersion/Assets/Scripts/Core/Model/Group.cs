using System.Collections.Generic;

namespace SichuanMahjong.Core.Model
{
    public class Group : Tiles
    {
        private GroupEnum category;
        private int identification;

        public Group(List<Tile> tiles, GroupEnum category, int identification) : base(tiles)
        {
            this.category = category;
            this.identification = identification;
        }

        public void SetCategory(GroupEnum category)
        {
            this.category = category;
        }

        public GroupEnum GetCategory()
        {
            return category;
        }

        /// <summary>
        /// Same quirk as the Java original: the duplicate shares this group's
        /// tile list and takes the pre-increment identification, so it never
        /// compares equal to the source group.
        /// </summary>
        public Group GetDup()
        {
            return new Group(tiles, category, identification++);
        }

        public override string ToString()
        {
            return "Group{category=" + category + ", tiles=[" + string.Join(", ", tiles) + "]}";
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(this, o)) return true;
            if (o == null || GetType() != o.GetType()) return false;
            if (!base.Equals(o)) return false;
            Group group = (Group)o;
            return identification == group.identification && category == group.category;
        }

        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            result = result * 31 + (int)category;
            result = result * 31 + identification;
            return result;
        }
    }
}
