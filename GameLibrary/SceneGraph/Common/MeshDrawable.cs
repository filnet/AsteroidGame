using GameLibrary.Geometry.Common;
using GameLibrary.SceneGraph.Bounding;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.SceneGraph.Common
{
    public class MeshDrawable : AbstractDrawable
    {
        public override Volume BoundingVolume
        {
            get;
        }

        public override Volume WorldBoundingVolume
        {
            get
            {
                return BoundingVolume;
            }
        }

        public override int VertexCount
        {
            get { return mesh.VertexCount; }
        }

        private readonly Mesh mesh;

        public MeshDrawable(int renderGroupId, Mesh mesh)
        {
            this.mesh = mesh;
            Enabled = true;
            Visible = true;
            RenderGroupId = renderGroupId;
            BoundingVolume = mesh.BoundingVolume;
            BoundingVolumeVisible = true;
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
