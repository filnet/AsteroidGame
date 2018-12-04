using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLibrary;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;

namespace AsteroidGame.Control
{
    public class WrapController : BaseNodeController<GroupNode>
    {
        internal GeometryNode mainNode;

        public WrapController(GroupNode node) : base(node)
        {
            //for(LinkedListNode<Node> it = Node.Nodes.First; it != null; it = it.Next)

            //            for(LinkedListNode<MyClass> it = myCollection.First; it != null; it = it.Next)
            //{
            //    if(it.Value.removalCondition == true)
            //        it.Value = null;
            //}
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (mainNode == null)
            {
                LinkedListNode<Node> it = Node.Nodes.First;
                mainNode = (it != null) ? it.Value as GeometryNode : null;
                mainNode.AddController(new WrapperController(mainNode));
            }
        }

        class WrapperController : BaseNodeController<GeometryNode>
        {
            private GeometryNode node1;
            private GeometryNode node2;
            private GeometryNode node3;

            public WrapperController(GeometryNode node)
                : base(node)
            {
            }


            //private Vector3 tmpX = new Vector3(dX, 0, 0);
            //private Vector3 tmpX = new Vector3(dX, 0, 0);

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                float dX = 0;
                float dY = 0;

                // BoundingSphere can be changed by other controllers during the update...
                // Controllers should not be registered in nodes...
                GameLibrary.SceneGraph.Bounding.BoundingSphere bs = Node.WorldBoundingVolume as GameLibrary.SceneGraph.Bounding.BoundingSphere;
                if (bs == null)
                {
                    return;
                }
                Vector3 p = /*Node.Translation +*/ bs.Center;
                float s = bs.Radius;
                if (p.X < -(3.0f - s))
                {
                    dX = 6.0f;
                }
                else if (p.X > 3.0f - s)
                {
                    dX = -6.0f;
                }
                if (p.Y < -(3.0f - s))
                {
                    dY = 6.0f;
                }
                else if (p.Y > 3.0f - s)
                {
                    dY = -6.0f;
                }
                // TODO add safety margin in the code above (margin of Radius?)
                // TODO remove clip effect from main node if not needed !!!
                if (dX != 0)
                {
                    sync(ref node1, dX, 0);
                }
                else
                {
                    unsync(node1);
                }
                if (dY != 0)
                {
                    sync(ref node2, 0, dY);
                }
                else
                {
                    unsync(node2);
                }
                if (dX != 0 && dY != 0)
                {
                    sync(ref node3, dX, dY);
                }
                else
                {
                    unsync(node3);
                }
            }

            private void unsync(GeometryNode node)
            {
                if (node != null)
                {
                    node.Visible = false;
                    node.Enabled = false;
                }
            }

            private void sync(ref GeometryNode node, float dX, float dY)
            {
                Vector3 delta = new Vector3(dX, dY, 0);
                if (node == null)
                {
                    node = Node.Clone() as GeometryNode;
                    node.CollisionGroupId = -1;
                    node.Translation = Node.Translation + delta;
                    Node.ParentNode.Add(node);
                }
                else
                {
                    node.Visible = true;
                    node.Enabled = true;
                    node.Scale = Node.Scale;
                    node.Translation = Node.Translation + delta;
                    node.Rotation = Node.Rotation;
                }
            }

        }
    }
}
