# Azure AI Search API

This is a comprehensive ASP.NET Web API for working with Azure AI Search service, particularly focused on vector search capabilities using text and image embeddings. The API provides endpoints for managing search indexes, generating embeddings, and performing various types of searches.

## Overview

The Azure AI Search API serves as a backend interface to Azure AI Search with integrated vector search capabilities. It supports:

- Creating and managing text and image vector search indexes
- Generating embeddings for text and images using Azure OpenAI
- Performing vector-based, hybrid, and semantic searches
- Supporting both text-only and multimodal (text+image) searches
- Document management within indexes

The API is designed to work with golf ball data as a demonstration use case, but can be extended for other applications.

## Authentication

The API uses Azure authentication with support for:
- Local development using Azure CLI credentials
- Production deployment using Managed Identity

## API Endpoints

### Index Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Index` | Retrieves all available index names |
| POST | `/api/Index/text` | Creates a new text embeddings index |
| POST | `/api/Index/textimage` | Creates a new text and image embeddings index |
| GET | `/api/Index/{indexName}/details` | Gets detailed information about a specific index |
| GET | `/api/Index/{indexName}/statistics` | Gets statistics for a specific index |
| DELETE | `/api/Index/{indexName}` | Deletes a specific index |

### Document Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Document/text/{indexName}/documents` | Lists documents in a text embeddings index |
| GET | `/api/Document/textimage/{indexName}/documents` | Lists documents in a text and image embeddings index |

### Embedding Generation

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Emedding/text/{indexName}/embeddings` | Generates embeddings for a text-only index |
| POST | `/api/Emedding/textimage/{indexName}/embeddings` | Generates embeddings for a text and image index |

### Search Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Search/text/{indexName}/search` | Performs a search against a text embeddings index |
| POST | `/api/Search/textimage/{indexName}/search` | Performs a search against a text and image embeddings index |

## Data Models

### GolfBallDataV1 (Text-only embedding model)
- Id
- Manufacturer
- USGA_Lot_Num
- Pole_Marking
- Colour
- ConstCode
- BallSpecs
- Dimples
- Spin
- Pole_2
- Seam_Marking
- ImageUrl
- VectorContent (for text embeddings)

### GolfBallDataV2 (Text and image embedding model)
- Id
- Manufacturer
- USGA_Lot_Num
- Pole_Marking
- Colour
- ConstCode
- BallSpecs
- Dimples
- Spin
- Pole_2
- Seam_Marking
- ImageUrl
- TextVector (for text embeddings)
- ImageVector (for image embeddings)

## Search Options

The API supports several search modes:
- Text-only search using vector embeddings
- Image search using vector embeddings
- Hybrid search (combining vector similarity with traditional keyword search)
- Semantic search with re-ranking

## Configuration

The API requires the following configuration settings in `appsettings.json` or `appsettings.Local.json`:

```json
{
  "Azure": {
    "TenantId": "your-tenant-id",
    "AccountToUse": "account-for-local-testing"
  },
  "AZURE_MANAGED_IDENTITY": "managed-identity-service-principal-id",
  "AZURE_SEARCH_SERVICE_ENDPOINT": "https://your-search-service-endpoint",
  "AZURE_SEARCH_ADMIN_KEY": "your-search-admin-key",
  "AZURE_SEARCH_KEY": "your-search-reader-key",
  "AZURE_OPENAI_ENDPOINT": "https://your-openai-endpoint/",
  "AZURE_OPENAI_API_KEY": "your-openai-api-key",
  "AZURE_OPENAI_DEPLOYMENT": "gpt-4o",
  "AZURE_OPENAI_API_VERSION": "2024-08-01-preview",
  "AZURE_OPENAI_EMBEDDING_DEPLOYMENT": "text-embedding-ada-002",
  "AZURE_OPENAI_EMBEDDING_MODEL": "text-embedding-ada-002",
  "AZURE_OPENAI_EMBEDDING_DIMENSIONS": "1536",
  "AZURE_VISION_ENDPOINT": "https://your-vision-endpoint",
  "AZURE_VISION_KEY": "your-vision-key",
  "CSV_FILE_NAME": "sample-data-b1.csv"
}
```

## Getting Started

1. Clone the repository
2. Configure `appsettings.Local.json` with your Azure service credentials
3. Run the application
4. Access the Swagger UI at `/swagger` to explore and test the API endpoints

## Dependencies

The API uses the following key packages:
- Azure.AI.OpenAI
- Azure.Identity
- Azure.Search.Documents
- Swashbuckle.AspNetCore for API documentation

## Docker Support

The project includes Docker support for containerized deployment. 