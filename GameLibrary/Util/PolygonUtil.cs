using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph.Common;

namespace GameLibrary.Util
{
    public class Polygon
    {
        public float Z;
        private Vector2[] vertices;

        public Polygon(int vertexCount)
        {
            vertices = new Vector2[vertexCount];
        }

        public Polygon(params Vector2[] vertices)
        {
            this.vertices = vertices;
        }
        
        public int Count
        {
            get { return vertices.Count(); }
        }

        public Vector2 this[int i]
        {
            get { return vertices[i]; }
            set { vertices[i] = value; }
        }
    }

    public class Point
    {
        public Point previous;
        public Point next;
        public Point sibling;
        public Point previousInbound;
        public Point nextInbound;
        public float t;
        public int dir;
        public Vector2 vertex;
        public Point(Vector2 vertex)
        {
            this.vertex = vertex;
        }
    }


    public class Intersection
    {
        public float t;
        public float u;
        public bool inbound;
        public Vector3 vertex;
    }

    public struct Intersection2
    {
        public float t;
        public float u;
        public bool inbound;
    }

    public static class PolygonUtil
    {
        /* def intersection(A, B, C, D):
            """
            This calculates the intersection point between two lines, AB and CD.
            The function is somewhat specialized for the purposes of the clipping
            visualization. The return value is None if the lines are parallel or a
            tuple of the form (t, u, inbound). inbound is a boolean value if
            we know that CD is a line that belongs to a polygon specified in CCW
            direction. The further optimization is that u and inbound are set to
            None if t is not within 0 and 1. All of the uses of this function
            in this application are only interested in line segments and this saves
            a couple of extra calculations.
            """
            b = B - A
            d_perp = (D - C).perp()
            denom = d_perp.dot(b)
            if denom != 0:
                c = C - A
                numer =  d_perp.dot(c)
                t = float(numer) / float(denom)
                if 0 <= t <= 1:
                    b_perp = b.perp()
                    numer = b_perp.dot(c)
                    u = float(numer) / float(denom)
                    return (t, u, denom < 0)
                else:
                    # the intersetion isn't within segment AB, so bail out
                    return (t, None, None)
            else:
                # the lines are parallel
                return None
        */
        public static Intersection2 intersect(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
        {
            Intersection2 intersection = new Intersection2();
            intersection.t = float.NaN;
            intersection.u = float.NaN;
            Vector2 b = B - A;
            Vector2 d_perp = new Vector2(-(D.Y - C.Y), D.X - C.X);
            float denom = Vector2.Dot(d_perp, b);
            if (denom != 0)
            {
                Vector2 c = C - A;
                float numer = Vector2.Dot(d_perp, c);
                intersection.t = numer / denom;
                if (intersection.t >= 0 && intersection.t <= 1)
                {
                    Vector2 b_perp = new Vector2(-b.Y, b.X);
                    numer = Vector2.Dot(b_perp, c);
                    intersection.u = numer / denom;
                    intersection.inbound = (denom < 0);
                    //return (t, u, denom < 0)
                }
                else
                {
                    // the intersection isn't within the segment AB, so bail out
                    //return (t, None, None)
                }
            }
            else
            {
                // the lines are parallel
                //return (None, None, None);
            }
            return intersection;
        }

        /* def insertIntersection(self,subject,clipping, A,B,C,D):
                """
                This is looking for the intersection point between the line segments
                AB and CD. We are using the parametric equation A-bt = C - du, where
                b is the vector (B-A) and d is the vector (D-C). We want to solve for
                t and u - if they are both between 0 and 1, we have a valid
                intersection point.

                For insertion, we assume that AB comes from the subject polygon and
                that CD comes from the clipping region. This means that the
                intersection point can be inserted directly between CD but that we need
                to use the t value to find a valid position between A and B.
                """
                self.controller.actionStack.append((self.controller.FIND_INTER,'Comparing the lines defined by the segments %s%s and %s%s' % (A.name, B.name, C.name, D.name), ('showLines', A, B, C, D)))
                intersection = Utilities.intersection(A,B,C,D)
                if intersection is None:
                    # the lines are parallel
                    self.controller.actionStack.append((self.controller.FIND_INTER,'%s%s and %s%s are parallel, so they are ignored' % (A.name, B.name, C.name, D.name), ('showLines', A, B, C, D)))
                    return

                (t,u,inbound) = intersection
                b = B - A
                intersection = A+b*t

                self.controller.actionStack.append((self.controller.FIND_INTER,'Calculating the intersection between %s%s and %s%s'% (A.name, B.name, C.name, D.name) , ('showLines', A, B, C, D), ('showPoint',intersection)))

                if u is None:
                    # we know that the point isn't within t
                    self.controller.actionStack.append((self.controller.FIND_INTER,'The intersection point is not contained within %s%s, so it is thrown out' % (A.name, B.name), ('showSegments', A, B, C, D), ('showPoint',intersection)))
                    return

                # the point is on line segment AB
                if 0 < u < 1:
                    # we have a valid intersection point
                    if t == 0:
                        newPoint = A
                        newPoint.setT(t, subject.name)
                    elif t==1:
                        newPoint = B
                        newPoint.setT(t, subject.name)
                    else:
                        newPoint = intersection
                        newPoint.setT(t, subject.name)
                        newPoint.inbound = inbound

                        # insert it into AB
                        prev = A
                        next = A.next[subject.name]
                        while next is not None and next.t[subject.name] < t:
                            prev = next
                            next = next.next[subject.name]

                        prev.setNext(newPoint, subject.name)
                        newPoint.setNext(next, subject.name)

                    newPoint.setT(u, clipping.name)
                    #insert into CD
                    prev = C
                    next = C.next[clipping.name]
                    while next is not None and next.t[clipping.name] < u:
                        prev = next
                        next = next.next[clipping.name]

                    prev.setNext(newPoint, clipping.name)
                    newPoint.setNext(next, clipping.name)

                    newPoint.name = str(self.currentIntersection)
                    self.currentIntersection += 1
                    self.controller.actionStack.append((self.controller.FIND_INTER,'The intersection point is contained within both line segments, so it is inserted into both polygons', ('showSegments', A, B, C, D), ('showPoint',newPoint,True)))
                elif (u == 0 or u == 1) and (0<t<1):
                    # we can ignore them if the point it an endpoint in both
                    # polygons - any following we do will just wrap around
                    if u == 0:
                        newPoint = C
                    else:
                        newPoint = D

                    newPoint.setT(t, subject.name)
                    newPoint.setT(u, clipping.name)
                    newPoint.inbound = inbound

                    # insert it into AB
                    prev = A
                    next = A.next[subject.name]
                    while next is not None and next.t[subject.name] < t:
                        prev = next
                        next = next.next[subject.name]

                    prev.setNext(newPoint, subject.name)
                    newPoint.setNext(next, subject.name)
                    newPoint.name = str(self.currentIntersection)
                    self.currentIntersection += 1
                    self.controller.actionStack.append((self.controller.FIND_INTER,'The intersection point is contained within both line segments, so it is inserted into both polygons', ('showSegments', A, B, C, D), ('showPoint',newPoint, True)))
                else:
                    self.controller.actionStack.append((self.controller.FIND_INTER,'The intersection point is not contained within %s%s, so it is thrown out'%(C.name, D.name), ('showSegments', A, B, C, D), ('showPoint',intersection)))
        */
        public static void insertIntersection(Point A, Point B, Point C, Point D, ref Point inbound)
        {
            Intersection2 intersection = intersect(A.vertex, B.vertex, C.vertex, D.vertex);
            if (float.IsNaN(intersection.u))
            {
                // we know that the point isn't within t
                // The intersection point is not contained within %s%s, so it is thrown out' % (A.name, B.name), ('showSegments', A, B, C, D), ('showPoint',intersection)))
                return;
            }

            // the point is on line segment AB
            if (intersection.u > 0 && intersection.u < 1)
            {
                // we have a valid intersection point
                Vector2 p = B.vertex - A.vertex;
                p = A.vertex + p * intersection.t;
                A.t = 0;
                B.t = 1;
                Point newPoint1;
                if (intersection.t == 0)
                {
                    newPoint1 = A;
                    A.t = intersection.t;
                }
                else if (intersection.t == 1)
                {
                    newPoint1 = B;
                    B.t = intersection.t;
                }
                else
                {
                    newPoint1 = new Point(p);
                    //newPoint.setT(t, subject.name)
                    newPoint1.t = intersection.t;
                    newPoint1.dir = intersection.inbound ? 1 : -1;

                    // insert it into AB
                    insertPointSorted(A, newPoint1);

                    if (newPoint1.dir < 0)
                    {
                        if (inbound == null)
                        {
                            inbound = newPoint1;
                        }
                        else
                        {
                            inbound.previousInbound = newPoint1;
                            newPoint1.nextInbound = inbound;
                            inbound = newPoint1;
                        }

                    }
                }
                C.t = 0;
                D.t = 1;
                Point newPoint2 = new Point(p);
                newPoint2.t = intersection.u;
                newPoint2.dir = intersection.inbound ? 1 : -1;

                newPoint1.sibling = newPoint2;
                newPoint2.sibling = newPoint1;

                // insert into CD
                insertPointSorted(C, newPoint2);
                // The intersection point is contained within both line segments, so it is inserted into both polygons', ('showSegments', A, B, C, D), ('showPoint',newPoint,True)))
            }
            else if ((intersection.u == 0 || intersection.u == 1) && (intersection.t > 0 && intersection.t < 1))
            {
                // we can ignore them if the point it an endpoint in both
                // polygons - any following we do will just wrap around
                Vector2 p = B.vertex - A.vertex;
                p = A.vertex + p * intersection.t;
                Point newPoint1;
                if (intersection.u == 0)
                {
                    newPoint1 = C;
                    newPoint1.t = intersection.u;
                }
                else
                {
                    newPoint1 = D;
                    newPoint1.t = intersection.u;
                }
                A.t = 0;
                B.t = 1;

                Point newPoint2 = new Point(p);
                newPoint2.t = intersection.t;
                newPoint2.dir = intersection.inbound ? 1 : -1;

                newPoint1.sibling = newPoint2;
                newPoint2.sibling = newPoint1;

                // insert it into AB
                insertPointSorted(A, newPoint2);

                if (newPoint2.dir < 0)
                {
                    if (inbound == null)
                    {
                        inbound = newPoint2;
                    }
                    else
                    {
                        inbound.previousInbound = newPoint2;
                        newPoint2.nextInbound = inbound;
                        inbound = newPoint2;
                    }

                }

                // The intersection point is contained within both line segments, so it is inserted into both polygons', ('showSegments', A, B, C, D), ('showPoint',newPoint, True)))
            }
            else
            {
                // The intersection point is not contained within %s%s, so it is thrown out'%(C.name, D.name), ('showSegments', A, B, C, D), ('showPoint',intersection)))
            }
            //return intersection;
        }

        /* def findIntersections(self, subject, clipping):
                #import pdb
                #pdb.set_trace()
                self.currentIntersection = 1
                A = subject.pointList
                while A is not None:
                    B = A.next[subject.name]
                    tmp = B
                    if B is None:
                        B = subject.pointList
                    C =  clipping.pointList
                    while C is not None:
                        D = C.next[clipping.name]
                        while D is not None and D.isIntersection(clipping.name):
                            # skip past intersections
                            D = D.next[clipping.name]
                        if D is not None:
                            self.insertIntersection(subject,clipping,A,B,C,D)
                        else:
                            self.insertIntersection(subject,clipping,A,B,C,clipping.pointList) # loopback on clipping
                        C = D
                    A = tmp
        */
        public static Point findIntersections(Point subject, Point clipping)
        {
            Point inbound = null;
            Point A = subject;
            while (A != null)
            {
                Point B = A.next;
                Point nextA = B;
                if (B == null)
                {
                    // wrap
                    B = subject;
                }
                Point C = clipping;
                while (C != null)
                {
                    Point D = C.next;
                    while (D != null && D.dir != 0)
                    {
                        // skip past intersections
                        D = D.next;
                    }
                    insertIntersection(A, B, C, D != null ? D : clipping, ref inbound);
                    C = D;
                }
                A = nextA;
            }
            return inbound;
        }


        //The main clipping function        
        /* def clip(self, p0, p1):
                """
              The main clipping function        
                """
      
                self.controller.actionStack.append((self.controller.FIND_INTER,'Step one is to identify all of the intersection points between the two polygons',))
                # find intersections
                self.findIntersections(p1,p0)
                self.controller.actionStack.append((self.controller.FIND_INTER,'The intersections have now all been identified', ('saveIntersections',)))
                polygons = []
                names = (p0.name, p1.name)
                # get next entering intersection
                entering = self.findEnteringIntersection(p1.pointList, p1.name, p0)
                while entering is not None:
                    self.controller.actionStack.append((self.controller.CLIP,"Point %s is an inbound intersection" % entering.name, ('showPoint', entering)))
                    newPoly = CVPolygon(Color.green)
                    current = entering
                    currentPoly = 1
                    while not current.visited:
                        self.controller.actionStack.append((self.controller.CLIP,"Save %s and mark it as visited" % current.name, ('showPoint', current, True)))
                        newPoly.addPoint(current.x, current.y, current.name)
         
                        current.visited = True

                        if current.isIntersection(names[currentPoly]) and current != entering:
                            currentPoly = (currentPoly + 1) % 2
                            self.controller.actionStack.append((self.controller.CLIP,"Point %s is an intersection, so switch polygon lists, now following polygon %s" % (current.name,names[currentPoly]),('switchPolygon',names[currentPoly])))
              
                        current = current.next[names[currentPoly]]
                        if current is None:
                            # time to wrap
                            if currentPoly == 0:
                                current = p0.pointList
                            else:
                                current = p1.pointList
                    self.controller.actionStack.append((self.controller.CLIP,"Point %s has already been visited, save this list of points as a new polygon" % (current.name), ('showPoint', current, True), ('savePolygon', newPoly)))
                    self.controller.actionStack.append((self.controller.CLIP,"Switch back to SUBJECT polygon to look for next intersection",('showPoint', current), ('switchPolygon','SUBJECT') ))
                    polygons.append(newPoly)
            
                    entering = self.findEnteringIntersection(current.next[p1.name], p1.name, p0)

                if len(polygons) == 0:
                    # we  found no polygons - possibly one is contained in the other
                    # first check to see if our clipping region contains the other one
                    # theoretically, any point will do, except that all of the end
                    # points might intersect the boundary of the shape - and thus fail
                    # the containment test. So, we pick a point that is halfway along
                    # the first edge and one unit vector in the direction opposite the
                    # perp vector
                    self.controller.actionStack.append((self.controller.CLIP,"No inbound intersections found - checking if one polygon contains the other",))
                    A = p1.pointList
                    B = p1.pointList.next[p1.name]
                    b = B - A
                    b_perp = b.perp()
                    b_perp.normalize()
                    x = .5 * (A.x + B.x) - b_perp.x
                    y = .5 * (A.y + B.y) - b_perp.y
           
                    if p0.contains(x,y):
                        # make a copy of poly1
                        newPoly = CVPolygon(c=Color.green, name='SUBJECT')
                        points = p1.pointList
                        for p in points:
                            newPoly.addPoint(p.x,p.y, p.name)
                        self.controller.actionStack.append((self.controller.CLIP,"The SUBJECT polygon is contained completely inside the CLIP polygon, so it is returned", ('savePolygon', newPoly)))
                        polygons.append(newPoly)
                    # now check to see if the clipping region is enclosed by the other
                    else:
                        A = p0.pointList
                        B = p0.pointList.next[p0.name]
                        b = B - A
                        b_perp = b.perp()
                        b_perp.normalize()
                        x = .5 * (A.x + B.x) - b_perp.x
                        y = .5 * (A.y + B.y) - b_perp.y
               
                        if p1.contains(x,y):
                            # make a copy of poly0
                            newPoly = CVPolygon(c=Color.green, name='CLIP')
                            points = p0.pointList
                            for p in points:
                                newPoly.addPoint(p.x,p.y, p.name)
                            self.controller.actionStack.append((self.controller.CLIP,"The CLIP polygon is contained completely inside the SUBJECT polygon, so it is returned", ('savePolygon', newPoly)))
                            polygons.append(newPoly)
                        else:
                            self.controller.actionStack.append((self.controller.CLIP,"The polygons are disjoint - no polygons returned",))
                self.controller.actionStack.append((self.controller.CLIP,"There are no more unvisited inbound intersections", ("finish",)))
                return polygons
         */
        public static void clip(MeshNode subjectMeshNode, MeshNode clippingMeshNode, LinkedList<Intersection> list)
        {
            // Step one is to identify all of the intersection points between the two polygons
            // find intersections
            Polygon subjectPoly = createPolygon(subjectMeshNode);
            Polygon clippingPoly = createPolygon(clippingMeshNode);

            Point subject = createPointList(subjectPoly);
            Point clipping = createPointList(clippingPoly);

            Point inbound = findIntersections(subject, clipping);
            // The intersections have now all been identified', ('saveIntersections',)))


            // get next entering intersection
            LinkedList<Point> intersection = new LinkedList<Point>();
            bool inSubject = true;
            while (inbound != null)
            {
                intersection.AddLast(inbound);
                Point current = inbound;

                while (current.next != inbound)
                {
                    if (current.dir != 0)
                    {
                        // switch
                        inSubject = !inSubject;
                        current = current.sibling;
                        if (current.dir < 0)
                        {
                            // remove inbound from inbound list ("mark" as visited)
                            if (current.previousInbound != null)
                            {
                                current.previousInbound.nextInbound = current.nextInbound;
                            }
                            if (current.nextInbound != null)
                            {
                                current.nextInbound.previousInbound = current.previousInbound;
                            }
                        }
                        // Point %s is an intersection, so switch polygon lists, now following polygon %s" % (current.name,names[currentPoly]),('switchPolygon',names[currentPoly])))
                    }
                    current = current.next;
                    if (current == null)
                    {
                        // time to wrap
                        if (inSubject)
                        {
                            current = subject;
                        }
                        else
                        {
                            current = clipping;
                        }
                    }
                    //Point prev = intersection;
                    //intersection = current;
                    //if (prev != null)
                    //{
                    //    intersection.nextInbound = prev;
                    //    prev.previousInbound = intersection;
                    //}
                    intersection.AddLast(current);
                }

                LinkedListNode<Point> it = intersection.First;
                while (it != null)
                {
                    Intersection i = new Intersection();
                    i.vertex = new Vector3(it.Value.vertex, subjectPoly.Z);
                    list.AddLast(i);
                    it = it.Next;
                }
                //"Point %s has already been visited, save this list of points as a new polygon" % (current.name), ('showPoint', current, True), ('savePolygon', newPoly)))
                //self.controller.actionStack.append((self.controller.CLIP,"Switch back to SUBJECT polygon to look for next intersection",('showPoint', current), ('switchPolygon','SUBJECT') ))
                //polygons.append(newPoly);

                inbound = inbound.nextInbound;
                //entering = self.findEnteringIntersection(current.next[p1.name], p1.name, p0)
            }
        }

        private static Point createPointList(Polygon poly)
        {
            Point next = null;
            for (int i = poly.Count - 1; i >= 0; i--)
            {
                Point prev = next;
                next = new Point(poly[i]);
                if (prev != null)
                {
                    next.next = prev;
                    prev.previous = next;
                }
            }
            return next;
        }

        private static Polygon createPolygon(MeshNode meshNode)
        {
            Mesh mesh = meshNode.Mesh;
            VertexPositionColor[] vertexBuffer = new VertexPositionColor[mesh.PrimitiveCount];
            mesh.VertexBuffer.GetData(vertexBuffer);
            Polygon poly = new Polygon(vertexBuffer.Count());
            Matrix worldMatrix = meshNode.WorldTransform;
            Vector3 tmpV;
            float Z = float.NaN;
            for (int i = 0; i < vertexBuffer.Count(); i++)
            {
                Vector3 v = vertexBuffer[i].Position;
                Vector3.Transform(ref v, ref worldMatrix, out tmpV);
                poly[i] = new Vector2(tmpV.X, tmpV.Y);
                if (float.IsNaN(Z))
                {
                    Z = tmpV.Z;
                }
                else
                {
                    // check that all points are in same plane
                }
            }
            //poly.Z = Z;
            return poly;
        }



        private static void insertPointSorted(Point point, Point newPoint)
        {
            Point prev = point;
            Point next = point.next;
            while (next != null && next.t < newPoint.t)
            {
                prev = next;
                next = next.next;
            }
            prev.next = newPoint;
            newPoint.next = next;
        }

        private static Point addFirst(Point point, Point newPoint)
        {
            if (point != null)
            {
                newPoint.next = point;
                point.previous = newPoint;
            }
            return newPoint;
        }
    }
}
