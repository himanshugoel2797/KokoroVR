using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public enum FaceIndex
    {
        Top = 0,
        Bottom,
        Left,
        Right,
        Front,
        Back,
    }

    public class Chunk : Interactable
    {
        //128x128x128 byte block
        private byte[] data;
        private object data_locker;
        private bool rebuilding;    //Mesh data is being rebuilt
        private bool updated;

        public Chunk()
        {
            data_locker = new object();
            data = new byte[ChunkStreamer.Side * ChunkStreamer.Side * ChunkStreamer.Side * 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z)
        {
            return (x * (ChunkStreamer.Side * ChunkStreamer.Side) + y * ChunkStreamer.Side + z) * 2;
        }

        public void RebuildMesh()
        {
            //Deploy a thread to do the update and register an event to push the changes to the gpu when it's done
            ThreadPool.QueueUserWorkItem((_) =>
            {
                if (rebuilding) Thread.Sleep(2);
                rebuilding = true;

                for (byte x = 0; x <= ChunkStreamer.Side - 1; x++)
                    for (byte y = 0; y <= ChunkStreamer.Side - 1; y++)
                        for (byte z = 0; z <= ChunkStreamer.Side - 1; z++)
                        {
                            byte cur, top, btm, frt, bck, lft, rgt;
                            lock (data_locker)
                            {
                                cur = data[GetIndex(x, y, z)];
                                top = y > 0 ? data[GetIndex(x, y - 1, z)] : cur;
                                btm = y < ChunkStreamer.Side - 1 ? data[GetIndex(x, y + 1, z)] : cur;
                                frt = z > 0 ? data[GetIndex(x, y, z - 1)] : cur;
                                bck = z < ChunkStreamer.Side - 1 ? data[GetIndex(x, y, z + 1)] : cur;
                                lft = x > 0 ? data[GetIndex(x - 1, y, z)] : cur;
                                rgt = x < ChunkStreamer.Side - 1 ? data[GetIndex(x + 1, y, z)] : cur;
                            }
                            //generate draws per face
                            //sort chunks front to back 
                            int bmp = 0;
                            bmp |= (top == 0) ? 0 : (1 << (int)FaceIndex.Top);
                            bmp |= (btm == 0) ? 0 : (1 << (int)FaceIndex.Bottom);
                            bmp |= (frt == 0) ? 0 : (1 << (int)FaceIndex.Front);
                            bmp |= (bck == 0) ? 0 : (1 << (int)FaceIndex.Back);
                            bmp |= (lft == 0) ? 0 : (1 << (int)FaceIndex.Left);
                            bmp |= (rgt == 0) ? 0 : (1 << (int)FaceIndex.Right);

                            data[GetIndex(x, y, z) + 1] = (byte)(~bmp & 0x3f);
                        }
                //push these changes to the gpu
                //Set rebuilding = false when upload is done
                updated = true;
            });
        }

        public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, Matrix4 p, Matrix4 v, VREye eye)
        {
            //passthrough renderer, as rendering is handled by the chunk streamer
        }

        public override void Update(double time, World parent)
        {
            if (updated)
            {
                //Upload the data
                rebuilding = false;
                updated = false;
            }

            //TODO: trigger rebuilds when voxel data is dirty
        }
    }
}
