namespace Assets.Scripts
{
    public interface IGameplayObject
    {
        Player Owner { get; }
        bool IsEnabled { get; }
    }
}
