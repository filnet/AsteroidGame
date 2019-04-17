using GameLibrary.Component.Camera;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.SceneGraph
{
    public class SimpleCamera : AbstractCamera
    {
        public override Vector3 Position { get { return position; } set => throw new NotImplementedException(); }

        public override Vector3 ViewDirection { get { return viewDirection; } }

        public override Matrix ProjectionMatrix { get { return projectionMatrix; } }

        public override Matrix ViewMatrix { get { return viewMatrix; } }

        public override Matrix ViewProjectionMatrix { get { return viewProjectionMatrix; } }

        public override Matrix InverseViewProjectionMatrix { get { return inverseViewProjectionMatrix; } }

        public override int VisitOrder { get { return visitOrder; } }

        public override Bounding.Frustum Frustum { get { return frustum; } }

        public Vector3 position;
        public Vector3 viewDirection;

        public Matrix viewMatrix;
        public Matrix projectionMatrix;
        public Matrix viewProjectionMatrix;

        public Matrix inverseViewProjectionMatrix;

        public int visitOrder;

        public Bounding.Frustum frustum;
        public Bounding.Sphere boundingSphere;
        public Bounding.Box boundingBox;

        public SimpleCamera() : base()
        {
            frustum = new Bounding.Frustum();
            //cullRegion = new Bounding.Region();
        }

        public SimpleCamera(Camera camera) : this()
        {
            position = camera.Position;
            viewDirection = camera.ViewDirection;

            viewMatrix = camera.ViewMatrix;
            projectionMatrix = camera.ProjectionMatrix;
            viewProjectionMatrix = camera.ViewProjectionMatrix;
            inverseViewProjectionMatrix = camera.InverseViewProjectionMatrix;

            visitOrder = camera.VisitOrder;

            frustum.Matrix = viewProjectionMatrix;
            //boundingBox = camera.BoundingBox.Clone() as Bounding.Box;
            //boundingSphere = camera.BoundingSphere.Clone() as Bounding.Sphere;
        }

        public SimpleCamera(Camera camera, ref Plane plane) : this()
        {
            // http://www.authorstream.com/Presentation/harishchandraraj-2487578-reflection-point-line-plane-space/
            float normalLengthSquared = Vector3.Dot(plane.Normal, plane.Normal);

            // reflect position
            float d = plane.DotCoordinate(camera.Position);
            position.X = camera.Position.X - 2.0f * plane.Normal.X * d / normalLengthSquared;
            position.Y = camera.Position.Y - 2.0f * plane.Normal.Y * d / normalLengthSquared;
            position.Z = camera.Position.Z - 2.0f * plane.Normal.Z * d / normalLengthSquared;

            // reflect direction
            // note the use of DorNormal as we are reflecting a direction, not a position
            d = plane.DotNormal(camera.ViewDirection);
            viewDirection.X = camera.ViewDirection.X - 2 * plane.Normal.X * d / normalLengthSquared;
            viewDirection.Y = camera.ViewDirection.Y - 2 * plane.Normal.Y * d / normalLengthSquared;
            viewDirection.Z = camera.ViewDirection.Z - 2 * plane.Normal.Z * d / normalLengthSquared;

            Vector3 eye = position;
            Vector3 target = position + viewDirection;
            Vector3 up = -Vector3.Up;
            Matrix.CreateLookAt(ref eye, ref target, ref up, out viewMatrix);

            projectionMatrix = camera.ProjectionMatrix;
            Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out viewProjectionMatrix);
            inverseViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);

            visitOrder = VectorUtil.visitOrder(viewDirection);

            frustum.Matrix = viewProjectionMatrix;
            //boundingBox = camera.BoundingBox.Clone() as Bounding.Box;
            //boundingSphere = camera.BoundingSphere.Clone() as Bounding.Sphere;
        }

        // MUST KEEP
        // http://www.shawnhargreaves.com/blogindex.html

        // animating water
        // http://graphicsrunner.blogspot.com/2010/08/water-using-flow-maps.html

        // http://terathon.com/lengyel/Lengyel-Oblique.pdf
        // 
        // http://tomhulton.blogspot.com/2015/08/portal-rendering-with-offscreen-render.html?m=1
        // http://aras-p.info/texts/obliqueortho.html

        // The DirectX formulation of this algorithm is indeed different because of the previously mentioned difference
        // in the normalized device coordinates bounds in the z direction: [-1,1] for OpenGL and[0, 1] for DirectX.
        // Although I haven't tested this myself, you should be able to get the best matrix for DirectX by making
        // the following changes to the code. A "2.0F" changes to a "1.0F", and a "+ 1.0F" is removed.
        //
        // Calculate the scaled plane vector
        // Vector4D c = clipPlane * (1.0F / Dot(clipPlane, q));
        //
        // Replace the third row of the projection matrix
        // matrix[2] = c.x;
        // matrix[6] = c.y;
        // matrix[10] = c.z;
        // matrix[14] = c.w;
        public void MakeOblique(ref Plane clipPlane)
        {
            Vector4 plane = new Vector4(clipPlane.Normal, clipPlane.D);
            MakeOblique(ref plane);
        }

        public void MakeOblique(ref Vector4 clipPlane)
        {
            // Calculate the clip-space corner point opposite the clipping plane
            // as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
            // transform it into camera space by multiplying it
            // by the inverse of the projection matrix

            ref Matrix matrix = ref projectionMatrix;

            Vector4 q;
            q.X = (Math.Sign(clipPlane.X) + matrix[8]) / matrix[0];
            q.Y = (Math.Sign(clipPlane.Y) + matrix[9]) / matrix[5];
            q.Z = -1.0F;
            q.W = (1.0F + matrix[10]) / matrix[14];

            // Calculate the scaled clipPlane vector
            Vector4 c = clipPlane * (1.0F / Vector4.Dot(clipPlane, q));

            // Replace the third row of the projection matrix
            matrix[2] = c.X;
            matrix[6] = c.Y;
            matrix[10] = c.Z;
            matrix[14] = c.W;

            //CreatePlanes();
            //CreateCorners();

            Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out viewProjectionMatrix);
            inverseViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);

            frustum.Matrix = viewProjectionMatrix;
        }



    }
}
