namespace ClefCraft.Application.Exceptions
{
    /// <summary>
    /// A save matched no row because another request changed or deleted it first (EF Core's
    /// DbUpdateConcurrencyException, which this layer can't reference). Nothing from that save was
    /// written. Not mapped by the exception middleware, so a handler that expects it converts it
    /// into a BadRequestException; anywhere else it stays a 500, as before.
    /// </summary>
    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(Exception innerException)
            : base("The data was changed by another request.", innerException)
        {
        }
    }
}
