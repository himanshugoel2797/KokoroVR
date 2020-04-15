using System;

namespace Kokoro.Graphics.Framegraph
{
    public class RenderLayoutEntry
    {
        public AttachmentLoadOp LoadOp { get; set; }
        public AttachmentStoreOp StoreOp { get; set; }
        public ImageLayout DesiredLayout { get; set; }
        public PipelineStage FirstLoadStage { get; set; }
        public PipelineStage LastStoreStage { get; set; }
        public ImageFormat Format { get; set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as RenderLayoutEntry);
        }

        public bool Equals(RenderLayoutEntry entry)
        {
            if (Object.ReferenceEquals(entry, null))
                return false;
            if (Object.ReferenceEquals(this, entry))
                return true;
            if (this.GetType() != entry.GetType())
                return false;
            if (LoadOp != entry.LoadOp)
                return false;
            if (StoreOp != entry.StoreOp)
                return false;
            if (DesiredLayout != entry.DesiredLayout)
                return false;
            if (FirstLoadStage != entry.FirstLoadStage)
                return false;
            if (LastStoreStage != entry.LastStoreStage)
                return false;
            if (Format != entry.Format)
                return false;

            return true;
        }

        public static bool operator ==(RenderLayoutEntry lhs, RenderLayoutEntry rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(RenderLayoutEntry lhs, RenderLayoutEntry rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LoadOp, StoreOp, DesiredLayout, FirstLoadStage, LastStoreStage, Format);
        }
    }
}
