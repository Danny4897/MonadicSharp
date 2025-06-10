using MonadicSharp;

namespace MonadicSharp;

public readonly struct Either<TLeft, TRight>
{
    private readonly TLeft _left;
    private readonly TRight _right;
    private readonly bool _isLeft;

    private Either(TLeft left)
    {
        _left = left;
        _right = default!;
        _isLeft = true;
    }

    private Either(TRight right)
    {
        _right = right;
        _left = default!;
        _isLeft = false;
    }

    public bool IsLeft => _isLeft;
    public bool IsRight => !_isLeft;

    public Option<TLeft> Left => _isLeft ? Option<TLeft>.Some(_left) : Option<TLeft>.None;
    public Option<TRight> Right => !_isLeft ? Option<TRight>.Some(_right) : Option<TRight>.None;

    public static Either<TLeft, TRight> FromLeft(TLeft value) => new(value);
    public static Either<TLeft, TRight> FromRight(TRight value) => new(value);
    // Pattern matching
    public TResult Match<TResult>(Func<TLeft, TResult> onLeft, Func<TRight, TResult> onRight)
    {
        return _isLeft ? onLeft(_left) : onRight(_right);
    }

    public void Match(Action<TLeft> onLeft, Action<TRight> onRight)
    {
        if (_isLeft) onLeft(_left);
        else onRight(_right);
    }

    // Functor/Monad
    public Either<TLeft, TResult> Map<TResult>(Func<TRight, TResult> mapper)
    {
        return _isLeft ? Either<TLeft, TResult>.FromLeft(_left) : Either<TLeft, TResult>.FromRight(mapper(_right));
    }

    public Either<TLeft, TResult> Bind<TResult>(Func<TRight, Either<TLeft, TResult>> binder)
    {
        return _isLeft ? Either<TLeft, TResult>.FromLeft(_left) : binder(_right);
    }

    public override string ToString()
    {
        return _isLeft ? $"Left({_left})" : $"Right({_right})";
    }
}