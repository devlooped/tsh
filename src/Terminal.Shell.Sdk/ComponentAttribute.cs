using System.Composition;

namespace Terminal.Shell;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public abstract class ComponentAttribute : ExportAttribute
{
    protected ComponentAttribute() { }

    protected ComponentAttribute(Type contractType) : base(contractType) { }

    protected ComponentAttribute(string contractName) : base(contractName) { }

    protected ComponentAttribute(string contractName, Type contractType) : base(contractName, contractType) { }
}
