using glTFLoader.Schema;
using Kokoro.Math;
using KokoroVR2.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KokoroVR2.MeshPipeline
{
    public class ModelLoader
    {
        const string POSITION = "POSITION";
        const string NORMAL = "NORMAL";
        const string TEXCOORD = "TEXCOORD_0";

        private Gltf model;
        private string bdir;
        private int tid;

        private List<byte> vertices;
        private List<byte> normals;
        private List<byte> texcoords;
        private List<byte> indices;

        private List<MeshNode> nodes;
        private List<Vector3> bounds;

        public ModelLoader(int tid)
        {
            this.tid = tid;
        }

        public void BaseUnpack(string file, string output_file)
        {
            var base_dir = /*Path.GetTempPath()*/ Environment.CurrentDirectory;
            var dir_path = Path.Combine(base_dir, "mesh_pipeline_kokorovr2");
            if (Directory.Exists(dir_path))
                Directory.Delete(dir_path, true);
            Directory.CreateDirectory(dir_path);

            glTFLoader.Interface.Unpack(file, dir_path);

            var n_file_orig = Path.ChangeExtension(Path.Combine(dir_path, Path.GetFileName(file)), "gltf");
        }

        public void Load(string file, string output_file, int lod_lv)
        {
            var base_dir = /*Path.GetTempPath()*/ Environment.CurrentDirectory;
            var dir_path = Path.Combine(base_dir, "mesh_pipeline_kokorovr2");
            var n_file_orig = Path.ChangeExtension(Path.Combine(dir_path, Path.GetFileName(file)), "gltf");

            Compressonator compressonator = new Compressonator(tid);
            var n_file = compressonator.Exec(n_file_orig, lod_lv);

            model = glTFLoader.Interface.LoadModel(n_file);
            bdir = dir_path;

            MeshComponent mesh = new MeshComponent();
            mesh.Materials = new PBRMaterial[model.Materials.Length];

            int node_cnt = 0;
            for (int i = 0; i < model.Meshes.Length; i++)
                for (int j = 0; j < model.Meshes[i].Primitives.Length; j++)
                    node_cnt++;

            for (int i = 0; i < model.Materials.Length; i++)
            {
                var mat = model.Materials[i];

                mesh.Materials[i] = new PBRMaterial()
                {
                    AlbedoFactor = new Vector3(mat.PbrMetallicRoughness.BaseColorFactor),
                    AlphaFactor = mat.AlphaMode == Material.AlphaModeEnum.OPAQUE ? 1.0f : mat.AlphaCutoff,
                    EmissiveFactor = new Vector3(mat.EmissiveFactor),
                    MetalnessFactor = mat.PbrMetallicRoughness.MetallicFactor,
                    RoughnessFactor = mat.PbrMetallicRoughness.RoughnessFactor,
                };
            }

            for (int i = 0; i < model.Scenes[model.Scene.Value].Nodes.Length; i++)
            {
                int id = 0;
                nodes = new List<MeshNode>();
                vertices = new List<byte>();
                normals = new List<byte>();
                texcoords = new List<byte>();
                indices = new List<byte>();
                bounds = new List<Vector3>();
                
                BuildNode(model.Scenes[model.Scene.Value].Nodes[i], ref id, -1);
                
                mesh.VertexData = vertices.ToArray();
                mesh.NormalData = normals.ToArray();
                mesh.UVData = texcoords.ToArray();
                mesh.IndexData = indices.ToArray();
                mesh.Nodes = nodes.ToArray();
                mesh.Bounds = bounds.ToArray();

                var output_file_tmp = $"{Path.GetFileNameWithoutExtension(output_file)}_n{i}_lv{lod_lv}{Path.GetExtension(output_file)}";
                mesh.Save(output_file_tmp);
            }
        }

        private void BuildNode(int idx, ref int id, int parent)
        {
            var cur_id = id;
            var node = new MeshNode();
            if (model.Nodes[idx].Mesh.HasValue)
            {
                var mesh_idx = model.Nodes[idx].Mesh.Value;

                for (int j = 0; j < model.Meshes[mesh_idx].Primitives.Length; j++)
                {
                    node.Offset = new Vector3(model.Nodes[idx].Translation);
                    node.Rotation = new Quaternion(model.Nodes[idx].Rotation);
                    node.Scale = new Vector3(model.Nodes[idx].Scale);
                    node.Parent = parent;

                    var idxBuf_b = GetBuffer(model.Meshes[mesh_idx].Primitives[j].Indices.Value);
                    var posBuf_b = GetBuffer(mesh_idx, j, POSITION);
                    var normBuf_b = GetBuffer(mesh_idx, j, NORMAL);
                    var texCo_b = GetBuffer(mesh_idx, j, TEXCOORD);
                    var matID = model.Meshes[mesh_idx].Primitives[j].Material.GetValueOrDefault();

                    Vector3 min = Vector3.One * float.MaxValue;
                    Vector3 max = Vector3.One * float.MinValue;

                    unsafe
                    {
                        fixed(byte* bp = posBuf_b)
                        {
                            float* fp = (float*)bp;

                            for(int i = 0; i < posBuf_b.Length / (3 * sizeof(float)); i++)
                            {
                                var vec = new Vector3(fp[0], fp[1], fp[2]);
                                min = Vector3.ComponentMin(min, vec);
                                max = Vector3.ComponentMax(max, vec);
                                fp += 3;
                            }
                        }
                    }

                    //compress and pack data into a custom format that can be loaded straight to memory
                    node.BaseIndex = (uint)indices.Count / sizeof(uint);
                    node.BaseVertex = (uint)vertices.Count / (3 * sizeof(float));
                    node.IndexCount = (uint)idxBuf_b.Length / sizeof(uint);
                    node.MaterialID = (ushort)matID;
                    node.BoundsID = (ushort)(bounds.Count / 2);

                    indices.AddRange(idxBuf_b);
                    vertices.AddRange(posBuf_b);
                    normals.AddRange(normBuf_b);
                    texcoords.AddRange(texCo_b);

                    bounds.Add(min);
                    bounds.Add(max);

                    nodes.Add(node);
                    id++;
                    node = new MeshNode();
                }
            }
            else
            {
                node.Offset = new Vector3(model.Nodes[idx].Translation);
                node.Rotation = new Quaternion(model.Nodes[idx].Rotation);
                node.Scale = new Vector3(model.Nodes[idx].Scale);
                node.Parent = parent;
                id++;
                nodes.Add(node);
            }

            var children = model.Nodes[idx].Children;
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    BuildNode(children[i], ref id, cur_id);
                }
            }
        }

        private byte[] GetBuffer(int mesh_idx, int primitive_idx, string attr)
        {
            return GetBuffer(model.Meshes[mesh_idx].Primitives[primitive_idx].Attributes[attr]);
        }

        private byte[] GetBuffer(int idx)
        {
            var buf_view = model.BufferViews[idx];
            var data = new byte[buf_view.ByteLength];
            var src_buf = glTFLoader.Interface.LoadBinaryBuffer(model, buf_view.Buffer, Path.Combine(bdir, model.Buffers[buf_view.Buffer].Uri));
            for (int i = 0; i < buf_view.ByteLength; i++)
            {
                data[i] = src_buf[buf_view.ByteOffset + i * (buf_view.ByteStride.HasValue ? buf_view.ByteStride.Value : 1)];
            }
            return data;
        }
    }
}
