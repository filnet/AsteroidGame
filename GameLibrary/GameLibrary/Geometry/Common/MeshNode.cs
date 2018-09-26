using System;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;
using GameLibrary.SceneGraph.Common;

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
                LocalBoundingVolume = mesh.BoundingVolume;
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

        public override void Draw(Scene scene, GameTime gameTime)
        {
            //if (!owned)
            //{
            //    Console.WriteLine("Not owned");
            //}
            if (mesh.IndexBuffer != null)
            {
                Scene.GraphicsDevice.SetVertexBuffer(mesh.VertexBuffer);
                Scene.GraphicsDevice.Indices = mesh.IndexBuffer;
                Scene.GraphicsDevice.DrawIndexedPrimitives(mesh.PrimitiveType, 0, 0, mesh.VertexCount, 0, mesh.PrimitiveCount);
            }
            else
            {
                Scene.GraphicsDevice.SetVertexBuffer(mesh.VertexBuffer);
                Scene.GraphicsDevice.DrawPrimitives(mesh.PrimitiveType, 0, mesh.PrimitiveCount);
            }
        }

    }

}

