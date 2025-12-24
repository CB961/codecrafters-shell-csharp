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
        var redirect = scRedirects.LastOrDefault();

        if (redirect is null)
        {
            Context = mainContext;
            return;
        }

        var outStream = redirect.Type is RedirectType.Out or RedirectType.Append
            ? CreateStreamWriter(redirect, mainContext, argEvaluator)
            : mainContext.StdOut;
        var errStream = redirect.Type is RedirectType.Error or RedirectType.AppendError
            ? CreateStreamWriter(redirect, mainContext, argEvaluator)
            : mainContext.StdErr;

        Context = new RedirectionContext(mainContext, outStream, errStream);
    }

    private StreamWriter CreateStreamWriter(
        RedirectNode redirect,
        IShellContext mainContext,
        ArgumentEvaluator argEvaluator)
    {
        var target = argEvaluator.Evaluate(redirect.Target);
        var fullTarget = PathHelper.ExpandPath(target, mainContext);

        Directory.CreateDirectory(Path.GetDirectoryName(fullTarget)!);

        var fileMode = redirect.Type is RedirectType.Append or RedirectType.AppendError 
            ? FileMode.Append 
            : FileMode.Create;

        var fs = new FileStream(fullTarget, fileMode, FileAccess.Write);
        var sw = new StreamWriter(fs) { AutoFlush = true };

        _disposables.Add(sw);
        _disposables.Add(fs);

        return sw;
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