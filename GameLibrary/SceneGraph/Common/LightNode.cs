using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.SceneGraph.Common
{ 

    public class LightNode : TransformNode
    {
        public LightNode(String name) : base(name)
        {
        }

        public LightNode(LightNode node) : base(node)
        {
        }

        /*public override Node Clone()
        {
            return new LightNode(this);
        }*/

        internal bool UpdateWorldTransform()
        {
            return UpdateWorldTransform(null);
        }

        internal override bool UpdateWorldTransform(TransformNode parentTransformNode)
        {
            if (base.UpdateWorldTransform(parentTransformNode))
            {
                return true;
            }
            return false;
        }

    }
}
