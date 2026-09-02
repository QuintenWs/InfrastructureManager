using System.Net;

namespace InfrastructureManager.Domain.Helpers;

public static class SubnetHelper
{
    /// <summary>
    /// Converts an IPv4 address string to a uint for bitwise operations.
    /// Returns null if the string is not a valid IPv4 address.
    /// </summary>
    public static uint? ToUint(string ip)
    {
        if (!IPAddress.TryParse(ip, out var addr)) return null;
        var bytes = addr.GetAddressBytes();
        if (bytes.Length != 4) return null;
        return ((uint)bytes[0] << 24)
             | ((uint)bytes[1] << 16)
             | ((uint)bytes[2] << 8)
             |  (uint)bytes[3];
    }

    /// <summary>
    /// Returns the network mask for a given CIDR prefix length.
    /// e.g. /24 → 0xFFFFFF00
    /// </summary>
    public static uint CidrToMask(int cidr)
    {
        if (cidr == 0) return 0;
        if (cidr == 32) return 0xFFFFFFFF;
        return 0xFFFFFFFF << (32 - cidr);
    }

    /// <summary>
    /// Returns true when the given IP is the correct network address for the CIDR.
    /// e.g. "192.168.10.0" with /24 is valid; "192.168.10.5" with /24 is NOT.
    /// </summary>
    public static bool IsValidNetworkAddress(string networkAddress, int cidr)
    {
        var ip = ToUint(networkAddress);
        if (ip == null) return false;

        var mask    = CidrToMask(cidr);
        var network = ip.Value & mask;

        // The network address must equal itself masked — no host bits set
        return network == ip.Value;
    }

    /// <summary>
    /// Returns the correct network address for a given IP and CIDR.
    /// e.g. "192.168.10.5" /24 → "192.168.10.0"
    /// </summary>
    public static string GetNetworkAddress(string ip, int cidr)
    {
        var ipUint = ToUint(ip);
        if (ipUint == null) return ip;

        var mask    = CidrToMask(cidr);
        var network = ipUint.Value & mask;

        return $"{(network >> 24) & 0xFF}." +
               $"{(network >> 16) & 0xFF}." +
               $"{(network >> 8)  & 0xFF}." +
               $"{network & 0xFF}";
    }

    /// <summary>
    /// Returns true if two subnets overlap.
    /// e.g. 192.168.10.0/24 and 192.168.10.128/25 overlap.
    /// </summary>
    public static bool Overlaps(
        string networkA, int cidrA,
        string networkB, int cidrB)
    {
        var ipA = ToUint(networkA);
        var ipB = ToUint(networkB);
        if (ipA == null || ipB == null) return false;

        var maskA  = CidrToMask(cidrA);
        var maskB  = CidrToMask(cidrB);
        var netA   = ipA.Value & maskA;
        var netB   = ipB.Value & maskB;

        // A overlaps B if A's network falls within B's range or vice versa
        return (netA & maskB) == netB || (netB & maskA) == netA;
    }
}
