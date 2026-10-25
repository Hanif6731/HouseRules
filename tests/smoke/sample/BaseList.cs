namespace Sample;

public interface IFirst;

public interface ISecond;

// The layout RCS0053 asks for when it measures indentation in 4-space steps.
public sealed class TwoBases
    :
        IFirst,
        ISecond
{
}
