namespace Terminal.Shell;

interface IComposition : IDisposable
{
    //Lazy<T, IDictionary<string, object>> GetExport<T>(string? contractName = default);
    //Lazy<T, TMetadataView> GetExport<T, TMetadataView>(string? contractName = default);

    IEnumerable<Lazy<T, IDictionary<string, object>>> GetExports<T>(string? contractName = default);
    //IEnumerable<Lazy<T, TMetadataView>> GetExports<T, TMetadataView>(string? contractName = default);

    T GetExportedValue<T>(string? contractName = default);
    //IEnumerable<T> GetExportedValues<T>(string? contractName = default);
}
