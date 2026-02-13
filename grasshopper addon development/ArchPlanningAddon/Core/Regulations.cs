namespace ArchPlanningAddon.Core
{
    public class Regulations
    {
        private double _maxBCR = 60.0;
        private double _maxFAR = 200.0;
        private double _maxHeight = 0.0;

        /// <summary>
        /// Maximum Building Coverage Ratio (Geonpyeolyul) in percent (e.g. 60.0 for 60%)
        /// </summary>
        public double MaxBCR { get { return _maxBCR; } set { _maxBCR = value; } }

        /// <summary>
        /// Maximum Floor Area Ratio (Yongjeongnyul) in percent (e.g. 200.0 for 200%)
        /// </summary>
        public double MaxFAR { get { return _maxFAR; } set { _maxFAR = value; } }

        /// <summary>
        /// Maximum Building Height in meters (optional, <= 0 means no limit)
        /// </summary>
        public double MaxHeight { get { return _maxHeight; } set { _maxHeight = value; } }

        private int _maxFloors;
        public int MaxFloors { get { return _maxFloors; } set { _maxFloors = value; } }

        public bool ApplySolarCheck { get; set; }

        public Regulations(double maxBCR, double maxFAR, double maxHeight, int maxFloors = 0, bool applySolarCheck = false)
        {
            MaxBCR = maxBCR;
            MaxFAR = maxFAR;
            MaxHeight = maxHeight;
            MaxFloors = maxFloors;
            ApplySolarCheck = applySolarCheck;
        }

        public Regulations()
        {
        }
    }
}
