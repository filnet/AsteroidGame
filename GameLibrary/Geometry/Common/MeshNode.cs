using System;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.Geometry.Common
{
    public class MeshNode : GeometryNode
    {
        private IMeshFactory meshFactory;

        private Mesh mesh;

        private bool owned;

        public Mesh Mesh
        {
            get { return mesh; }
        }

        public MeshNode(String name, IMeshFactory meshFactory)
            : base(name)
        {
            this.meshFactory = meshFactory;
        }

        public MeshNode(MeshNode node)
            : base(node)
        {
            meshFactory = null;
            mesh = node.mesh;
        }

        public override Node Clone()
        {
            return new MeshNode(this);
        }

        public override void Initialize()
        {
            if (meshFactory != null)
            {
                mesh = meshFactory.CreateMesh(Scene.GraphicsDevice);
                BoundingVolume = mesh.BoundingVolume;
                owned = true;
            }
            base.Initialize();
        }

        public override void Dispose()
        {
            if (owned && (mesh != null))
            {
                mesh.Dispose();
                mesh = null;
            }
        }

        public override void preDraw()
        {
            if (mesh.IndexBuffer != null)
            {
                Scene.GraphicsDevice.SetVertexBuffer(mesh.VertexBuffer);
                Scene.GraphicsDevice.Indices = mesh.IndexBuffer;
            }
            else
            {
                Scene.GraphicsDevice.SetVertexBuffer(mesh.VertexBuffer);
            }
        }

        public override void Draw()
        {
            //if (!owned)
            //{
            //    Console.WriteLine("Not owned");
            //}

            // TODO here we can implement some logic based on time + how many primitives to draw
            // will show the "construction" order
            if (mesh.IndexBuffer != null)
            {
                Scene.GraphicsDevice.DrawIndexedPrimitives(mesh.PrimitiveType, 0, 0, mesh.PrimitiveCount);
            }
            else
            {
                Scene.GraphicsDevice.DrawPrimitives(mesh.PrimitiveType, 0, mesh.PrimitiveCount);
            }
        }

    }

}

