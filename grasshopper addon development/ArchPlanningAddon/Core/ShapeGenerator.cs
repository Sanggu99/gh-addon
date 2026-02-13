using System;
using Rhino.Geometry;

namespace ArchPlanningAddon.Core
{
    public static class ShapeGenerator
    {
        /// <summary>
        /// Generates a footprint curve based on area, shape type, and approximate dimensions.
        /// Returns a curve centered at (0,0,0) in XY plane.
        /// </summary>
        public static Curve GenerateFootprint(BuildingShape shape, double targetFloorArea, double widthRatio = 1.0)
        {
            // Simple heuristics to determine dimensions from Area
            switch (shape)
            {
                case BuildingShape.U_Shape:
                    return GenerateUShape(targetFloorArea, widthRatio);
                case BuildingShape.L_Shape:
                    return GenerateLShape(targetFloorArea, widthRatio);
                case BuildingShape.Rectangle:
                default:
                    return GenerateRectangle(targetFloorArea, widthRatio);
            }
        }

        private static Curve GenerateRectangle(double area, double ratio)
        {
            // Area = w * d
            // ratio = w / d  => w = d * ratio
            // Area = d * ratio * d = d^2 * ratio
            // d = Sqrt(Area / ratio)
            
            double d = Math.Sqrt(area / ratio);
            double w = d * ratio;

            return new Rectangle3d(Plane.WorldXY, new Interval(-w/2, w/2), new Interval(-d/2, d/2)).ToNurbsCurve();
        }

        private static Curve GenerateLShape(double area, double ratio)
        {
            // Assume "thick" L-shape. 
            // Let's say Thickness is 1/3 of Width/Depth approximately.
            // Simplified: Bounding Box is W x D.
            // Full Rect Area = W * D
            // Missing Corner Area = (W-t)*(D-t)
            // Actual Area = W*D - (W-t)(D-t)
            
            // Heuristic: Start with a bounding box similar to Rectangle logic, but slightly larger to account for void.
            double d_box = Math.Sqrt(area / ratio) * 1.2; 
            double w_box = d_box * ratio;
            
            double thickness = Math.Min(w_box, d_box) * 0.4; // 40% thickness

            // Points
            // 0: (-w/2, -d/2) SW
            // 1: (w/2, -d/2) SE
            // 2: (w/2, -d/2 + t)
            // 3: (-w/2 + t, -d/2 + t) -> Inner Corner
            // 4: (-w/2 + t, d/2)
            // 5: (-w/2, d/2) NW
            
            Point3d[] corners = new Point3d[7];
            corners[0] = new Point3d(-w_box/2, -d_box/2, 0);
            corners[1] = new Point3d(w_box/2, -d_box/2, 0);
            corners[2] = new Point3d(w_box/2, -d_box/2 + thickness, 0);
            corners[3] = new Point3d(-w_box/2 + thickness, -d_box/2 + thickness, 0);
            corners[4] = new Point3d(-w_box/2 + thickness, d_box/2, 0);
            corners[5] = new Point3d(-w_box/2, d_box/2, 0);
            corners[6] = corners[0]; // Close

            return new Polyline(corners).ToNurbsCurve();
        }

        private static Curve GenerateUShape(double area, double ratio)
        {
            // U-shape bounding box
            double d_box = Math.Sqrt(area / ratio) * 1.3; 
            double w_box = d_box * ratio;
            
            double thickness = Math.Min(w_box, d_box) * 0.35; 

            // U shape facing South (opening is South) or North?
            // Let's standard make it C shape opening East for now, or U opening North.
            // Let's do U opening "UP" (Positive Y, but hole is there). No, U shape usually means solid U.
            // Let's make it standard U opening towards +Y (Void is at +Y side).
            
            // Points
            // SW: (-w/2, -d/2)
            // SE: (w/2, -d/2)
            // NE_Out: (w/2, d/2)
            // NE_In:  (w/2 - t, d/2)
            // Inner_Right: (w/2 - t, -d/2 + t)
            // Inner_Left:  (-w/2 + t, -d/2 + t)
            // NW_In: (-w/2 + t, d/2)
            // NW_Out: (-w/2, d/2)

            Point3d[] pts = new Point3d[9];
            pts[0] = new Point3d(-w_box/2, -d_box/2, 0);
            pts[1] = new Point3d(w_box/2, -d_box/2, 0);
            pts[2] = new Point3d(w_box/2, d_box/2, 0);
            pts[3] = new Point3d(w_box/2 - thickness, d_box/2, 0);
            pts[4] = new Point3d(w_box/2 - thickness, -d_box/2 + thickness, 0);
            pts[5] = new Point3d(-w_box/2 + thickness, -d_box/2 + thickness, 0);
            pts[6] = new Point3d(-w_box/2 + thickness, d_box/2, 0);
            pts[7] = new Point3d(-w_box/2, d_box/2, 0);
            pts[8] = pts[0];

            return new Polyline(pts).ToNurbsCurve();
        }
    }
}
