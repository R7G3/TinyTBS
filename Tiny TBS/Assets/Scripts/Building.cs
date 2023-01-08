namespace Assets.Scripts
{
    public class Building
    {
        public BuildingType Type { get; set; }

        public BuildingState State { get; set; }

        public IFraction Fraction { get; set; }
    }
}
