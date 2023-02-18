namespace Assets.Scripts.Buildings
{
    public class Building
    {
        public BuildingType Type { get; set; }

        public BuildingState State { get; set; }

        public IFraction Fraction { get; set; }
    }
}
