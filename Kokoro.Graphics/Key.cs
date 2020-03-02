using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Glfw;

namespace KokoroVR2.Input
{
    public enum Key
    {
        Space = GlfwKeySpace,
        Up = GlfwKeyUp,
        Down = GlfwKeyDown,
        Left = GlfwKeyLeft,
        Right = GlfwKeyRight,
        PageUp = GlfwKeyPageUp,
        PageDown = GlfwKeyPageDown,
        Home = GlfwKeyHome,
        End = GlfwKeyEnd,
    }
}
