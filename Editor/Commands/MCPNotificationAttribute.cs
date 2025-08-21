using System;

namespace Braxnet.Commands;

/// <summary>
/// Attribute to mark classes as MCP notification handlers
/// </summary>
[AttributeUsage( AttributeTargets.Class )]
public class MCPNotificationAttribute : Attribute
{
    public string Name { get; }

    public MCPNotificationAttribute( string name )
    {
        Name = name;
    }
}
