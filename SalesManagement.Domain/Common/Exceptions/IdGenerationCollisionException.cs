namespace SalesManagement.Domain.Common.Exceptions
{
    public sealed class IdGenerationCollisionException : DomainException
    {
        public IdGenerationCollisionException(string message) : base(message) { }
    }
}
