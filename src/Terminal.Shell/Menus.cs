using System.Composition;

namespace Terminal.Shell;

[Shared]
partial class Menus
{
    [MenuCommand("File._Exit")]
    internal void Exit() => Application.ExitRunLoopAfterFirstIteration = true;
}
