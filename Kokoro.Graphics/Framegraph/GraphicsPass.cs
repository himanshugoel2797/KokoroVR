using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

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

    public abstract class UniquelyNamedObject
    {
        public string Name { get; set; }
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

    public class BufferTransferPass : UniquelyNamedObject
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public ulong SourceOffset { get; set; }
        public ulong DestinationOffset { get; set; }
        public ulong Size { get; set; }
    }

    public class ImageTransferPass : UniquelyNamedObject
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public ulong SourceOffset { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint Depth { get; set; }
        public uint MipLevel { get; set; }
        public uint BaseArrayLayer { get; set; }
        public uint LayerCount { get; set; }
    }

    public class GraphicsPass : UniquelyNamedObject
    {
        public string[] Shaders { get; set; }
        public DescriptorSetup DescriptorSetup { get; set; }
        public RenderLayout RenderLayout { get; set; }
        public PrimitiveType Topology { get; set; } = PrimitiveType.Triangle;
        public bool RasterizerDiscard { get; set; } = false;
        public float LineWidth { get; set; } = 1.0f;
        public CullMode CullMode { get; set; } = CullMode.Back;

        public bool EnableBlending { get; set; } = false;

        public DepthTest DepthTest { get; set; } = DepthTest.Greater;
        public bool DepthWriteEnable { get; set; } = true;
        public bool DepthClamp { get; set; } = false;

        public bool ViewportDynamic { get; set; } = false;
        public uint ViewportX { get; set; } = 0;
        public uint ViewportY { get; set; } = 0;
        public uint ViewportWidth { get; set; }
        public uint ViewportHeight { get; set; }
        public uint ViewportMinDepth { get; set; } = 0;
        public uint ViewportMaxDepth { get; set; } = 1;
    }
}
