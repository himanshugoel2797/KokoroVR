using System;

namespace Kokoro.Graphics.Framegraph
{
    public struct RenderLayout
    {
        public RenderLayoutEntry[] Color { get; set; }
        public RenderLayoutEntry Depth { get; set; }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(obj, null))
                return false;

            if (Object.ReferenceEquals(this, obj))
                return true;

            if (this.GetType() != obj.GetType())
                return false;

            return this.Equals((RenderLayout)obj);
        }

        public bool Equals(RenderLayout layout)
        {
            if (Color == null && layout.Color != null)
                return false;

            if (Color != null && layout.Color == null)
                return false;

            if (Depth != layout.Depth)
                return false;

            if (Color != null)
            {
                if (Color.Length != layout.Color.Length)
                    return false;

                for (int i = 0; i < Color.Length; i++)
                    if (Color[i] != layout.Color[i])
                        return false;
            }

            return true;
        }

        public static bool operator ==(RenderLayout lhs, RenderLayout rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(RenderLayout lhs, RenderLayout rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (Color != null)
                for (int i = 0; i < Color.Length; i++)
                    hash = HashCode.Combine(hash, Color[i]);
            hash = HashCode.Combine(hash, Depth);
            return hash;
        }
    }
}
