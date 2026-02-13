using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace ArchPlanningAddon.Core
{
    public static class MassingGenerator
    {
        public static List<Brep> GenerateSimpleExtrusion(Site site, Regulations regulations, double floorHeight, Vector3d northVector, out string debugMsg)
        {
            debugMsg = "";
            var results = new List<Brep>();

            // Normalizing North Vector
            if (!northVector.IsValid || northVector.Length < 0.001) northVector = Vector3d.YAxis;
            northVector.Unitize();

            if (site == null || site.Boundary == null)
            {
                debugMsg = "Invalid Site";
                return results;
            }

            // 1. Calculate Allowable Building Area (Footprint)
            double siteArea = site.Area;
            double manualMaxBuildingArea = siteArea * (regulations.MaxBCR / 100.0);

            // 2. Offset Site Boundary for Setback (Simulated simple setback)
            // For MVP, we just use the site boundary or a slight offset if needed.
            // Let's assume the input curve is already the buildable area or we use full site.
            // If we want to be realistic, we might offset -1.0m
            
            // Simple Logic: Extrude until FAR is met.
            double targetTotalFloorArea = siteArea * (regulations.MaxFAR / 100.0);

            // 3. Create Footprint
            Curve footprintCurve = site.Boundary.DuplicateCurve();
            double footprintArea = site.Area;

            // Apply BCR constraint: Scale down if needed
            if (footprintArea > manualMaxBuildingArea)
            {
                debugMsg += string.Format("[Info] Scaling down footprint to meet Max BCR ({0}%).\n", regulations.MaxBCR);
                
                // Calculate scale factor (Area scales by square of linear factor)
                double scaleFactor = Math.Sqrt(manualMaxBuildingArea / footprintArea);
                
                // Find centroid for scaling center
                var amp = AreaMassProperties.Compute(footprintCurve);
                Point3d center = amp != null ? amp.Centroid : footprintCurve.GetBoundingBox(true).Center;

                // Scale
                footprintCurve.Transform(Transform.Scale(center, scaleFactor));
                
                // Update footprint area
                var newAmp = AreaMassProperties.Compute(footprintCurve);
                footprintArea = newAmp != null ? newAmp.Area : 0;
            }

            // Calculate number of floors needed
            int floors = (int)Math.Ceiling(targetTotalFloorArea / footprintArea);
            
            // Check Height Limit (Meters)
            double totalHeight = floors * floorHeight;
            if (regulations.MaxHeight > 0 && totalHeight > regulations.MaxHeight)
            {
                // Cap height
                int maxFloorsByHeight = (int)Math.Floor(regulations.MaxHeight / floorHeight);
                if (maxFloorsByHeight < floors)
                {
                    floors = maxFloorsByHeight;
                    debugMsg += string.Format("[Constraint] Max Height ({0}m) reached. Capped at {1} floors.\n", regulations.MaxHeight, floors);
                }
            }

            // Check Floor Count Limit
            if (regulations.MaxFloors > 0 && floors > regulations.MaxFloors)
            {
                floors = regulations.MaxFloors;
                debugMsg += string.Format("[Constraint] Max Floors ({0}) reached. Capped at {0} floors.\n", floors);
            }

            // Create Breps
            double accumulatedArea = 0;
            
            for (int i = 0; i < floors; i++)
            {
                double elevation = i * floorHeight;
                
                // Determine if this is the last floor and needs trimming (if floor count was result of ceiling)
                // Actually, floors = Ceiling(Target/Footprint).
                // So TotalArea of 'floors' > Target.
                // We should check if we are on the last floor, and scale it if user wants 'Exact Volume'.
                // Yes, user asked "Calculate volume exactly".
                
                Curve currentFootprint = footprintCurve.DuplicateCurve();
                
                // If it is the last floor, check if we need to shrink it
                if (i == floors - 1)
                {
                    double remainingTarget = targetTotalFloorArea - accumulatedArea;
                    if (remainingTarget < footprintArea - 0.1 && remainingTarget > 0)
                    {
                        // Scale down last floor
                        double scaleFactor = Math.Sqrt(remainingTarget / footprintArea);
                        var amp = AreaMassProperties.Compute(currentFootprint);
                        Point3d center = amp != null ? amp.Centroid : currentFootprint.GetBoundingBox(true).Center;
                        currentFootprint.Transform(Transform.Scale(center, scaleFactor));
                        
                        debugMsg += string.Format("[Info] Penthouse generated. Area: {0:F1}\n", remainingTarget);
                    }
                }

                // Base curve at elevation
                currentFootprint.Translate(new Vector3d(0, 0, elevation));

                // *** Solar Check (Iljosaseon) ***
                if (regulations.ApplySolarCheck)
                {
                    double currentCeilingHeight = (i + 1) * floorHeight;
                    double setbackDist = 0;
                    if (currentCeilingHeight <= 9.0)
                    {
                        setbackDist = 1.5;
                    }
                    else
                    {
                        setbackDist = currentCeilingHeight / 2.0;
                    }

                    // Shift the SITE boundary South (Opposite of North)
                    Vector3d shiftVector = -northVector * setbackDist;
                    Curve limitCurve = site.Boundary.DuplicateCurve(); 
                    limitCurve.Translate(shiftVector);
                    
                    // Project to XY for Intersection
                    // currentFootprint is already at elevation. Project down to XY.
                    Curve footprintProj = currentFootprint.DuplicateCurve();
                    footprintProj.Translate(new Vector3d(0, 0, -elevation)); 
                    if (!footprintProj.IsPlanar()) footprintProj = Curve.ProjectToPlane(footprintProj, Plane.WorldXY);

                    if (!limitCurve.IsPlanar()) limitCurve = Curve.ProjectToPlane(limitCurve, Plane.WorldXY);

                    // Intersection
                    Curve[] intersection = Curve.CreateBooleanIntersection(footprintProj, limitCurve);
                    
                    if (intersection != null && intersection.Length > 0)
                    {
                        // Assume largest loop
                        Array.Sort(intersection, (a, b) => 
                        {
                            var ampA = AreaMassProperties.Compute(a);
                            var ampB = AreaMassProperties.Compute(b);
                            if (ampA == null || ampB == null) return 0;
                            return ampB.Area.CompareTo(ampA.Area);
                        });
                        
                        currentFootprint = intersection[0];
                        currentFootprint.Translate(new Vector3d(0, 0, elevation)); // Move back up to current elevation
                    }
                    else
                    {
                         debugMsg += string.Format("[Solar] Floor {0} cut completely.\n", i + 1);
                         continue;
                    }
                }
                
                // Extrude
                Extrusion extrusion = Extrusion.Create(currentFootprint, floorHeight, true);
                if (extrusion != null)
                {
                    results.Add(extrusion.ToBrep());
                    
                    var amp = AreaMassProperties.Compute(currentFootprint);
                    if (amp != null) accumulatedArea += amp.Area;
                }
            }

            debugMsg += string.Format("Generated {0} floors. Total Area: {1:F1} / {2:F1} (Target)", floors, accumulatedArea, targetTotalFloorArea);

            return results;
        }
    }
}
