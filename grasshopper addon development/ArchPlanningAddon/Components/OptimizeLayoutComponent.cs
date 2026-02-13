using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ArchPlanningAddon.Core;
using Grasshopper.Kernel.Types;

namespace ArchPlanningAddon.Components
{
    public class OptimizeLayoutComponent : GH_Component
    {
        public OptimizeLayoutComponent()
          : base("Optimize Layout", "Optimize",
              "Generate optimal site layout for given programs",
              "ArchPlanning", "Optimize")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Site Data", "S", "Site Object", GH_ParamAccess.item);
            pManager.AddGenericParameter("Programs", "P", "List of Building Programs", GH_ParamAccess.list);
            pManager.AddNumberParameter("Max BCR", "BCR", "Maximum Building Coverage Ratio (0.0-1.0)", GH_ParamAccess.item, 0.6);
            pManager.AddNumberParameter("Max FAR", "FAR", "Maximum Floor Area Ratio (0.0-10.0)", GH_ParamAccess.item, 2.0);
            pManager.AddNumberParameter("Height Limit", "H", "Height Limit (m)", GH_ParamAccess.item, 100.0);
            pManager.AddIntegerParameter("Iterations", "I", "Number of Iterations", GH_ParamAccess.item, 100);
            pManager.AddBooleanParameter("Run", "R", "Run Optimization", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Massing", "M", "Best Layout Massing", GH_ParamAccess.list);
            pManager.AddTextParameter("Report", "Rep", "Optimization Report", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object siteObj = null;
            List<BuildingProgram> programs = new List<BuildingProgram>();
            double maxBCR = 0.6;
            double maxFAR = 2.0;
            double maxH = 100.0;
            int iterations = 100;
            bool run = false;

            if (!DA.GetData(0, ref siteObj)) return;
            if (!DA.GetDataList(1, programs)) return;
            DA.GetData(2, ref maxBCR);
            DA.GetData(3, ref maxFAR);
            DA.GetData(4, ref maxH);
            DA.GetData(5, ref iterations);
            DA.GetData(6, ref run);

            if (!run) return;

            Site site = siteObj as Site;
            if (siteObj is GH_ObjectWrapper)
            {
                GH_ObjectWrapper wrapper = siteObj as GH_ObjectWrapper;
                site = wrapper.Value as Site;
            }

            if (site == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Site");
                return;
            }
            
            // Site Geometry might be mesh/brep converted to Site object?
            // Assuming Site object is valid from SiteSetupComponent.

            Regulations regs = new Regulations(maxBCR * 100.0, maxFAR * 100.0, maxH); // Regs constructor expects Percentage?
            // Checking Regulations.cs...
            // public Regulations(double maxBCR, double maxFAR, double maxHeight...
            // Usually user inputs 0.6 for 60%. SimpleMassingComponent inputs 60 directly.
            // Let's assume input is Percentage based on default 0.6? No, typical GH convention is 0.6. 
            // BUT Regulations.cs likely uses whole numbers if previously we used sliders 60, 200.
            // Let's convert 0.6 -> 60.
            if (maxBCR <= 1.0) maxBCR *= 100.0;
            if (maxFAR <= 10.0 && maxFAR > 0) maxFAR *= 100.0; // Heuristic
            
            regs = new Regulations(maxBCR, maxFAR, maxH, 0, true); // Enable Solar by default in Optimization? Or add input. 
            // For now, enable Solar.
            
            OptimizationResult res = LayoutOptimizer.Optimize(site, programs, regs, iterations);

            DA.SetDataList(0, res.Massing);
            DA.SetData(1, res.Report);
        }

        protected override System.Drawing.Bitmap Icon { get { return null; } }
        public override Guid ComponentGuid { get { return new Guid("E4567890-1234-5678-90AB-CDEF01234568"); } }
    }
}
