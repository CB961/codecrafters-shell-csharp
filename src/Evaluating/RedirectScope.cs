using System.Collections.Immutable;
using codecrafters_shell.Helpers;
using codecrafters_shell.Interfaces;
using codecrafters_shell.Parsing;
using codecrafters_shell.Parsing.Ast;
using codecrafters_shell.Redirection;

namespace codecrafters_shell.Evaluating;

public sealed class RedirectScope : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    public IShellContext Context { get; }
    
    public RedirectScope(
        ImmutableList<RedirectNode> scRedirects,
        IShellContext mainContext,
        ArgumentEvaluator argEvaluator
    )
    {
        var redirect = scRedirects.LastOrDefault(r => r.Type == RedirectType.Out);

        if (redirect is null)
        {
            Context = mainContext;
            return;
        } 

        var target = argEvaluator.Evaluate(redirect.Target);
        var fullTarget = PathHelper.ExpandPath(target, mainContext);
        
        Directory.CreateDirectory(Path.GetDirectoryName(fullTarget)!);

        var fs = new FileStream(fullTarget, FileMode.Create, FileAccess.Write);
        var sw = new StreamWriter(fs) { AutoFlush = true };

        _disposables.Add(sw);
        _disposables.Add(fs);

        Context = new RedirectionContext(mainContext, sw, mainContext.StdErr);
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }
}