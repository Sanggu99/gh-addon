using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ArchPlanningAddon.Core;
using Grasshopper.Kernel.Types;

namespace ArchPlanningAddon.Components
{
    public class SimpleMassingComponent : GH_Component
    {
        public SimpleMassingComponent()
          : base("Massing Generator", "Massing",
              "Generate simple massing based on FAR/BCR",
              "ArchPlanning", "Generate")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Site Data", "S", "Site Object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max FAR", "FAR", "Max FAR (%)", GH_ParamAccess.item, 200.0);
            pManager.AddNumberParameter("Max BCR", "BCR", "Max BCR (%)", GH_ParamAccess.item, 60.0);
            pManager.AddNumberParameter("Max Height", "H", "Max Height (m)", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Floor Height", "FH", "Floor-to-Floor Height (m)", GH_ParamAccess.item, 3.5);
            pManager.AddIntegerParameter("Max Floors", "F", "Max Floor Count", GH_ParamAccess.item, 0);
            pManager.AddPointParameter("North Point", "NP", "Point defining North (relative to center)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Apply Solar", "Sol", "Apply Solar Right Check?", GH_ParamAccess.item, false);
            pManager[6].Optional = true; // North Point optional
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Massing", "M", "Generated Massing Volumes", GH_ParamAccess.list);
            pManager.AddTextParameter("Report", "R", "Generation Report", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object siteObj = null;
            double far = 200;
            double bcr = 60;
            double maxH = 0;
            int maxF = 0;
            double floorH = 3.5;
            Point3d northPt = Point3d.Unset;
            bool applySolar = false;

            if (!DA.GetData(0, ref siteObj)) return;
            DA.GetData(1, ref far);
            DA.GetData(2, ref bcr);
            DA.GetData(3, ref maxH);
            DA.GetData(4, ref floorH);
            DA.GetData(5, ref maxF);
            DA.GetData(7, ref applySolar);

            Site site = siteObj as Site;
            if (siteObj is GH_ObjectWrapper)
            {
                GH_ObjectWrapper wrapper = siteObj as GH_ObjectWrapper;
                site = wrapper.Value as Site;
            }
            
            if (site == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Site Data");
                return;
            }

            Regulations regs = new Regulations(bcr, far, maxH, maxF, applySolar);
            
            // Calculate North Vector
            Vector3d northVector = Vector3d.YAxis;
            object northPtObj = null;
            DA.GetData(6, ref northPtObj); // Get Point
            if (northPtObj != null)
            {
                 if (northPtObj is GH_Point) northPt = ((GH_Point)northPtObj).Value;
                 else if (northPtObj is Point3d) northPt = (Point3d)northPtObj;
            }

            if (northPt != Point3d.Unset && northPt.IsValid)
            {
                // Use Site Centroid if available, else center
                Point3d center = Point3d.Origin;
                var amp = AreaMassProperties.Compute(site.Boundary);
                if (amp != null) center = amp.Centroid;
                
                northVector = northPt - center;
            }

            string msg;
            List<Brep> massing = MassingGenerator.GenerateSimpleExtrusion(site, regs, floorH, northVector, out msg);

            DA.SetDataList(0, massing);
            DA.SetData(1, msg);
        }

        protected override System.Drawing.Bitmap Icon { get { return null; } }
        public override Guid ComponentGuid { get { return new Guid("C2345678-90AB-4CDE-F012-3456789ABCDE"); } }
    }
}
