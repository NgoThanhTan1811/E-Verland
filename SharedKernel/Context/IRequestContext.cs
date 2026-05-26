namespace SharedKernel.Context;

public interface IRequestContext
{
    string TraceId { get; }
}
