using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Materials
{
    public class PBRMaterial
    {
        //Color: 16DiffR:16DiffG:16DiffB:16Roughness
        //Normal: 16NX:16NY:16DerivX:16DerivY
        //Specular: 16SpecR:16SpecG:16SpecB
        //Depth: 32D
        public Texture Diffuse { get; set; }    //R,G,B,Roughness
        public Texture Specular { get; set; }   //SR,SG,SB
        public Texture Derivative { get; set; } //DX,DY
    }
}
