[README (2).md](https://github.com/user-attachments/files/33304911/README.2.md)
# CoffeeNChill Canteen Management System

**Module:** Cloud Development B (CLDV6212/w)
**Assessment:** PoE, Part 2: Queue Triggers & Docker Compose Orchestration

| Student | Student Number |
|---|---|
| Nikshay | ST10473535 |
| Sena | ST10469318 |

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Video Demonstration](#video-demonstration)
3. [Group Member Contributions](#group-member-contributions)
4. [Architecture](#architecture)
5. [Repository Structure](#repository-structure)
6. [Prerequisites](#prerequisites)
7. [Quick Start (Docker Compose)](#quick-start-docker-compose)
8. [Docker Hub Images](#docker-hub-images)
9. [API Endpoints](#api-endpoints)
10. [Order Queue Workflow](#order-queue-workflow)
11. [Postman Collection](#postman-collection)
12. [Changelog](#changelog)
13. [AI Usage Disclosure](#ai-usage-disclosure)
14. [Reference List](#reference-list)

---

## Project Overview

CoffeeNChill is a campus canteen that struggles with long queues during lecture breaks. Part 1 stores menu items in Azure Table Storage and currently stores staff PDFs in Azure Blob Storage through HTTP-triggered Azure Functions. **Assignment gap to resolve:** the brief specifies Azure File Share; this ZIP uses Blob Storage instead, so this part does not yet match the required storage service.

**Part 2** solves the peak-hour problem. Instead of processing orders synchronously (which crashes under load), orders are placed on an **Azure Storage Queue**. A background **queue-triggered Azure Function** picks up each message, records the order in an **Orders** Azure Table and moves it through the barista lifecycle:

`Received → Preparing → Ready → Collected`

The whole stack (Azurite + Azure Functions host) starts with a single command using **Docker Compose**, pulling images directly from **Docker Hub**.

---

## Video Demonstration

> **Part 2 video (unlisted YouTube):** [ADD YOUR YOUTUBE LINK HERE](https://www.youtube.com/watch?v=REPLACE_ME)

The video shows:

- The single-command `docker-compose up` startup, with images pulled from Docker Hub
- Messages being placed onto `order-processing-queue`
- The queue trigger firing and the Orders table being updated
- Postman collection execution and status verification
- A voiceover explaining the code architecture and design decisions

> Part 1 video: [ADD PART 1 LINK HERE](https://www.youtube.com/watch?v=REPLACE_ME)

---

## Group Member Contributions

| Person | Main responsibility |
|---|---|
| Nikshay (ST10473535) | Order queue, queue trigger, Table Storage, status API and final README/video |
| Sena (ST10469318) | Docker, Docker Compose, Azurite, Postman, Docker Hub and testing documentation |

Individual commit history can be verified on the GitHub repository's commit log (minimum of five meaningful commits per student).

---

## Architecture

```
                 ┌──────────────────────────────────────────────┐
                 │               Docker Compose                 │
                 │                                              │
  Client /       │   ┌──────────────────┐   ┌────────────────┐  │
  Postman  ─────────▶│  Azure Functions │──▶│    Azurite     │  │
 (port 7071)     │   │  Host (v2.0)     │   │ Tables / Blobs │  │
                 │   │                  │   │    / Queues    │  │
                 │   │ HTTP triggers    │   └────────────────┘  │
                 │   │ Queue trigger    │                       │
                 │   └──────────────────┘                       │
                 └──────────────────────────────────────────────┘

 POST /api/orders/queue ──▶ order-processing-queue ──▶ ProcessOrderQueue ──▶ Orders table
```

**Storage used:**

| Resource | Type | Purpose |
|---|---|---|
| `MenuItems` | Table | Menu data (Part 1) |
| `staff-docs` | File Share | Staff documents (Part 1) |
| `order-processing-queue` | Queue | Incoming orders (Part 2) |
| `order-processing-queue-poison` | Queue | Messages that repeatedly fail processing (Part 2) |
| `Orders` | Table | Order records and status (Part 2) |

---

## Repository Structure

> Adjust this to match your actual folder names before submitting.

```
.
├── docker-compose.yml          # Single-command orchestration (root)
├── README.md
├── docs/
│   └── CoffeeNChill.postman_collection.json
├── CoffeeNChill.Functions/
│   ├── Dockerfile
│   ├── Functions/              # HTTP + Queue triggered functions
│   ├── Models/
│   ├── Services/
│   ├── host.json
│   └── local.settings.json
└── .gitignore
```

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine + Compose plugin)
- [Postman](https://www.postman.com/downloads/)
- Internet access (to pull images from Docker Hub)
- *(Optional, for development)* .NET SDK and Azure Functions Core Tools

---

## Quick Start (Docker Compose)

1. **Clone the repository**

   ```bash
   git clone <YOUR_GITHUB_REPO_URL>
   cd <YOUR_REPO_FOLDER>
   ```

2. **Start the full stack with one command**

   ```bash
   docker-compose up
   ```

   Add `-d` to run in the background:

   ```bash
   docker-compose up -d
   ```

3. **Confirm the containers are running**

   ```bash
   docker ps
   ```

   You should see the Azurite container and the Functions container.

4. **Call the API** at `http://localhost:7071/api/...`

5. **Stop the stack**

   ```bash
   docker-compose down
   ```

### Example `docker-compose.yml`

> Replace `<dockerhub_username>` with your Docker Hub username. Images are **pulled from Docker Hub** and not built locally.

```yaml
services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    container_name: coffeenchill-azurite
    command: azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0 --loose
    ports:
      - "10000:10000"   # Blob
      - "10001:10001"   # Queue
      - "10002:10002"   # Table
    volumes:
      - azurite-data:/data
    networks:
      - coffeenchill-net

  functions:
    image: <dockerhub_username>/coffeenchill-functions:v2.0
    container_name: coffeenchill-functions
    depends_on:
      - azurite
    ports:
      - "7071:80"
    environment:
      AzureWebJobsStorage: "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;"
      FUNCTIONS_WORKER_RUNTIME: "dotnet-isolated"
    networks:
      - coffeenchill-net

volumes:
  azurite-data:

networks:
  coffeenchill-net:
    driver: bridge
```

> **Note:** Inside Docker Compose the Functions container must reach Azurite through the service name (`azurite`), not `127.0.0.1`. Confirm the port mapping matches your Azurite version (Blob 10000, Queue 10001, Table 10002 is the Azurite default).

---

## Docker Hub Images

| Image | Tag | Link |
|---|---|---|
| Functions app | `part2` | `https://hub.docker.com/r/senandlovu/coffeenchill-functions` |
| Azurite | `latest` | `https://hub.docker.com/r/microsoft/azure-storage/azurite` (official image is published at `mcr.microsoft.com/azure-storage/azurite`) |

The Compose file references the published Functions image and does not build it locally. Before submission, verify that `senandlovu/coffeenchill-functions:part2` is publicly pullable and matches the source in this repository. The marker must be able to pull the image without your local build cache.

---

## API Endpoints

Base URL (Docker Compose): `http://127.0.0.1:18080`

### Orders (new in Part 2)

| Method | Route | Description |
|---|---|---|
| POST | `/api/orders/queue` | Validates the order and places it on `order-processing-queue` |
| GET | `/api/orders` | Lists orders and their current status *(adjust to your implementation)* |
| GET | `/api/orders/{orderDate}/{orderId}` | Returns the status of one order *(adjust to your implementation)* |

**Sample request body for `POST /api/orders/queue`:**

```json
{
  "OrderId": "ORD-2026-8801",
  "CustomerName": "Jane Smith",
  "SelectedItemSKUs": ["COF-001", "PAS-104"],
  "TotalPrice": 65.00,
  "OrderTimestamp": "2026-10-12T10:15:00Z"
}
```

### Menu (from Part 1)

| Method | Route | Description |
|---|---|---|
| POST | `/api/menu` | Create a menu item |
| GET | `/api/menu` | Get all menu items |
| GET | `/api/menu/category/{category}` | Get items by category |
| PUT | `/api/menu/{category}/{id}` | Update price or availability |
| DELETE | `/api/menu/{category}/{id}` | Delete a menu item |

### Staff Documents (from Part 1)

| Method | Route | Description |
|---|---|---|
| POST | `/api/documents/upload` | Upload a file (multipart/form-data) to `staff-docs` |
| GET | `/api/documents` | List files with name, size and last modified date |
| GET | `/api/documents/download/{fileName}` | Download a file |

---

## Order Queue Workflow

1. **Placement:** A client sends `POST /api/orders/queue`. The function validates the fields (OrderId, CustomerName, SelectedItemSKUs, TotalPrice), generates or checks the timestamp, serializes the order to JSON and pushes it onto `order-processing-queue`. The client receives a response immediately and is not blocked while the order is prepared.
2. **Trigger:** `ProcessOrderQueue` (`[QueueTrigger("order-processing-queue")]`) fires automatically when a message arrives.
3. **Parsing:** The JSON payload is deserialized into an `Order` object.
4. **Recording:** An entity is written to the `Orders` table:
   - `PartitionKey` = order date
   - `RowKey` = `OrderId`
   - `Status` = `Received`
5. **Lifecycle:** The function simulates the barista workflow, updating the entity through `Received → Preparing → Ready → Collected`.
6. **Failure handling:** If processing fails repeatedly, the message moves to the poison queue (`order-processing-queue-poison`) and the exception is logged.

---

## Postman Collection

The collection is exported to [`/docs/CoffeeNChill.postman_collection.json`](docs/CoffeeNChill.postman_collection.json).

**How to use:**

1. Run `docker-compose up`.
2. Open Postman and import the JSON file.
3. Set the `{{baseUrl}}` variable to `http://localhost:7071`.
4. Run the collection (Collection Runner) in order.

**Folders:**

- **Menu**: CRUD and category filtering (Part 1)
- **Documents**: upload, list, download (Part 1)
- **Orders**: place an order on the queue, then check the Orders table for status updates (Part 2)

All new Part 2 requests include sample bodies and automated test scripts.

---

## Changelog

### v2.0: Part 2 (Queue Triggers & Docker Compose)

**Added**

- `POST /api/orders/queue` producer endpoint with field validation and JSON serialization
- `ProcessOrderQueue` queue-triggered Azure Function
- `Orders` Azure Table with order lifecycle status (`Received → Preparing → Ready → Collected`)
- Poison queue handling and exception logging
- Order status endpoints
- `docker-compose.yml` orchestrating Azurite and the Functions host using Docker Hub images
- Custom bridge network and a volume for Azurite data
- Updated Postman collection covering queue placement and Orders table status checks
- New Docker Hub image tag `coffeenchill-functions:v2.0`

**Changed**

- README restructured with changelog, contributions and Compose instructions
- Functions app configuration updated to reach Azurite via the Compose service name

### v1.0: Part 1 (Azure Functions, Tables, Files & Docker Hub)

- Menu CRUD HTTP functions backed by Azure Table Storage
- Staff PDF upload, list and download currently backed by Azure Blob Storage; migrate to Azure File Share to meet the brief
- Standalone Dockerfile and image published to Docker Hub (`v1.0`)
- Postman collection for Part 1 endpoints

---



## AI Usage Disclosure

AI assistance was used during the review and repair process to help inspect the repository, compare the implementation with the supplied assignment brief, identify likely defects, suggest code changes, and improve documentation. The student/team must review every change, run the application and tests, understand the final implementation, and edit this disclosure to accurately describe the assistance actually used before submission. AI-generated code must not be submitted as a substitute for the student's own analysis or understanding. Any externally sourced or adapted code must be attributed separately in the relevant source file and in the reference list.

## Code Attribution

The repository should identify any code copied or adapted from third-party sources, including the source author/organisation, title, URL, date (where available), and the date accessed, using the IIE Harvard Anglia format required by the module. The existing reference list below records learning resources; it does not by itself prove that code was copied from those resources. Add source-specific attributions only for material actually used.

## Reference List

*A beginners guide to azure file shares*. 2024. [Video]. YouTube. Available at: <https://www.youtube.com/watch?v=mB5pSRuSgUo> [Accessed 11 October 2026].

*Azure functions | triggers and bindings | common scenarios | advantages | for beginners*. 2022. [Video]. YouTube. Available at: <https://www.youtube.com/watch?v=xLgba_DHpFs&list=PLBEm2Vv2nD-OqE6at0_Ew1wW7NpgBdWo1> [Accessed 11 October 2026].

*Azure queue storage tutorial*. 2020. [Video]. YouTube. Available at: <https://www.youtube.com/watch?v=JQ6KhjU5Zsg> [Accessed 11 October 2026].

*Azure Table Storage Tutorial | Easy and scalable NoSQL database*. 2020. [Video]. YouTube. Available at: <https://www.youtube.com/watch?v=HSL1poL1VR0> [Accessed 11 October 2026].

*Container basics: Part 9 - orchestration with docker compose*. 2021. [Video]. YouTube. Available at: <https://www.youtube.com/watch?v=yfLb_ga2wDE> [Accessed 11 October 2026].

*The only docker tutorial you need to get started*. 2025. [Video]. YouTube. Available at: <https://www.youtube.com/watch?v=DQdB7wFEygo> [Accessed 11 October 2026].
