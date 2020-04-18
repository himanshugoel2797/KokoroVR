using System;

namespace Kokoro.Common
{
    public abstract class UniquelyNamedObject
    {
        public static bool ThrowOnUniquenessViolation { get; set; } = false;


        public string Name { get; }

        public UniquelyNamedObject(string name)
        {
            this.Name = name;
#if DEBUG
            if (ThrowOnUniquenessViolation)
                throw new NotImplementedException();
#endif
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as UniquelyNamedObject);
        }

        public bool Equals(UniquelyNamedObject layout)
        {
            if (Object.ReferenceEquals(layout, null))
                return false;

            if (Object.ReferenceEquals(this, layout))
                return true;

            if (this.GetType() != layout.GetType())
                return false;

            if (Name != layout.Name)
                return false;
            return true;
        }

        public static bool operator ==(UniquelyNamedObject lhs, UniquelyNamedObject rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(UniquelyNamedObject lhs, UniquelyNamedObject rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
