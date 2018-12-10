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
    public class TransformNode : GroupNode, Transform
    {
        private Vector3 scale = Vector3.One;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 translation = Vector3.Zero;

        private Matrix transform = Matrix.Identity;
        private Matrix worldTransform = Matrix.Identity;

        public Vector3 Scale
        {
            get { return scale; }
            set
            {
                if (!scale.Equals(value))
                {
                    scale = value;
                    setTransformDirty();
                }
            }
        }

        public Quaternion Rotation
        {
            get { return rotation; }
            set
            {
                if (!rotation.Equals(value))
                {
                    rotation = value;
                    setTransformDirty();
                }
            }
        }

        public Vector3 Translation
        {
            get { return translation; }
            set
            {
                if (!translation.Equals(value))
                {
                    translation = value;
                    setTransformDirty();
                }
            }
        }

        public Matrix Transform
        {
            get { return transform; }
            set { transform = value; setWorldTransformDirty(); }
        }

        public Matrix WorldTransform
        {
            get { return worldTransform; }
            private set { worldTransform = value; }
        }

        public TransformNode(String name) : base(name)
        {
            // TODO accept only transformable nodes?
            setTransformDirty();
        }

        public TransformNode(TransformNode node) : base(node)
        {
            Scale = node.Scale;
            Rotation = node.Rotation;
            Translation = node.Translation;

            //Transform = node.Transform;
            //WorldTransform = node.WorldTransform;

            setTransformDirty();
        }

        public override Node Clone()
        {
            return new TransformNode(this);
        }

        public override void Initialize(GraphicsDevice graphicsDevice)
        {
            base.Initialize(graphicsDevice);
        }

        internal bool UpdateTransform()
        {
            if (!isDirty(Node.DirtyFlag.Transform))
            {
                return false;
            }

            Transform = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Translation);

            clearDirty(Node.DirtyFlag.Transform);

            return true;
        }

        internal virtual bool UpdateWorldTransform(TransformNode parentTransformNode)
        {
            if (!isDirty(Node.DirtyFlag.WorldTransform) && (parentTransformNode == null || !parentTransformNode.isDirty(Node.DirtyFlag.ChildWorldTransform)))
            {
                return false;
            }
            //if (!isDirty(Node.DirtyFlag.WorldTransform)) return false;

            Matrix parentWorldTransform = (parentTransformNode != null) ? parentTransformNode.WorldTransform : Matrix.Identity;
            WorldTransform = Transform * parentWorldTransform;
            clearDirty(Node.DirtyFlag.WorldTransform);

            // trigger a "deep" world transform update
            setDirty(DirtyFlag.ChildWorldTransform);
            //setChildDirty(DirtyFlag.WorldTransform, -1);

            return true;
        }

        private void setTransformDirty()
        {
            setDirty(DirtyFlag.Transform);
            setParentDirty(DirtyFlag.ChildTransform);
        }

        private void setWorldTransformDirty()
        {
            setDirty(DirtyFlag.WorldTransform);
            //setDirty(DirtyFlag.ChildWorldTransform);
            //setParentDirty(DirtyFlag.ChildWorldTransform);
        }

    }
}
