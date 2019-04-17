using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.SceneGraph.Common
{
    /**
     * Geometry Node
     *  
     * Defines geometric model, material, and physical properties for physical simulation
     * Geometric Model
     *     - Model mesh
     *     - Shader for rendering the model
     *     - Bounding box
     *     - ...
     * Material
     *     - Diffuse color
     *     - Specular color
     *     - Specular power
     *     - Emissive color
     *     - Transparency
     *     - Texture
     * Physical Properties
     *     - Mass, Shape, ...
     *     - Pickable, Interactable, Collidable, IsVehicle,…
     *     - Linear/Angular Damping, Restitution, Friction,…
     *     - Initial Linear/Angular Velocity
     public
    */
    public class Physics
    {
        public float Mass { get; set; }

        public Vector3 LinearVelocity { get; set; }

        public Vector3 AngularVelocity { get; set; }

        public Physics()
        {
            Mass = 1.0f;
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
        }

    }

    public abstract class GeometryNode : TransformNode, Physical, Drawable
    {
        private Volume boundingVolume;
        private Volume worldBoundingVolume;

        public bool BoundingVolumeVisible { get; set; }

        private Physics physics;

        public int RenderGroupId { get; set; }

        public int CollisionGroupId { get; set; }

        /// <summary>
        /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        public Volume BoundingVolume
        {
            get { return boundingVolume; }
            internal set { boundingVolume = value; }
        }

        /// <summary>
        /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        public Volume WorldBoundingVolume
        {
            get { return worldBoundingVolume; }
            internal set { worldBoundingVolume = value; }
        }

        public Physics Physics { get { return physics; } }

        public GeometryNode(String name) : base(name)
        {
            physics = new Physics();
            CollisionGroupId = -1;
            BoundingVolumeVisible = true;
        }

        public GeometryNode(GeometryNode node) : base(node)
        {
            RenderGroupId = node.RenderGroupId;
            CollisionGroupId = node.CollisionGroupId;
            BoundingVolumeVisible = node.BoundingVolumeVisible;

            boundingVolume = node.boundingVolume != null ? node.boundingVolume.Clone() : null;
            worldBoundingVolume = node.worldBoundingVolume != null ? node.worldBoundingVolume.Clone() : null;

            physics = null;
        }

        /*public override Node Clone()
        {
            return new GeometryNode(this);
        }*/

        internal bool UpdateWorldTransform()
        {
            return UpdateWorldTransform(null);
        }

        internal override bool UpdateWorldTransform(TransformNode parentTransformNode)
        {
            if (base.UpdateWorldTransform(parentTransformNode))
            {
                if (BoundingVolume != null)
                {
                    // TODO : do not create garbage
                    worldBoundingVolume = BoundingVolume.Transform(WorldTransform, worldBoundingVolume);
                }
                return true;
            }
            return false;
        }

        public abstract int VertexCount { get; }

        public virtual void PreDraw(GraphicsDevice gd) { throw new NotSupportedException(); }
        public virtual void Draw(GraphicsDevice gd) { throw new NotSupportedException(); }
        public virtual void PostDraw(GraphicsDevice gd) { throw new NotSupportedException(); }

        public virtual void PreDrawInstanced(GraphicsDevice gd, VertexBuffer instanceVertexBuffer, int intstanceOffset) { throw new NotSupportedException(); }
        public virtual void DrawInstanced(GraphicsDevice gd, int intstanceCount) { throw new NotSupportedException(); }
        public virtual void PostDrawInstanced(GraphicsDevice gd) { throw new NotSupportedException(); }

    }
}
