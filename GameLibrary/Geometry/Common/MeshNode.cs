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

        public override void Initialize(GraphicsDevice graphicsDevice)
        {
            if (meshFactory != null)
            {
                mesh = meshFactory.CreateMesh(graphicsDevice);
                BoundingVolume = mesh.BoundingVolume;
                owned = true;
            }
            base.Initialize(graphicsDevice);
        }

        public override void Dispose()
        {
            if (owned && (mesh != null))
            {
                mesh.Dispose();
                mesh = null;
            }
        }

        public override void preDraw(GraphicsDevice gd)
        {
            if (mesh.IndexBuffer != null)
            {
                gd.SetVertexBuffer(mesh.VertexBuffer);
                gd.Indices = mesh.IndexBuffer;
            }
            else
            {
                gd.SetVertexBuffer(mesh.VertexBuffer);
            }
        }

        public override void Draw(GraphicsDevice gd)
        {
            //if (!owned)
            //{
            //    Console.WriteLine("Not owned");
            //}

            // TODO here we can implement some logic based on time + how many primitives to draw
            // will show the "construction" order
            if (mesh.IndexBuffer != null)
            {
                gd.DrawIndexedPrimitives(mesh.PrimitiveType, 0, 0, mesh.PrimitiveCount);
            }
            else
            {
                gd.DrawPrimitives(mesh.PrimitiveType, 0, mesh.PrimitiveCount);
            }
        }

    }

}

