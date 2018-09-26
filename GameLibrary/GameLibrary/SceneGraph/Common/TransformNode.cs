using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.SceneGraph.Common
{
    /**
     * Transform Node
     * Defines transformation of nodes with transform property
     * - Scaling
     * - Rotation
     * - Translation
     * Applies to Camera Node, Geometry Node, Light Node, Particle Node, Sound Node, LOD Node
     * Transform nodes can be cascaded to compose transformations
     */
    public class TransformNode : GroupNode
    {
        private Vector3 scale = Vector3.One;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 translation = Vector3.Zero;

        private Matrix localMatrix = Matrix.Identity;
        private Matrix worldMatrix = Matrix.Identity;

        private Boolean dirty;

        public Vector3 Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        public Quaternion Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        public Vector3 Translation
        {
            get { return translation; }
            set { translation = value; }
        }

        public Matrix LocalMatrix
        {
            get { return localMatrix; }
            set { localMatrix = value; }
        }

        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
            set { worldMatrix = value; }
        }

        public TransformNode(String name)
            : base(name)
        {
            // TODO accept only transformable nodes?
        }

        public TransformNode(TransformNode node)
            : base(node)
        {
            scale = node.scale;
            rotation = node.rotation;
            translation = node.translation;

            localMatrix = node.localMatrix;
            worldMatrix = node.worldMatrix;
        }

        public override Node Clone()
        {
            return new TransformNode(this);
        }

        //public override void Dispose()
        //{
        //    base.Dispose();
        //    if (!sharedDrawable && drawableGameComponent != null)
        //    {
        //        drawableGameComponent.Dispose();
        //    }
        //}

        //protected override void LoadContent()
        //{
        //    base.LoadContent();
        //}

        //public override void Draw(GameTime gameTime)
        //{
        //    base.Draw(gameTime);
        //    if (drawableGameComponent != null)
        //    {
        //        drawableGameComponent.Draw(gameTime);
        //    }
        //}
    }
}
