namespace InfrastructureManager.Domain.Exceptions;

/// <summary>Thrown when an IP address is already in use by another device.</summary>
public class IpConflictException : Exception
{
    public string IpAddress    { get; }
    public string ConflictWith { get; }

    public IpConflictException(string ip, string conflictWith)
        : base($"IP address {ip} is already in use by '{conflictWith}'.")
    {
        IpAddress    = ip;
        ConflictWith = conflictWith;
    }
}

/// <summary>Thrown when a network address or CIDR is invalid, or ranges overlap.</summary>
public class SubnetValidationException : Exception
{
    public SubnetValidationException(string message) : base(message) { }
}
