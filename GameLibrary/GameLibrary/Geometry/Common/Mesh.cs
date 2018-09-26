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
        
        protected VertexDeclaration vertexDeclaration;

        protected VertexBuffer vertexBuffer;

        protected IndexBuffer indexBuffer;
        
        protected PrimitiveType primitiveType;
        
        protected int vertexCount;
        
        protected int primitiveCount;

        protected BoundingVolume boundingVolume;

        #endregion

        #region Properties

        public VertexDeclaration VertexDeclaration
        {
            get { return vertexDeclaration; }
            internal set { vertexDeclaration = value; }
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
            //internal set { primitiveType = value; }
        }

        public int VertexCount
        {
            get { return vertexCount; }
            internal set { vertexCount = value; }
        }

        public int PrimitiveCount
        {
            get { return primitiveCount; }
            //set { primitiveCount = value; }
        }

        public BoundingVolume BoundingVolume
        {
            get { return boundingVolume; }
            set { boundingVolume = value; }
        }

        #endregion

        #region Constructors

        public Mesh(PrimitiveType primitiveType, int primitiveCount)
        {
            this.primitiveType = primitiveType;
            this.primitiveCount = primitiveCount;
            vertexCount = -1;
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
