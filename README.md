UK Historical Property Prices Explorer

A lightweight application that queries the HM Land Registry SPARQL endpoint to retrieve and analyse historical property sales data across England and Wales.

This project connects directly to the official HM Land Registry Price Paid Data (PPD) dataset via SPARQL, allowing users to:

🔎 Search historical property sale prices by postcode, address, town, or local authority

📅 Filter transactions by date range

🏠 Analyse property types (detached, semi-detached, terraced, flat)

📊 Explore trends in historical sale values

📡 Query live open government data without maintaining a local dataset

The application demonstrates how to:

Construct dynamic SPARQL queries

Integrate with the HM Land Registry Linked Data platform

Parse RDF/SPARQL JSON results

Transform open government data into structured analytics

Data Source

Data is sourced directly from the official HM Land Registry Linked Data SPARQL endpoint, which provides open access to Price Paid Data for England and Wales.

Why This Project?

UK property data is publicly available but not always developer-friendly. This project aims to:

Provide a clean query interface

Enable reproducible historical price analysis

Serve as a foundation for property analytics tools

Demonstrate practical use of SPARQL with real-world government data

Tech Stack (Example)

SPARQL

REST API wrapper (optional): ASP.NET Core

Frontend UI (optional): React