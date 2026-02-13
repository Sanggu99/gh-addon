using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ArchPlanningAddon.Core;

namespace ArchPlanningAddon.Components
{
    public class DefineProgramComponent : GH_Component
    {
        public DefineProgramComponent()
          : base("Define Program", "Program",
              "Define a building program requirements",
              "ArchPlanning", "Optimize")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Program Name", GH_ParamAccess.item, "Program A");
            pManager.AddNumberParameter("Target Area", "A", "Target Total Floor Area (m2)", GH_ParamAccess.item, 5000.0);
            pManager.AddIntegerParameter("Shape", "S", "Allowed Shape (0:Rect, 1:L, 2:U)", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Stacking", "St", "Stacking Type (0:Indep, 1:Podium, 2:Tower)", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Floors", "F", "Preferred Floor Count (Height control)", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Program", "P", "Building Program Object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "";
            double area = 0;
            int shapeInt = 0;
            int stackInt = 0;
            int floors = 0;

            if (!DA.GetData(0, ref name)) return;
            if (!DA.GetData(1, ref area)) return;
            DA.GetData(2, ref shapeInt);
            DA.GetData(3, ref stackInt);
            DA.GetData(4, ref floors);

            BuildingShape shape = (BuildingShape)shapeInt;
            StackingType stacking = (StackingType)stackInt;

            BuildingProgram prog = new BuildingProgram(name, area, shape, stacking, floors);

            DA.SetData(0, prog);
        }

        protected override System.Drawing.Bitmap Icon { get { return null; } }
        public override Guid ComponentGuid { get { return new Guid("D3456789-0123-4567-89AB-CDEF01234567"); } }
    }
}
