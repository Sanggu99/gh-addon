using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ArchPlanningAddon
{
    public class ArchPlanningAddonInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "ArchPlanningAddon";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                return "Architectural Planning Tools for FAR/BCR checks and Massing";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("a0e91234-7d88-43e5-8f69-7c9876543210");
            }
        }

        public override string AuthorName
        {
            get
            {
                return "Antigravity";
            }
        }

        public override string AuthorContact
        {
            get
            {
                return "";
                }
}
        }
}
