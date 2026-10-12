# CoffeeNChill — Combined CLDV6212 Part 1 and Part 2 Submission


## Student and repository

| Field | Details |
|---|---|
| Submission type | Group Assignment - Part 1 and Part 2 |
| Student name | **Mandlakazi Fezile Sibiya** | **Jarrad Rickson** | **Tyrone Seamark** |
| Student number | **ST10473547** | **ST10478657** | **ST10470368** |
| GitHub repository | [Mandlakazi07/CoffeeNChill](https://github.com/Mandlakazi07/CoffeeNChill) |
| Module | CLDV6212 — Cloud Development |
| Demonstration video | **https://youtu.be/r_DwcSN_6zo** |

## Project overview

CoffeeNChill is a canteen-ordering backend built with Azure Functions and Azure Storage. The combined project brings the menu and staff-document endpoints from Part 1 together with the asynchronous order queue workflow from Part 2.

The repository contains HTTP-triggered Azure Functions, storage services and models, Postman request collections, a Dockerfile, and a Docker Compose configuration for running Azure Functions alongside Azurite.

### Part 1 — Menu and staff documents

Menu operations use Azure Table Storage. The intended menu schema uses the menu category as the `PartitionKey` and the item identifier/SKU as the `RowKey`.

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/menu` | Create a menu item |
| `GET` | `/api/menu` | List menu items |
| `GET` | `/api/menu/category/{category}` | List menu items in a category |
| `PUT` | `/api/menu/{category}/{id}` | Update a menu item |
| `DELETE` | `/api/menu/{category}/{id}` | Delete a menu item |
| `POST` | `/api/documents/upload` | Upload a staff document (currently implemented with Blob Storage) |
| `GET` | `/api/documents` | List staff documents |
| `GET` | `/api/documents/download/{fileName}` | Download a staff document |

**Known Part 1 requirement gap:** the supplied assignment brief specifies an Azure File Share (for example, `staff-docs`) for staff documents. The current source code uses `Azure.Storage.Blobs` and a Blob container instead. The endpoint names and some comments refer to files/file shares, but that does not make the implementation Azure Files. This must be migrated and tested before claiming full compliance with the File Share requirement.

### Part 2 — Queue-based order processing

The Part 2 flow is designed to accept an order over HTTP, enqueue a JSON message, process it with a Queue-triggered Azure Function, and persist order data in the `Orders` table.

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/orders/queue` | Validate and enqueue an order; expected response is `202 Accepted` when the message is accepted |
| `GET` | `/api/orders/{orderId}` | Retrieve an order's current status |
| `PUT` | `/api/orders/{orderId}/status` | Update an order status using a JSON `status` field |

The queue-trigger function listens to `order-processing-queue`. The intended lifecycle is `Received → Preparing → Ready → Collected`. The current implementation updates the statuses in sequence in the background function; actual runtime behavior still needs to be demonstrated with the Functions host and storage emulator.

## Architecture and storage

```text
Postman / HTTP client
        |
        v
Azure Functions (HTTP triggers)
   |                     |
   | Menu operations     | POST /api/orders/queue
   v                     v
MenuItems table     order-processing-queue
                          |
                          v
                 ProcessOrderQueue function
                          |
                          v
                     Orders table

Staff document endpoints currently use Blob Storage (not Azure File Share).
```

In Docker Compose, the Functions container is configured to connect to Azurite through the Compose service name `azurite`. The Compose file exposes the API at `http://127.0.0.1:18080` and the local Azurite ports at `11000` (Blob), `11001` (Queue) and `11002` (Table).

## Repository structure

```text
.
├── CoffeeNChillFunctions.slnx
├── CoffeeNChillFunctions/
│   ├── DTOs/
│   ├── Functions/
│   │   ├── Documents/
│   │   ├── Menu/
│   │   └── Orders/
│   ├── Models/
│   ├── Services/
│   ├── Validation/
│   ├── Postman/
│   ├── Program.cs
│   └── host.json
├── Postman/
├── docs/
├── Dockerfile
├── docker-compose.yml
├── CoffeeNChill-Part1.postman_collection.json
└── README.md
```

## Prerequisites

- .NET SDK compatible with the project's target framework (`net10.0` in the current project file).
- Azure Functions Core Tools v4 for running the Functions host locally.
- Azurite for local Azure Storage emulation.
- Postman for endpoint tests.
- Docker Desktop and Docker Compose if using the Compose deployment path.

## Build verification

The project was built on Windows using:

```powershell
dotnet build .\CoffeeNChillFunctions\CoffeeNChillFunctions.csproj
```

The build output provided during development reported **Build succeeded**. This verifies compilation only; it does not establish that the Functions host starts, storage calls succeed, the queue trigger processes messages, or all Postman requests pass. See [`docs/VERIFICATION_STATUS.md`](docs/VERIFICATION_STATUS.md) for the current test status.

## Run with Docker Compose

Docker Desktop must be running before using Docker commands. The local Docker engine had startup trouble during preparation, so the runtime workflow has not yet been verified on the submission machine.

The Compose file references the published image `senandlovu/coffeenchill-functions:part2`. To ensure that the container uses the source checked out in this repository, build that image locally first, then start Compose without pulling a replacement image:

```powershell
docker build -t senandlovu/coffeenchill-functions:part2 .
docker compose up -d --pull never
docker compose ps
docker compose logs functions --tail 100
```

API base URL: `http://127.0.0.1:18080`.

Stop the containers with:

```powershell
docker compose down
```

If a current source image is published to Docker Hub, record the exact tag and verify that it is publicly pullable. Merely referencing a Docker Hub image in Compose does not publish the source that is in this repository.

## Docker, setup and troubleshooting
Focus: helping an examiner run the project
This person can independently prepare operational documentation:
1. Write a local setup guide covering prerequisites and how to start the application with Docker Compose.
2. Explain the Functions and Azurite services, ports, network and persistent volume using the existing Compose configuration.
3. Create a troubleshooting guide for container startup, connection and queue-processing problems.
4. Document how to inspect application logs safely.
5. Create a repository guide explaining the purpose of the main folders and files.
6. Prepare a final deployment and submission checklist covering Docker Hub tags, GitHub, Postman and the demonstration video.
Suggested commit style: docs: add Docker Compose setup and troubleshooting guide

## Postman

The repository includes a JSON collection at the root (`CoffeeNChill-Part1.postman_collection.json`) and additional Postman exports under `Postman/`. Import the collection that matches the current endpoint routes and set its base URL to `http://127.0.0.1:18080` when using the provided Compose port mapping.

Run and record the relevant success and validation/error requests. Retain evidence such as response status codes, response bodies, and Function logs. Do not mark a request as passing merely because it exists in the collection.

## Individual submission, reuse and contribution disclosure

This repository is being maintained as an **individual combined submission** by the student named in the table above. It brings Part 1 and Part 2 material into one repository; it should not be interpreted as a declaration that every existing source file was originally written independently for this individual submission.

Some implementation and project structure were carried forward or adapted from the earlier shared group Part 1/Part 2 project: [the original group repository](https://github.com/EMKNDN/emkndn-cldv6212-2026-cldv6212-poe-ndlovu-sena/branches). The imported source ZIP did not include Git history, so the precise original author of every line or file could not be independently verified from that snapshot. Before submission, the student should compare files against the original repository and add specific attribution where required by the module's academic-integrity rules. The group work must not be represented as entirely new individual work.

The individual work on this combined repository should be described accurately and only after completion. Evidence currently available confirms that the project was copied into this repository, committed and pushed, and that the .NET project compiled. Runtime API, storage and queue testing remains outstanding.

## Generative AI disclosure

**An AI assistant (ChatGPT by OpenAI) was used during preparation to inspect the supplied source snapshot, compare files against the stated requirements, suggest and prepare code/documentation revisions, draft this README and a demonstration script, and provide Git/PowerShell troubleshooting steps. AI assistance contributed to the review and revision process; the submitted code must not be described as wholly unaided original work.**

**I,the student am responsible for understanding the submitted implementation, verifying all suggested changes, conducting the required runtime tests, and ensuring this disclosure and any code attribution meet the institution's current rules. At the time of this document revision, the available local evidence shows a successful .NET build, but not a successful end-to-end runtime test. The unresolved Azure File Share gap is called out above rather than being presented as complete.**

## References and attribution

- EMKNDN. (n.d.) *CLDV6212 CoffeeNChill group repository*. Available at: <https://github.com/EMKNDN/emkndn-cldv6212-2026-cldv6212-poe-ndlovu-sena/branches> (Accessed: 11 October 2026).
- Microsoft. (n.d.) *Develop Azure Functions locally by using Core Tools*. Available at: <https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local> (Accessed: 11 October 2026).
- Microsoft. (n.d.) *Install and run the Azurite emulator for Azure Storage*. Available at: <https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite> (Accessed: 11 October 2026).

Use the IIE Harvard Anglia format required by the module and include additional references for any external code actually copied or adapted. Only list sources that were genuinely consulted; update author/date/access details as needed before submission.

## Code attribution and technical references

External source attribution, IIE Harvard Anglia references and the AI-use disclosure are documented in [docs/CODE_ATTRIBUTION.md](docs/CODE_ATTRIBUTION.md).
