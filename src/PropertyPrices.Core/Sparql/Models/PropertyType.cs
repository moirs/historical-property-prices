namespace PropertyPrices.Core.Sparql.Models;

/// <summary>
/// Represents property types supported by HM Land Registry price paid data.
/// </summary>
public enum PropertyType
{
    /// <summary>Detached house.</summary>
    Detached,

    /// <summary>Semi-detached house.</summary>
    SemiDetached,

    /// <summary>Terraced house.</summary>
    Terraced,

    /// <summary>Flat or apartment.</summary>
    Flat,

    /// <summary>Other property types not classified above.</summary>
    Other
}
