namespace swd.Application.Exceptions
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message)
            : base(message)
        {
        }
    }
}
