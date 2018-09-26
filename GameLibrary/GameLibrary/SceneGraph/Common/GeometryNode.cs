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

    public class GeometryNode : TransformNode
    {
        private BoundingVolume localBoundingVolume;
        private BoundingVolume worldBoundingVolume;
            
        private Physics physics;

        public int RenderGroupId { get; set; }

        public int CollisionGroupId { get; set; }

        /// <summary>
        /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        public virtual BoundingVolume LocalBoundingVolume
        {
            get { return localBoundingVolume; }
            internal set { localBoundingVolume = value; }
        }

        /// <summary>
        /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        public virtual BoundingVolume WorldBoundingVolume
        {
            get { return worldBoundingVolume; }
            internal set { worldBoundingVolume = value; }
        }

        public Physics Physics { get { return physics; } }

        public GeometryNode(String name)
            : base(name)
        {
            physics = new Physics();
            CollisionGroupId = -1;
        }

        public GeometryNode(GeometryNode node)
            : base(node)
        {
            RenderGroupId = node.RenderGroupId;
            CollisionGroupId = node.CollisionGroupId;
            localBoundingVolume = node.localBoundingVolume != null ? node.localBoundingVolume.Clone() : null;
            worldBoundingVolume = node.worldBoundingVolume != null ? node.worldBoundingVolume.Clone() : null;

            physics = null;
        }

        public override Node Clone()
        {
            return new GeometryNode(this);
        }

        public virtual void Draw(Scene scene, GameTime gameTime)
        {
        }
    }
}
