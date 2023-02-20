namespace Assets.Scripts
{
    public interface IFraction
    {
        string Id { get; }
    }

    class Fraction : IFraction
    {
        public Fraction(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}
