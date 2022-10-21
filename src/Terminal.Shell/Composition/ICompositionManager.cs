namespace Terminal.Shell;

interface ICompositionManager
{
    Task<IComposition> CreateCompositionAsync(CancellationToken cancellation = default);
}
