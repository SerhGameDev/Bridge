#nullable enable
using System;

namespace Bridge.Runtime
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class BridgeCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; set; } = "";
        public BridgePermission Permission { get; set; } = BridgePermission.Admin;
        public BridgeCommandAttribute(string name) { Name = name; }
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class BridgeRangeAttribute : Attribute
    {
        public double Minimum { get; }
        public double Maximum { get; }
        public BridgeRangeAttribute(double minimum, double maximum) { Minimum = minimum; Maximum = maximum; }
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class BridgeDescriptionAttribute : Attribute
    {
        public string Description { get; }
        public BridgeDescriptionAttribute(string description) { Description = description; }
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class BridgeAllowNullAttribute : Attribute { }
}
