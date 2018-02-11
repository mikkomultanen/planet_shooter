﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using ClipperLibPolygon = System.Collections.Generic.List<ClipperLib.IntPoint>;
using ClipperLibPolygons = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using Delaunay;

public static class ClipperHelper {
    private static float multiplier = 1000;

    public static List<List<Vector2>> clip(List<Vector2> boundary, Triangle piece)
    {
        //create Boundary ClipperLibPolygon
        ClipperLibPolygons boundaryPoly = createPolygons(boundary);

        //create ClipperLibPolygon from the triangular piece
        ClipperLibPolygons subjPoly = createPolygons(piece);

        //clip triangular polygon against the boundary polygon
        ClipperLibPolygons result = new ClipperLibPolygons();
        Clipper c = new Clipper();
        c.AddPaths(subjPoly, PolyType.ptClip, true);
        c.AddPaths(boundaryPoly, PolyType.ptSubject, true);
        c.Execute(ClipType.ctIntersection, result, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

        List<List<Vector2>> clippedPolygons = new List<List<Vector2>>();

        foreach (ClipperLibPolygon poly in result)
        {
            List<Vector2> clippedPoly = new List<Vector2>();
            foreach (IntPoint p in poly)
            {
                clippedPoly.Add(new Vector2(p.X, p.Y) / multiplier);
            }
            clippedPolygons.Add(clippedPoly);

        }
        return clippedPolygons;
        
    }
    public static List<List<Vector2>> clip(List<Vector2> boundary, List<Vector2> region)
    {
        ClipperLibPolygons boundaryPoly = createPolygons(boundary);
        ClipperLibPolygons regionPoly = createPolygons(region);

        //clip triangular polygon against the boundary polygon
        ClipperLibPolygons result = new ClipperLibPolygons();
        Clipper c = new Clipper();
        c.AddPaths(regionPoly, PolyType.ptClip, true);
        c.AddPaths(boundaryPoly, PolyType.ptSubject, true);
        c.Execute(ClipType.ctIntersection, result, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

        List<List<Vector2>> clippedPolygons = new List<List<Vector2>>();

        foreach (ClipperLibPolygon poly in result)
        {
            List<Vector2> clippedPoly = new List<Vector2>();
            foreach (IntPoint p in poly)
            {
                clippedPoly.Add(new Vector2(p.X, p.Y) / multiplier);
            }
            clippedPolygons.Add(clippedPoly);

        }
        return clippedPolygons;
    }

    private static ClipperLibPolygons createPolygons(List<Vector2> source)
    {
        ClipperLibPolygons poly = new ClipperLibPolygons(1);
        poly.Add(new ClipperLibPolygon(source.Count));
        foreach (Vector2 p in source)
        {
            poly[0].Add(new IntPoint(p.x * multiplier, p.y * multiplier));
        }

        return poly;
    }
    private static ClipperLibPolygons createPolygons(Triangle tri)
    {
        ClipperLibPolygons poly = new ClipperLibPolygons(1);
        poly.Add(new ClipperLibPolygon(3));
        for (int i = 0; i < 3; i++)
        {
            poly[0].Add(new IntPoint(tri.sites[i].x * multiplier, tri.sites[i].y * multiplier));
        }

        return poly;
    }
   
}
