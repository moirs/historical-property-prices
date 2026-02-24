# Copilot Instructions for UK Historical Property Prices Explorer

## Project Overview

A data exploration application for querying the HM Land Registry SPARQL endpoint to retrieve and analyse historical property sales data across England and Wales. The project is currently a template with an optional architecture supporting:
- **Backend** (optional): ASP.NET Core REST API
- **Frontend** (optional): React UI
- **Data Layer**: Direct SPARQL queries to HM Land Registry Linked Data

## Data Source Architecture

### SPARQL Endpoint Integration
- **Endpoint**: HM Land Registry Linked Data SPARQL service
- **Data Format**: RDF/Linked Data (queries return JSON)
- **Geographic Scope**: England and Wales only
- **Key Queryable Fields**:
  - Postcode (primary filter - most selective)
  - Local authority
  - Transaction date range
  - Property type (Detached, Semi-detached, Terraced, Flat)
  - Sale price

### Key Implementation Patterns

1. **SPARQL Query Construction**
   - Build queries dynamically based on user filters
   - Always prioritize postcode/local authority filters first (most performant)
   - Include date range constraints when available
   - Request only necessary SPARQL variables to reduce query size

2. **Result Mapping**
   - SPARQL JSON results contain RDF properties that must map to domain objects
   - Handle sparse/missing property values gracefully
   - Parse linked data references from result triples

3. **Performance Considerations**
   - SPARQL endpoint has query complexity limits - avoid overly broad queries
   - Consider caching strategies for frequently accessed queries
   - Test query performance directly against the live endpoint before implementation

4. **Error Handling**
   - Validate postcode/address input format before querying
   - Handle SPARQL endpoint timeouts and unavailability
   - Account for rate limiting on the public endpoint
   - Provide meaningful error messages for invalid search parameters

## Development Approach

When implementing features:

1. **Always test SPARQL queries** directly against the HM Land Registry SPARQL browser first
2. **Separate concerns**: Keep SPARQL query builders distinct from API/UI layers
3. **Type safety** (if using TypeScript): Define types for SPARQL query results
4. **Caching** (if implementing backend): Cache popular postcodes, regions, and trend queries with appropriate TTL

## Testing Strategy

- **SPARQL Query Tests**: Verify queries return expected results with real postcodes (e.g., SW1A1AA, M11AA)
- **Result Parsing**: Test RDF-to-object mapping for all property types and edge cases
- **Integration Tests**: End-to-end tests using consistent test postcodes and date ranges
- **Endpoint Resilience**: Test error handling for timeouts and invalid queries

## Build & Development Commands

To be added once the project structure is established with package.json/build configuration.

## Key Resources

- [HM Land Registry Open Data](https://www.gov.uk/government/organisations/land-registry)
- [Price Paid Data Documentation](https://www.gov.uk/government/statistical-data-sets/price-paid-data-downloads)
- [SPARQL Query Language Guide](https://www.w3.org/TR/sparql11-query/)
