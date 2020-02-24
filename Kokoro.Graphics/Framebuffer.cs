using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public enum AttachmentKind
    {
        ColorAttachment0,
        ColorAttachment1,
        ColorAttachment2,
        ColorAttachment3,
        ColorAttachment4,
        ColorAttachment5,
        ColorAttachment6,
        ColorAttachment7,
        ColorAttachment8,
        ColorAttachment9,
        DepthAttachment = 1024,
    }
    public class Framebuffer
    {
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public IDictionary<AttachmentKind, ImageView> Attachments { get; }

        public Framebuffer(uint w, uint h)
        {
            Attachments = new Dictionary<AttachmentKind, ImageView>();
            Width = w;
            Height = h;
        }

        public ImageView this[AttachmentKind attachment]
        {
            set
            {
                Attachments[attachment] = value;
            }
            get
            {
                return Attachments[attachment];
            }
        }
    }
}
