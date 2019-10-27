using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Engine.Initialize(ExperienceKind.Standing);
            Engine.AddWorld(new World("TestWorld"));
            Engine.SetActiveWorld("TestWorld");
            Engine.Start();
        }
    }
}
