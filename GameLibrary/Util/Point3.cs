using System;
using System.Diagnostics;
using System.Text;

namespace GameLibrary.Util
{
    // See http://hhoppe.com/perfecthash.pdf
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public struct Point3 : IEquatable<Point3>
    {
        //public static readonly IEqualityComparer<Point3> KeyEqualityComparerInstance = new KeyEqualityComparer();

        public int X;
        public int Y;
        public int Z;

        public Point3(int X, int Y, int Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point3))
                return false;

            var other = (Point3)obj;
            return ((X == other.X) && (Y == other.Y) && (Z == other.Z));
        }

        public bool Equals(Point3 other)
        {
            return ((X == other.X) && (Y == other.Y) && (Z == other.Z));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 31) ^ Y.GetHashCode();
                hashCode = (hashCode * 127) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        public static Point3 Add(Point3 value1, Point3 value2)
        {
            value1.X += value2.X;
            value1.Y += value2.Y;
            value1.Z += value2.Z;
            return value1;
        }

        public static void Add(ref Point3 value1, ref Point3 value2, out Point3 result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
            result.Z = value1.Z + value2.Z;
        }

        public static Point3 Subtract(Point3 value1, Point3 value2)
        {
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            value1.Z -= value2.Z;
            return value1;
        }

        public static void Subtract(ref Point3 value1, ref Point3 value2, out Point3 result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            result.Z = value1.Z - value2.Z;
        }

        public int ManhatanDistance()
        {
            return Math.Max(Math.Max(Math.Abs(X), Math.Abs(Y)), Math.Abs(Z));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("{X:");
            sb.Append(this.X);
            sb.Append(" Y:");
            sb.Append(this.Y);
            sb.Append(" Z:");
            sb.Append(this.Z);
            sb.Append("}");
            return sb.ToString();
        }

        internal string DebugDisplayString
        {
            get
            {
                return string.Concat(
                    this.X.ToString(), "  ",
                    this.Y.ToString(), "  ",
                    this.Z.ToString()
                );
            }
        }

        public static bool operator ==(Point3 value1, Point3 value2)
        {
            return value1.X == value2.X
                && value1.Y == value2.Y
                && value1.Z == value2.Z;
        }

        public static bool operator !=(Point3 value1, Point3 value2)
        {
            return !(value1 == value2);
        }


        public static Point3 operator +(Point3 value1, Point3 value2)
        {
            value1.X += value2.X;
            value1.Y += value2.Y;
            value1.Z += value2.Z;
            return value1;
        }
      
        public static Point3 operator -(Point3 value)
        {
            value = new Point3(-value.X, -value.Y, -value.Z);
            return value;
        }
    
        public static Point3 operator -(Point3 value1, Point3 value2)
        {
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            value1.Z -= value2.Z;
            return value1;
        }

        /*private sealed class KeyEqualityComparer : IEqualityComparer<Point3>
        {
            public bool Equals(Point3 key1, Point3 key2)
            {
                return ((key1.X == key2.X) && (key1.Y == key2.Y) && (key1.Z == key2.Z));
            }

            public int GetHashCode(Point3 key)
            {
                int hash = key.X;
                hash = hash * 31 + key.Y;
                hash = hash * 31 + key.Z;
                return hash;
            }
        }*/
    }

}
