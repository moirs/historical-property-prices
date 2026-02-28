namespace PropertyPrices.Infrastructure.Data.Exceptions;

/// <summary>
/// Base exception for SPARQL endpoint operations.
/// </summary>
public class SparqlException : Exception
{
    /// <summary>Initializes a new instance of the SparqlException class.</summary>
    public SparqlException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the SparqlException class with an inner exception.</summary>
    public SparqlException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when the SPARQL endpoint is unavailable or returns an HTTP error.
/// </summary>
public class SparqlEndpointException : SparqlException
{
    /// <summary>Gets the HTTP status code, if applicable.</summary>
    public int? StatusCode { get; }

    /// <summary>Initializes a new instance of the SparqlEndpointException class.</summary>
    public SparqlEndpointException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>Initializes a new instance with an inner exception.</summary>
    public SparqlEndpointException(string message, Exception innerException, int? statusCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception thrown when a SPARQL query is invalid or malformed.
/// </summary>
public class SparqlQueryException : SparqlException
{
    /// <summary>Gets the invalid query.</summary>
    public string? Query { get; }

    /// <summary>Initializes a new instance of the SparqlQueryException class.</summary>
    public SparqlQueryException(string message, string? query = null)
        : base(message)
    {
        Query = query;
    }

    /// <summary>Initializes a new instance with an inner exception.</summary>
    public SparqlQueryException(string message, string? query, Exception innerException)
        : base(message, innerException)
    {
        Query = query;
    }
}

/// <summary>
/// Exception thrown when a SPARQL query times out.
/// </summary>
public class SparqlTimeoutException : SparqlException
{
    /// <summary>Gets the timeout duration.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>Initializes a new instance of the SparqlTimeoutException class.</summary>
    public SparqlTimeoutException(string message, TimeSpan timeout)
        : base(message)
    {
        Timeout = timeout;
    }

    /// <summary>Initializes a new instance with an inner exception.</summary>
    public SparqlTimeoutException(string message, TimeSpan timeout, Exception innerException)
        : base(message, innerException)
    {
        Timeout = timeout;
    }
}
