namespace InfrastructureManager.Domain.Enums;

public enum DeviceType
{
    // Networking
    Switch         = 1,
    RouterRed      = 2,
    RouterBlack    = 3,
    Crypto         = 4,
    SFP            = 5,
    MediaConverter = 6,
    Firewall       = 7,

    // Workstation
    WPC            = 8,   // Workstation PC
    BPC            = 9,   // Business PC
    WSKit          = 10,  // Workstation Kit
    Desktop        = 11,
    Laptop         = 12,
    ExtraScreen    = 13,

    // Peripherals & Storage
    Printer        = 14,
    UPS            = 15,
    Safe           = 16,
    NAS            = 17,

    // Communication & Mobile
    Phone          = 18,
    VTC            = 19,  // Video Teleconference

    // Military / Specialized
    Armadillo      = 20,
    RAP            = 21,  // Remote Access Point
    SAR            = 22,

    // Cabling
    STPCables      = 23,

    // USB Devices
    USBRed         = 24,
    USBBlack       = 25,

    Other          = 99
}
