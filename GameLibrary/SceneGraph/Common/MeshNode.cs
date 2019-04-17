using System;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.SceneGraph.Common
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

        public override int VertexCount
        {
            get { return mesh.VertexCount; }
        }

        public MeshNode(String name, IMeshFactory meshFactory) : base(name)
        {
            this.meshFactory = meshFactory;
        }

        // not used...
        public MeshNode(String name, Mesh mesh) : base(name)
        {
            this.mesh = mesh;
            owned = true;
        }

        public MeshNode(MeshNode node) : base(node)
        {
            mesh = node.mesh;
        }

        /*protected void TakeOwnership(Mesh mesh)
        {
            this.mesh = mesh;
            owned = true;
        }*/

        public override Node Clone()
        {
            return new MeshNode(this);
        }

        public override void Initialize(GraphicsDevice graphicsDevice)
        {
            if (meshFactory != null)
            {
                mesh = meshFactory.CreateMesh(graphicsDevice);
                if (mesh == null)
                {
                    // abort
                    return;
                }
                BoundingVolume = mesh.BoundingVolume;
                owned = true;

                if (!mesh.IsDynamic)
                {
                    // mesh factory is not needed anymore
                    // and might hold onto quite a lot of data...
                    meshFactory = null;
                }
            }
            base.Initialize(graphicsDevice);
        }

        public override void Dispose()
        {
            if ((mesh != null) && owned)
            {
                mesh.Dispose();
                mesh = null;
            }
        }

        public override void PreDraw(GraphicsDevice gd)
        {
            gd.SetVertexBuffer(mesh.VertexBuffer);
            if (mesh.IndexBuffer != null)
            {
                gd.Indices = mesh.IndexBuffer;
            }
        }

        public override void Draw(GraphicsDevice gd)
        {
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

        public override void PostDraw(GraphicsDevice gd)
        {
        }

        public override void PreDrawInstanced(GraphicsDevice gd, VertexBuffer instanceVertexBuffer, int instanceOffset)
        {
            VertexBufferBinding[] vertexBufferBindings = {
                new VertexBufferBinding(mesh.VertexBuffer, 0, 0),
                new VertexBufferBinding(instanceVertexBuffer, instanceOffset, 1)
            };
            gd.SetVertexBuffers(vertexBufferBindings);
            if (mesh.IndexBuffer != null)
            {
                gd.Indices = mesh.IndexBuffer;
            }
        }

        public override void DrawInstanced(GraphicsDevice gd, int instanceCount)
        {
            gd.DrawInstancedPrimitives(mesh.PrimitiveType, 0, 0, mesh.PrimitiveCount, instanceCount);
        }

        public override void PostDrawInstanced(GraphicsDevice gd)
        {
        }

    }

}

