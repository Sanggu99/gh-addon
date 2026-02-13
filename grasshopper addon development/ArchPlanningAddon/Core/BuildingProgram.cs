using System;
using System.Drawing;

namespace ArchPlanningAddon.Core
{
    public enum BuildingShape
    {
        Rectangle,
        L_Shape,
        U_Shape,
        Any
    }

    public enum StackingType
    {
        Independent, // Sits on ground
        Podium,      // Sits on ground, supports towers
        Tower        // Sits on Podium
    }

    public class BuildingProgram
    {
        public string Name { get; set; }
        public Color DisplayColor { get; set; }
        
        // Area Targets
        public double TargetTotalArea { get; set; }
        public double MinFloorArea { get; set; }
        public double MaxFloorArea { get; set; }

        // Spatial Preferences
        public double PreferredWidth { get; set; }
        public double PreferredDepth { get; set; }
        public int PreferredFloors { get; set; } // NEW: Control height/footprint ratio
        
        // Typology
        public BuildingShape AllowedShape { get; set; }
        public StackingType Stacking { get; set; }

        public BuildingProgram(string name, double totalArea, BuildingShape shape = BuildingShape.Rectangle, StackingType stacking = StackingType.Independent, int floors = 0)
        {
            Name = name;
            TargetTotalArea = totalArea;
            AllowedShape = shape;
            Stacking = stacking;
            DisplayColor = Color.Gray;

            // Defaults
            PreferredFloors = floors > 0 ? floors : (stacking == StackingType.Podium ? 2 : 20); // Heuristic defaults
            
            MinFloorArea = totalArea / PreferredFloors; 
            MaxFloorArea = totalArea; 
            PreferredWidth = 30.0;
            PreferredDepth = 15.0;
        }
    }
}
