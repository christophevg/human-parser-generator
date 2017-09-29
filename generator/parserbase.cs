// ParserBase - base functionality for parsers
// author: Adam Simon <adamosimoni@gmail.com>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

public partial interface ISyntaxNode { }

public abstract class ParserBase<TRoot>
    where TRoot : class, ISyntaxNode, new()
{
    #region Stack frame
    abstract class RuleStackFrame
    {
        public RuleStackFrame(RuleStackFrame parent, int position)
        {
            Parent = parent;
            Position = position;
        }

        public RuleStackFrame Parent { get; }
        public abstract Action Rule { get; }
        public abstract ISyntaxNode Node { get; set; }
        public int Position { get; }
        public ParseError Error { get; set; }
        public bool Success => Error == null;
        public Action OnError { get; set; }
        public Action OnSuccess { get; set; }
    }

    class RootStackFrame : RuleStackFrame
    {
        public RootStackFrame() : base(null, 0) { }

        public override Action Rule
        {
            get => throw new NotSupportedException();
        }

        public override ISyntaxNode Node
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }

    class SingleRuleStackFrame : RuleStackFrame
    {
        public SingleRuleStackFrame(RuleStackFrame parent, Scanner scanner, Action rule) : base(parent, scanner.Position)
        {
            Rule = rule;
        }

        public override ISyntaxNode Node
        {
            get => Parent.Node;
            set => Parent.Node = value;
        }

        public override Action Rule { get; }
    }

    class NonTerminalFrame : SingleRuleStackFrame
    {
        public NonTerminalFrame(RuleStackFrame parent, Scanner scanner, Action rule) : base(parent, scanner, rule) { }

        public override ISyntaxNode Node { get; set; }
    }

    class CompositeRuleFrame : RuleStackFrame
    {
        public CompositeRuleFrame(RuleStackFrame parent, Scanner scanner, Action[] rules) : base(parent, scanner.Position)
        {
            Rules = rules;
        }

        public override ISyntaxNode Node
        {
            get => Parent.Node;
            set => Parent.Node = value;
        }

        public override Action Rule => Rules[RuleIndex];

        public Action[] Rules { get; }
        public int RuleIndex { get; set; }
    }
    #endregion

    static readonly Action noop = () => { };
    readonly Action _single, _conjunction, _disjunction;
    readonly Action _repeat, _propagateError;

    Stack<(Action Action, RuleStackFrame Frame)> _stack;
    RuleStackFrame _currentFrame;

    Scanner _scanner;

    public ParserBase() : this(null) { }

    public ParserBase(string ignoring)
    {
        Ignoring = ignoring;

        // caching delegates
        _single = Single;
        _conjunction = Conjunction;
        _disjunction = Disjunction;
        _repeat = Repeat;
        _propagateError = PropagateError;
    }

    [DebuggerStepThrough]
    protected TNode GetNode<TNode>()
        where TNode : class, ISyntaxNode
    {
        return (TNode)_currentFrame.Node;
    }

    [DebuggerStepThrough]
    protected void SetNode(ISyntaxNode node)
    {
        _currentFrame.Node = node;
    }

    public string Ignoring { get; }

    public TRoot SyntaxTree { get; private set; }
    public ParseError Error { get; private set; }

    public ParserBase<TRoot> Parse(string source)
    {
        _stack = new Stack<(Action, RuleStackFrame)>();
        _scanner = new Scanner(source);

        SyntaxTree = null;
        Error = null;

        if (Ignoring != null)
            _scanner.Ignore(Ignoring);

        _currentFrame = new RootStackFrame();

        TRoot result = null;
        Root(r => result = r);

        while (_stack.TryPop(out (Action Action, RuleStackFrame Frame) item))
        {
            _currentFrame = item.Frame;
            item.Action();
        }

        if (!_currentFrame.Success)
            Error = _currentFrame.Error;
        else if (!_scanner.IsConsumed)
            Error = _scanner.CreateError(() => "Cannot parse remaining data");
        else
            SyntaxTree = result;

        return this;
    }

    protected abstract void Root(Action<TRoot> onSuccess);

    void Push(Action action, RuleStackFrame frame)
    {
        _stack.Push((action, frame));
    }

    #region Rule execution
    void Single()
    {
        var currentFrame = (SingleRuleStackFrame)_currentFrame;

        // pushing in reverse order because stack provides right-to-left traversal
        Push(() =>
        {
            if (!_currentFrame.Success)
            {
                _scanner.Position = _currentFrame.Position;
                _currentFrame.OnError();
            }
            else
                _currentFrame.OnSuccess();
        }, currentFrame);

        currentFrame.Rule();
    }

    void Conjunction()
    {
        var currentFrame = (CompositeRuleFrame)_currentFrame;

        if (!currentFrame.Success)
        {
            _scanner.Position = currentFrame.Position;
            currentFrame.OnError();
            return;
        }

        if (currentFrame.RuleIndex >= currentFrame.Rules.Length)
        {
            currentFrame.OnSuccess();
            return;
        }

        // pushing in reverse order because stack provides right-to-left traversal
        Push(_conjunction, currentFrame);

        currentFrame.Rule();
        currentFrame.RuleIndex++;
    }

    void Disjunction()
    {
        var currentFrame = (CompositeRuleFrame)_currentFrame;

        if (currentFrame.RuleIndex > 0 && currentFrame.Success)
        {
            currentFrame.OnSuccess();
            return;
        }

        _scanner.Position = currentFrame.Position;

        if (currentFrame.RuleIndex >= currentFrame.Rules.Length)
        {
            currentFrame.OnError();
            return;
        }

        currentFrame.Error = null;

        // pushing in reverse order because stack provides right-to-left traversal
        Push(_disjunction, currentFrame);

        currentFrame.Rule();
        currentFrame.RuleIndex++;
    }
    #endregion

    #region Completion handling
    void Repeat()
    {
        Push(_single, new SingleRuleStackFrame(_currentFrame.Parent, _scanner, _currentFrame.Rule)
        {
            OnError = _currentFrame.OnError,
            OnSuccess = _currentFrame.OnSuccess,
        });
    }

    void PropagateError()
    {
        _currentFrame.Parent.Error = _currentFrame.Error;
    }
    #endregion

    #region Rules
    protected void Terminal(string value)
    {
        var error = _scanner.TryConsume(value);
        if (error != null)
            _currentFrame.Error = error;
    }

    protected void Terminal(Regex pattern, Action<string> onSuccess)
    {
        var error = _scanner.TryConsume(pattern, out string value);
        if (error == null)
            onSuccess(value);
        else
            _currentFrame.Error = error;
    }

    protected void Terminal<TNode>(Regex pattern, Action<TNode, string> setter, Action<TNode> onSuccess)
        where TNode : class, ISyntaxNode, new()
    {
        var error = _scanner.TryConsume(pattern, out string value);
        if (error == null)
        {
            var node = new TNode();
            setter(node, value);
            onSuccess(node);
        }
        else
            _currentFrame.Error = error;
    }

    void NonTerminal<TNode>(TNode node, Action<TNode> onSuccess, Action rule)
        where TNode : class, ISyntaxNode
    {
        Push(_single, new NonTerminalFrame(_currentFrame, _scanner, rule)
        {
            Node = node,
            OnError = _propagateError,
            OnSuccess = () =>
            {
                var result = GetNode<TNode>();
                _currentFrame = _currentFrame.Parent;
                onSuccess(result);
            }
        });
    }

    protected void VirtualNonTerminal<TNode>(Action<TNode> onSuccess, Action rule)
        where TNode : class, ISyntaxNode
    {
        NonTerminal(null, onSuccess, rule);
    }

    protected void NonTerminal<TNode>(Action<TNode> onSuccess, Action rule)
        where TNode : class, ISyntaxNode, new()
    {
        NonTerminal(new TNode(), onSuccess, rule);
    }

    protected void All(params Action[] rules)
    {
        Push(_conjunction, new CompositeRuleFrame(_currentFrame, _scanner, rules)
        {
            OnError = _propagateError,
            OnSuccess = noop,
        });
    }

    protected void Any(params Action[] rules)
    {
        Push(_disjunction, new CompositeRuleFrame(_currentFrame, _scanner, rules)
        {
            OnError = _propagateError,
            OnSuccess = noop,
        });
    }

    protected void Plural(Action rule)
    {
        Push(_single, new SingleRuleStackFrame(_currentFrame, _scanner, rule)
        {
            OnError = noop,
            OnSuccess = _repeat,
        });
    }

    protected void Optional(Action rule)
    {
        Push(_single, new SingleRuleStackFrame(_currentFrame, _scanner, rule)
        {
            OnError = noop,
            OnSuccess = noop,
        });
    }

    #endregion

    [Conditional("DEBUG")]
    protected void Log(string msg)
    {
#if NETCOREAPP2_0
        Debug.WriteLine(msg + " @ " + _scanner.Context.Replace(Environment.NewLine, "\\n"));
#else
        Console.Error.WriteLine(msg + " @ " + Scanner.Context.Replace(Environment.NewLine, "\\n"));
#endif
    }
}
