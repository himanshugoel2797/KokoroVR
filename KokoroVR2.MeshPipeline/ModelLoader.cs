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

            CompositeMesh mesh = new CompositeMesh();
            mesh.Materials = new PBRMaterial[model.Materials.Length];

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
                mesh.Root = new MeshNode();
                BuildNode(mesh.Root, model.Scenes[model.Scene.Value].Nodes[i]);
                var output_file_tmp = $"{Path.GetFileNameWithoutExtension(output_file)}_n{i}_lv{lod_lv}{Path.GetExtension(output_file)}";
                mesh.Save(output_file_tmp);
            }
        }

        private void BuildNode(MeshNode node, int idx)
        {
            if (model.Nodes[idx].Mesh.HasValue)
            {
                var mesh_idx = model.Nodes[idx].Mesh.Value;

                node.Mesh = new MeshData[model.Meshes[mesh_idx].Primitives.Length];
                for (int j = 0; j < model.Meshes[mesh_idx].Primitives.Length; j++)
                {
                    var idxBuf_b = GetBuffer(model.Meshes[mesh_idx].Primitives[j].Indices.Value);
                    var posBuf_b = GetBuffer(mesh_idx, j, POSITION);
                    var normBuf_b = GetBuffer(mesh_idx, j, NORMAL);
                    var texCo_b = GetBuffer(mesh_idx, j, TEXCOORD);
                    var matID = model.Meshes[mesh_idx].Primitives[j].Material.GetValueOrDefault();

                    //compress and pack data into a custom format that can be loaded straight to memory
                    node.Mesh[j] = new MeshData()
                    {
                        IndexCount = (uint)idxBuf_b.Length / sizeof(uint),
                        IndexData = idxBuf_b,
                        Name = $"{model.Meshes[mesh_idx].Name}_{j}",
                        MaterialID = (uint)matID,
                        NormalData = normBuf_b,
                        UVData = texCo_b,
                        VertexData = posBuf_b,
                        VertexCount = (uint)posBuf_b.Length / (3u * sizeof(float)),
                    };
                }
            }

            node.Offset = new Vector3(model.Nodes[idx].Translation);
            node.Rotation = new Quaternion(model.Nodes[idx].Rotation);
            node.Scale = new Vector3(model.Nodes[idx].Scale);

            var children = model.Nodes[idx].Children;
            if (children != null)
            {
                node.Children = new MeshNode[children.Length];
                for (int i = 0; i < children.Length; i++)
                {
                    node.Children[i] = new MeshNode();
                    BuildNode(node.Children[i], children[i]);
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
