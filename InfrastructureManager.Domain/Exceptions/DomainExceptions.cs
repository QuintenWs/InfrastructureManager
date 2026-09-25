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

/// <summary>Thrown when a device custom-field value's format doesn't match
/// its FieldType (e.g. non-numeric text in a "number" field), or a required
/// field was left empty.</summary>
public class DeviceFieldValidationException : Exception
{
    public DeviceFieldValidationException(string message) : base(message) { }
}
