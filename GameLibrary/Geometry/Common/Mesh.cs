using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.Geometry.Common
{

    public class Mesh : IDisposable
    {
        #region Member Fields
        
        protected readonly bool isDynamic;

        protected VertexBuffer vertexBuffer;

        protected IndexBuffer indexBuffer;

        protected readonly PrimitiveType primitiveType;

        protected readonly int primitiveCount;

        protected Volume boundingVolume;

        #endregion

        #region Properties

        public bool IsDynamic
        {
            get { return isDynamic; }
        }

        public VertexBuffer VertexBuffer
        {
            get { return vertexBuffer; }
            internal set { vertexBuffer = value; }
        }

        public IndexBuffer IndexBuffer
        {
            get { return indexBuffer; }
            internal set { indexBuffer = value; }
        }

        /// <summary>
        /// Gets or sets primitive type used to render this mesh. 
        /// Default is PrimitiveType.TriangleList
        /// </summary>
        public PrimitiveType PrimitiveType
        {
            get { return primitiveType; }
        }

        public int VertexCount
        {
            get { return vertexBuffer.VertexCount; }
        }

        public int PrimitiveCount
        {
            get { return primitiveCount; }
        }

        public Volume BoundingVolume
        {
            get { return boundingVolume; }
            set { boundingVolume = value; }
        }

        #endregion

        #region Constructors

        public Mesh(PrimitiveType primitiveType, int primitiveCount) : this(primitiveType, primitiveCount, false)
        {
        }

        public Mesh(PrimitiveType primitiveType, int primitiveCount, bool isDynamic)
        {
            this.isDynamic = isDynamic;
            this.primitiveType = primitiveType;
            this.primitiveCount = primitiveCount;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (vertexBuffer != null)
            {
                vertexBuffer.Dispose();
                vertexBuffer = null;
            }
            if (indexBuffer != null)
            {
                indexBuffer.Dispose();
                indexBuffer = null;
            }
        }

        #endregion
    }
}
