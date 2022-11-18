namespace Terminal.Shell;

interface ICompositionManager
{
    Task<IComposition> CreateCompositionAsync(bool cached = true, CancellationToken cancellation = default);
}
