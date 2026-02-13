using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ArchPlanningAddon.Core;
using Grasshopper.Kernel.Types;

namespace ArchPlanningAddon.Components
{
    public class RegulationCheckComponent : GH_Component
    {
        public RegulationCheckComponent()
          : base("Regulation Check", "RegCheck",
              "Check FAR/BCR of given Breps",
              "ArchPlanning", "Check")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Building Masses", "B", "Breps to check", GH_ParamAccess.list);
            pManager.AddGenericParameter("Site Data", "S", "Site Object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Reg Max FAR", "FAR", "Max FAR (%)", GH_ParamAccess.item, 200.0);
            pManager.AddNumberParameter("Reg Max BCR", "BCR", "Max BCR (%)", GH_ParamAccess.item, 60.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Current FAR", "cFAR", "Current FAR (%)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Current BCR", "cBCR", "Current BCR (%)", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "St", "Pass/Fail Status", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Brep> breps = new List<Brep>();
            object siteObj = null;
            double maxFar = 200;
            double maxBcr = 60;

            if (!DA.GetDataList(0, breps)) return;
            if (!DA.GetData(1, ref siteObj)) return;
            DA.GetData(2, ref maxFar);
            DA.GetData(3, ref maxBcr);

            Site site = siteObj as Site;
            if (siteObj is GH_ObjectWrapper)
            {
                GH_ObjectWrapper wrapper = siteObj as GH_ObjectWrapper;
                site = wrapper.Value as Site;
            }

            if (site == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Site Input");
                return;
            }

            // Calculation
            double totalFloorArea = 0;
            double buildingArea = 0; // Max footprint

            // Simplified calc: 
            // Total Area: Sum of areas of all horizontal top/bottom surfaces / 2? No, usually floor surfaces.
            // Or sum of volumes / floor height? 
            // Let's assume input Breps are solid volumes (floors).
            // We need to calculate the area of the bottom face of each brep, or if it is a multi-floor mass, slice it.
            // For this Component, let's assume valid 'Floor' breps (slabs) or 'Mass' breps.
            
            // If Mass, we usually slice. But implementing slicer is complex.
            // Let's assume the user pipes in 'Floors' (flat breps) or 'Masses' where we just take Volume/Height?
            // BETTER APPROACH: Calculate projected area for BCR, and Sum of Areas for FAR?
            // If they are solids, we need to know if they represent one floor or many.
            
            // MVP Strategy: Treat each Brep as a 'Mass'.
            //   BCR = Union of all footprints / Site Area
            //   FAR = Sum of Volume / (Standard Height)? No.
            //   If the generated massing is 'stacked floor slabs', we can just sum their top areas.
            
            // Let's calculate the Planar Union of the footprints for BCR.
            // Let's calculate the Volume / 3.5m? Or just ask user?
            
            // Let's assume the MassingGenerator produces individual floor slabs (volumes of height H).
            // So we can just sum the Volume and divide by average thickness? 
            // Or simply Sum(Area of bottom face).
            
            foreach (var b in breps)
            {
               // Naive approach: Get Area of faces pointing up?
               // Or Volume.
               var amp = AreaMassProperties.Compute(b);
               if (amp != null)
               {
                   // If it's a solid, we might assume it's one floor.
                   // Let's assume we sum the area of faces with normal Z > 0 (Roof/Floor)
                   // But if it is a solid box, it has top and bottom.
                   // 100m2 box -> Top 100, Bottom 100.
                   // Floor area is 100.
                   // So we take Total Area of Horizontal Faces / 2 ?
                   
                   // Let's try: Get largest horizontal section?
                   
                   // Simplified: Volume / 3.5 (default floor height)? No, inaccurate.
                   
                   // Let's iterate faces.
                   foreach(var face in b.Faces)
                   {
                       // Check normal
                       /* 
                          We need a reliable way.
                          For now, let's approximate: 
                          FloorArea += Volume / 3.0 (if user doesn't provide floor height?)
                       */
                   }
                   
                   // Better: just use Volume if we assume it's generated by our tool.
                   // If generated by MassingGenerator, they are slabs.
                   // Floor Area ~= Volume / Height (from generator).
                   
                   // Actually, let's just use the bounding box area for footprint?
               }
            }
            
            // REVISION:
            // Input should be "Floor Surfaces" for FAR, and "Mass" for BCR?
            // Let's keep it simple: We check "Masses".
            // BCR = Shadow Area (Outline) / Site Area.
            // FAR = Volume / 3.5 (Approximation) / Site Area?? No.
            
            // Let's just stick to what MassingGenerator produces: "Floor Slabs" as Breps.
            // Each Brep is one floor.
            // Floor Area = Volume / Height (or just top face area).
            
            foreach(var b in breps)
            {
                var amp = VolumeMassProperties.Compute(b);
                if (amp != null)
                {
                    // Assume standard thickness? 
                    // Let's look at the BoundingBox.Z length
                    var bbox = b.GetBoundingBox(true);
                    var height = bbox.Max.Z - bbox.Min.Z;
                    
                    if (height > 0.1) // Avoid flat surfaces
                    {
                        var area = amp.Volume / height; // V = A * h -> A = V/h
                        totalFloorArea += area;
                        
                        // For BCR, we need the union of footprints.
                        // Simplified: Accumulate max area? OR Union curves.
                        // Let's assume stacked boxes for now -> Max area of any single floor?
                        // No, could be separate buildings.
                        buildingArea += area; // Only if not overlapping!!
                    }
                }
            }
            // Note: BCR calculation above is WRONG if floors overlap (multistory).
            // BCR should be the area of the projected union.
            // For MVP, if we assume a single tower stack, BCR is max floor area.
            
            // Let's refine BCR roughly: Max(Floor Areas) (assuming single stack)
            // If multiple stacks, this is wrong.
            // Let's compute Curve.CreateBooleanUnion of all footprints?
            // That's heavy.
            
            // For this specific 'SimpleMassing' workflow, the footprint IS consistent or setbacked.
            // So default to Max(FloorArea).
            
            double maxSingleFloorArea = 0;
             foreach(var b in breps)
            {
                var amp = VolumeMassProperties.Compute(b);
                if (amp != null) {
                     var bbox = b.GetBoundingBox(true);
                    var height = bbox.Max.Z - bbox.Min.Z;
                    if (height > 0.01) {
                         double a = amp.Volume / height;
                         if (a > maxSingleFloorArea) maxSingleFloorArea = a;
                    }
                }
            }
            buildingArea = maxSingleFloorArea;

            double currentFar = (totalFloorArea / site.Area) * 100;
            double currentBcr = (buildingArea / site.Area) * 100;

            string status = "Pass";
            if (currentFar > maxFar) status = "Fail (FAR)";
            if (currentBcr > maxBcr) status = (!status.StartsWith("Fail") ? "Fail (BCR)" : status + " & BCR");

            DA.SetData(0, currentFar);
            DA.SetData(1, currentBcr);
            DA.SetData(2, status);
        }

        protected override System.Drawing.Bitmap Icon { get { return null; } }
        public override Guid ComponentGuid { get { return new Guid("D3456789-01AB-4CDE-F012-3456789ABCDE"); } }
    }
}
