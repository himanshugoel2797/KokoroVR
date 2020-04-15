using System;

namespace Kokoro.Graphics.Framegraph
{
    public enum ImageSizeMode
    {
        Fixed = 0,
        ScreenRelative = 1,
    }

    public class ObjectManager
    {
        //Handle reallocating imageviews etc upon resizing the screen/texture
        //Other objects don't store object references directly, this class also takes care of registering stuff to the framegraph
    }
}