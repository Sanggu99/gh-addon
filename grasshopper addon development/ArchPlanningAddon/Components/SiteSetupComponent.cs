using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ArchPlanningAddon.Core;

namespace ArchPlanningAddon.Components
{
    public class SiteSetupComponent : GH_Component
    {
        public SiteSetupComponent()
          : base("Setup Site", "Site",
              "Define Site Boundary and Properties",
              "ArchPlanning", "Setup")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Boundary", "B", "Site Boundary Curve", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Site Data", "S", "Site Object", GH_ParamAccess.item);
            pManager.AddNumberParameter("Area", "A", "Site Area", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve boundary = null;
            if (!DA.GetData(0, ref boundary)) return;

            if (boundary == null || !boundary.IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary must be a closed curve.");
                return;
            }

            Site site = new Site(boundary);
            
            DA.SetData(0, site);
            DA.SetData(1, site.Area);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return null; }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("B1234567-89AB-4CDE-F012-3456789ABCDE"); }
        }
    }
}
