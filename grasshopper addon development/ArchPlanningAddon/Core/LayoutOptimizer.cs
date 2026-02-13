using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace ArchPlanningAddon.Core
{
    public class OptimizationResult
    {
        public List<Brep> Massing { get; set; }
        public double TotalArea { get; set; }
        public string Report { get; set; }

        public OptimizationResult()
        {
            Massing = new List<Brep>();
        }
    }

    public static class LayoutOptimizer
    {
        public static OptimizationResult Optimize(Site site, List<BuildingProgram> programs, Regulations regulations, int iterations)
        {
            OptimizationResult bestResult = new OptimizationResult();
            double bestScore = -1.0;

            Random rnd = new Random();

            // 1. Separate programs
            var podiums = programs.Where(p => p.Stacking == StackingType.Podium).ToList();
            var towers = programs.Where(p => p.Stacking == StackingType.Tower).ToList();
            var independent = programs.Where(p => p.Stacking == StackingType.Independent).ToList();

            for (int i = 0; i < iterations; i++)
            {
                List<Brep> currentMassing = new List<Brep>();
                List<Curve> placedFootprints = new List<Curve>();
                double currentTotalArea = 0;
                string currentReport = "";

                // --- Step A: Place Podiums & Independent ---
                List<BuildingProgram> groundLayer = new List<BuildingProgram>();
                groundLayer.AddRange(podiums);
                groundLayer.AddRange(independent);
                
                // Shuffle
                groundLayer = groundLayer.OrderBy(x => rnd.Next()).ToList();

                Dictionary<BuildingProgram, Brep> podiumMasses = new Dictionary<BuildingProgram, Brep>();
                Dictionary<BuildingProgram, Curve> podiumCurves = new Dictionary<BuildingProgram, Curve>();

                foreach (var prog in groundLayer)
                {
                    // Try N times to place
                    bool placed = false;
                    for (int tryP = 0; tryP < 20; tryP++)
                    {
                        // 1. Determine Footprint Area based on PreferredFloors
                        // If Podium (2 floors), Area = Total / 2.
                        int preferredFloors = prog.PreferredFloors > 0 ? prog.PreferredFloors : (prog.Stacking == StackingType.Podium ? 2 : 20);
                        double targetFpArea = prog.TargetTotalArea / (double)preferredFloors;
                        
                        // Random Aspect Ratio
                        double ratio = 0.5 + rnd.NextDouble() * 1.5;

                        Curve footprint = ShapeGenerator.GenerateFootprint(prog.AllowedShape, targetFpArea, ratio);
                        
                        // Random Rotation
                        double angle = rnd.Next(0, 4) * Math.PI / 2.0; 
                        footprint.Rotate(angle, Vector3d.ZAxis, Point3d.Origin);

                        // Random Position on Site
                        BoundingBox siteBox = site.Boundary.GetBoundingBox(true);
                        // Margin for footprint size
                        double tX = siteBox.Min.X + rnd.NextDouble() * (siteBox.Max.X - siteBox.Min.X);
                        double tY = siteBox.Min.Y + rnd.NextDouble() * (siteBox.Max.Y - siteBox.Min.Y);
                        footprint.Translate(new Vector3d(tX, tY, 0));

                        // Check Boundary
                        if (!IsFullyInside(footprint, site.Boundary)) continue;

                        // Check Collision with others
                        if (IsColliding(footprint, placedFootprints)) continue;

                        // Valid! Generate Mass
                        // Height logic
                        var amp = AreaMassProperties.Compute(footprint);
                        double fpArea = amp.Area;
                        // preferredFloors already declared above
                        int floors = preferredFloors; 
                        double height = floors * 4.0; // 4m floor-to-floor typical for commercial/podium

                        Extrusion ext = Extrusion.Create(footprint, height, true);
                        Brep mass = ext.ToBrep();
                        
                        // Add to list
                        currentMassing.Add(mass);
                        placedFootprints.Add(footprint);
                        if(prog.Stacking == StackingType.Podium)
                        {
                            podiumMasses[prog] = mass;
                            podiumCurves[prog] = footprint;
                        }
                        
                        placed = true;
                        currentTotalArea += fpArea * floors;
                        break;
                    }
                }

                // --- Step B: Place Towers on Podiums ---
                foreach (var tower in towers)
                {
                     // Find a random podium
                     if (podiumCurves.Count == 0) continue;
                     var podiumProg = podiumCurves.Keys.ToList()[rnd.Next(podiumCurves.Count)];
                     Curve podiumCrv = podiumCurves[podiumProg];
                     Brep podiumMass = podiumMasses[podiumProg];
                     
                     BoundingBox podBox = podiumCrv.GetBoundingBox(true);
                     Point3d podCenter = podBox.Center;
                     
                     // Get Preferred Height/Area data
                     int tFloors = tower.PreferredFloors > 0 ? tower.PreferredFloors : 20;
                     double tTargetFpArea = tower.TargetTotalArea / (double)tFloors;

                     bool placed = false;
                     for(int tryT = 0; tryT < 50; tryT++) // Resume logic
                     {
                         double ratio = 0.5 + rnd.NextDouble() * 1.5;
                         Curve towerFootprint = ShapeGenerator.GenerateFootprint(tower.AllowedShape, tTargetFpArea, ratio);
                         
                         // Rotate
                         double angle = rnd.Next(0, 4) * Math.PI / 2.0;
                         towerFootprint.Rotate(angle, Vector3d.ZAxis, Point3d.Origin);
                         
                         // Position Strategy:
                         // 0-4: Center
                         // 5-9: Corners
                         // 10+: Random
                         Vector3d move = Vector3d.Zero;
                         if (tryT < 5)
                         {
                             // Try Center
                             move = new Vector3d(podCenter.X, podCenter.Y, 0);
                         }
                         else
                         {
                             // Random Jitter inside box
                             double jX = (rnd.NextDouble() - 0.5) * (podBox.Max.X - podBox.Min.X) * 0.8; // Stay within 80%
                             double jY = (rnd.NextDouble() - 0.5) * (podBox.Max.Y - podBox.Min.Y) * 0.8;
                             move = new Vector3d(podCenter.X + jX, podCenter.Y + jY, 0);
                         }
                         
                         towerFootprint.Translate(move);
                         
                         // Check if inside Podium
                         // Relaxed Check: We check if Curve Center is inside, or if Curve passes "CurveInCurve"
                         if (!IsFullyInside(towerFootprint, podiumCrv)) continue;
                         
                         // Check Collision with others
                         // IMPORTANT: We must NOT collide with other Towers or other Podiums.
                         // BUT we ARE inside our own Parent Podium, so we must exclude Parent Podium from the collision check.
                         // Filter out the parent podium curve from the obstacle list.
                         var obstacles = placedFootprints.Where(x => x != podiumCrv).ToList();
                         
                         if (IsColliding(towerFootprint, obstacles)) continue; 
                         
                         // Move to Top of Podium
                         double podHeight = podiumMass.GetBoundingBox(true).Max.Z; 
                         towerFootprint.Translate(new Vector3d(0,0, podHeight));
                         
                         // Generate Tower Mass
                         double height = tFloors * 3.5;
                         Extrusion tExt = Extrusion.Create(towerFootprint, height, true);
                         Brep towerBrep = tExt.ToBrep();
                         
                         currentMassing.Add(towerBrep);
                         
                         // Track Projected
                         Curve projFootprint = towerFootprint.DuplicateCurve();
                         projFootprint.Translate(new Vector3d(0,0, -podHeight));
                         placedFootprints.Add(projFootprint);
                         
                         placed = true;
                         currentTotalArea += AreaMassProperties.Compute(towerFootprint).Area * tFloors;
                         break;
                     }
                }

                // Eval Score
                if (currentTotalArea > bestScore)
                {
                    bestScore = currentTotalArea;
                    bestResult.Massing = currentMassing;
                    bestResult.TotalArea = currentTotalArea;
                    bestResult.Report = "Iteration " + i + ": Area " + currentTotalArea;
                }
            }

            return bestResult;
        }

        // Helper: Check inclusion
        private static bool IsFullyInside(Curve inner, Curve outer)
        {
            // Simple check: Check points
            Polyline poly;
            if (inner.TryGetPolyline(out poly))
            {
                foreach(var pt in poly)
                {
                    if (outer.Contains(pt, Plane.WorldXY, 0.01) == PointContainment.Outside) return false;
                }
                return true;
            }
            return false;
        }

        // Helper: Check collision
        private static bool IsColliding(Curve A, List<Curve> others)
        {
           foreach(var B in others)
           {
               var events = Intersection.CurveCurve(A, B, 0.01, 0.01);
               if (events != null && events.Count > 0) return true;
               
               // Also check containment (one inside another)
               if (IsFullyInside(A, B) || IsFullyInside(B, A)) return true;
           }
           return false;
        }
        
        // Helper extension
        private static double Preferences(this BuildingProgram p, BuildingProgram self) { return self.PreferredWidth / self.PreferredDepth; }
    }
}
