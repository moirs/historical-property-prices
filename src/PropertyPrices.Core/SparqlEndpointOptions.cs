namespace PropertyPrices.Core;

public class SparqlEndpointOptions
{
    public const string SectionName = "SparqlEndpoint";

    public string Url { get; set; } = "https://data.gov.uk/sparql";
    public int TimeoutSeconds { get; set; } = 30;
}
