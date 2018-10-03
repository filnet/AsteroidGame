﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLibrary.Geometry.Common;

namespace GameLibrary.Geometry
{
    public class GeometryUtil
    {
        // grid

        public static MeshNode CreateLine(String name)
        {
            return new MeshNode(name, new LineMeshFactory());
        }

        // grid

        public static MeshNode CreateGrid(String name, int count)
        {
            return new MeshNode(name, new GridMeshFactory(count));
        }

        // circle

        public static MeshNode CreateCircle(String name, int count)
        {
            return new MeshNode(name, new CircleMeshFactory(count));
        }

        // cube

        // sphere

        public static MeshNode CreateSphere(String name, int depth)
        {
            return new MeshNode(name, new GeodesicMeshFactory(depth, false));
        }

        // geodesic

        public static MeshNode CreateGeodesic(String name, int depth)
        {
            return CreateGeodesic(name, depth, true, false);
        }

        public static MeshNode CreateGeodesic(String name, int depth, Boolean facetted, Boolean flat)
        {
            return new MeshNode(name, new GeodesicMeshFactory(depth, facetted, flat));
        }

        public static MeshNode CreateGeodesicWF(String name, int depth)
        {
            return CreateGeodesicWF(name, depth, false);
        }

        public static MeshNode CreateGeodesicWF(String name, int depth, Boolean flat)
        {
            return new MeshNode(name, new GeodesicWFMeshFactory(depth, flat));
        }

    }
}