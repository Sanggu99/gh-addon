using System;
using Rhino.Geometry;

namespace ArchPlanningAddon.Core
{
    public class Site
    {
        public Curve Boundary { get; private set; }
        public double Area { get; private set; }
        public Plane SitePlane { get; private set; }

        public Site(Curve boundary)
        {
            if (boundary == null) throw new ArgumentNullException("boundary");
            
            // Ensure boundary is closed and planar
            if (!boundary.IsClosed)
            {
                // Attempt to close? Or throw. For now, throw.
                // Assuming validation happens in component.
            }

            Boundary = boundary;
            
            // Calculate Area
            var amp = AreaMassProperties.Compute(boundary);
            if (amp != null)
            {
                Area = amp.Area;
            }
            else
            {
                Area = 0;
            }

            // Determine plane (assuming flat site for now, or use best fit)
            // For MVP, assume XY plane usually, or fit plane.
            Plane fitPlane;
            if (boundary.TryGetPlane(out fitPlane))
            {
                SitePlane = fitPlane;
            }
            else
            {
                SitePlane = Plane.WorldXY;
            }
        }
    }
}
