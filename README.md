# CoffeeNChill: CLDV6212 Cloud Development (Parts 1, 2 and 3)

| | |
|---|---|
| **Live API (Swagger)** | **TBC: `http://<vps-ip>:5000/swagger`**, added once the VPS is running |
| **Part 3 video (unlisted)** | **TBC**, added after recording |
| Part 2 video | <https://youtu.be/r_DwcSN_6zo> |
| GitHub repository | <https://github.com/Mandlakazi07/CoffeeNChill> |
| Docker Hub: gateway | <https://hub.docker.com/r/mandlakazi07/coffeenchill-gateway> (tag `part3`) |
| Docker Hub: Functions app | <https://hub.docker.com/r/mandlakazi07/coffeenchill-functions> (tag `part2`) |

CoffeeNChill is a canteen-ordering backend. Part 3 puts an ASP.NET Core API gateway in front of the Azure Functions app from Parts 1 and 2. The gateway handles login with JWTs, role-based access, validation and errors. Everything runs in Docker containers on a Linux VPS, and a GitHub Actions pipeline builds, tests and deploys it.

## Contents

- [Students](#students)
- [Changelog](#changelog)
- [Architecture](#architecture)
- [API endpoints](#api-endpoints)
- [Setup](#setup)
  - [Run locally with Docker](#run-locally-with-docker)
  - [Configuration (.env)](#configuration-env)
  - [Deploy to the VPS](#deploy-to-the-vps)
- [Docker Hub images](#docker-hub-images)
- [Testing](#testing)
- [CI/CD pipeline](#cicd-pipeline)
- [Known limitations](#known-limitations)
- [Who worked on what](#who-worked-on-what)
- [AI use declarations](#ai-use-declarations)
- [Code attribution and references](#code-attribution-and-references)

## Students

| Student name | Student number |
|---|---|
| Mandlakazi Fezile Sibiya | ST10473547 |
| Jarrad Rickson | ST10478657 |
| Tyrone Seamark | ST10470368 |

## Changelog

### Part 3: API gateway, security, Docker Hub, VPS and CI/CD (November 2026)

- Added the `CoffeeNChillGateway` ASP.NET Core Web API project as the single front door to the system.
- Registration and login, with passwords stored as BCrypt hashes in a `Users` table in Azure Table Storage (PartitionKey = role, RowKey = email).
- Login returns a JWT with name, email and role claims. The signing key, issuer and audience come from configuration (`.env`), never from code.
- Role-based access: anyone can read the menu, only Staff and Admin can change it. Public sign-up only creates Customers. Staff and Admin accounts can only be created by an Admin, and the first Admin is created at startup from `.env`.
- DTOs with validation for every request and response, plus global exception middleware so every error has the same JSON shape.
- Swagger documents every endpoint and response, with an Authorize button for the token. It is on in every environment, so it also works on the live server.
- Unit tests for the security logic (`CoffeeNChillGateway.Tests`).
- A multi-stage Dockerfile for the gateway. `docker-compose.yml` now starts Azurite, the Functions app and the gateway together, pulling the images from Docker Hub.
- Part 3 Postman collection in `docs/postman/`: public access, login for each role, and the 401 and 403 cases.
- GitHub Actions pipeline: build and test, push the images to Docker Hub, deploy to the VPS.
- VPS setup script (`scripts/setup-vps.sh`): Docker, start on boot, firewall.

### Part 2: Queue-based order processing (October 2026)

- `POST /api/orders/queue` validates an order and puts it on `order-processing-queue` (202 Accepted).
- The `ProcessOrderQueue` queue-triggered function moves each order through `Received → Preparing → Ready → Collected` and saves it in the `Orders` table, with retry and poison-queue handling.
- `GET /api/orders/{orderId}` and `PUT /api/orders/{orderId}/status`, plus date-based order reads.
- Dockerfile for the Functions app and `docker-compose.yml` running it with Azurite. Image published as `mandlakazi07/coffeenchill-functions:part2`.
- Postman collections, test results, and API documentation for the order endpoints.

### Part 1: Menu and staff documents (October 2026)

- Azure Functions (isolated worker, .NET) for the menu in Azure Table Storage (PartitionKey = category, RowKey = item id).
- Staff document upload, list and download.
- Postman collection for the Part 1 endpoints.

## Architecture

```text
Client (browser / Postman / phone)
        |
        |  HTTP :5000  (the only port open to the internet)
        v
+---------------------------+         Docker network (coffeenchill-part2-network)
| gateway                   |
| ASP.NET Core Web API      |----> Azurite (Tables: Users)
| JWT, roles, validation,   |
| errors, Swagger           |----> functions (Azure Functions app, Parts 1 and 2)
+---------------------------+              |
                                           v
                               Azurite (MenuItems, Orders tables,
                               order-processing-queue, blobs)
```

All three containers run on the VPS. Only the gateway port is published publicly. Azurite and the Functions app are bound to `127.0.0.1`, so clients have to go through the gateway.

## API endpoints

### Gateway (Part 3), base URL `http://<host>:5000`

| Method | Route | Who can call it | Responses |
|---|---|---|---|
| `POST` | `/api/auth/register` | Anyone (creates a Customer) | 201, 400, 403 (asked for Staff/Admin), 409 |
| `POST` | `/api/auth/login` | Anyone | 200 with a JWT, 400, 401 |
| `POST` | `/api/auth/users` | Admin | 201, 400, 401, 403, 409 |
| `GET` | `/api/menu` | Anyone | 200 |
| `GET` | `/api/menu/{id}` | Anyone | 200, 404 |
| `POST` | `/api/menu` | Staff, Admin | 201, 400, 401, 403 |
| `PUT` | `/api/menu/{id}` | Staff, Admin | 200, 400, 401, 403, 404 |
| `DELETE` | `/api/menu/{id}` | Staff, Admin | 204, 401, 403, 404 |

Send the token as `Authorization: Bearer <token>`, or click **Authorize** in Swagger and paste it. Every error looks like this:

```json
{ "status": 403, "error": "Forbidden", "message": "Your role is not allowed to do this.", "traceId": "00-..." }
```

Validation errors also include an `errors` object naming each field.

### Functions app (Parts 1 and 2), internal on the Docker network

| Method | Route | Purpose |
|---|---|---|
| `POST` / `GET` | `/api/menu` | Create / list menu items |
| `GET` | `/api/menu/category/{category}` | Menu items in a category |
| `PUT` / `DELETE` | `/api/menu/{category}/{id}` | Update / delete a menu item |
| `POST` | `/api/documents/upload` | Upload a staff document |
| `GET` | `/api/documents`, `/api/documents/download/{fileName}` | List / download staff documents |
| `POST` | `/api/orders/queue` | Queue an order (202 Accepted) |
| `GET` | `/api/orders/{orderId}` | Order status |
| `PUT` | `/api/orders/{orderId}/status` | Update order status |

## Setup

You need [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine with the Compose plugin). To build and test without Docker you also need the .NET 10 SDK.

### Run locally with Docker

```bash
git clone https://github.com/Mandlakazi07/CoffeeNChill.git
cd CoffeeNChill
cp .env.example .env
```

Open `.env` and fill in:

- `JWT_KEY`: a random key of at least 32 characters. Generate one with `openssl rand -base64 48`. In PowerShell: `$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)`.
- `ADMIN_EMAIL` and `ADMIN_PASSWORD`: the first Admin account (password at least 8 characters).

Then start everything, either from the Docker Hub images:

```bash
docker compose up -d
```

or built from the source in this repository:

```bash
docker compose -f docker-compose.yml -f docker-compose.build.yml up -d --build
```

Open <http://localhost:5000/swagger>. Log in with `POST /api/auth/login` using the Admin from `.env`, click **Authorize**, and paste the token.

Stop with `docker compose down`. Add `-v` to also delete the stored data.

### Configuration (.env)

`.env` is git-ignored and never committed. `.env.example` lists every setting with placeholder values.

| Variable | Required | Purpose |
|---|---|---|
| `JWT_KEY` | Yes | JWT signing key (32+ characters). Compose will not start without it. |
| `JWT_ISSUER` / `JWT_AUDIENCE` | No | Token issuer and audience (defaults `CoffeeNChillGateway` / `CoffeeNChillClients`). |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | Yes, to create Staff | First Admin account, created at startup if it does not exist. |
| `GATEWAY_PORT` | No | Public port (default `5000`). |
| `DOCKERHUB_NAMESPACE`, `GATEWAY_TAG`, `FUNCTIONS_TAG` | No | Which Docker Hub images to pull. |

More detail: [docs/part3-docker-and-postman.md](docs/part3-docker-and-postman.md).

### Deploy to the VPS

On a fresh Ubuntu VPS, as the user that will run the app:

```bash
curl -fsSLO https://raw.githubusercontent.com/Mandlakazi07/CoffeeNChill/main/scripts/setup-vps.sh
ADMIN_EMAIL=you@example.com ADMIN_PASSWORD='a-long-password' bash setup-vps.sh
```

If the repository is private, copy [scripts/setup-vps.sh](scripts/setup-vps.sh) to the server with `scp` instead of `curl`. The script does four things:

- installs Docker and makes it start on boot
- adds the user to the `docker` group
- turns on the firewall with only SSH and port 5000 open
- creates `~/coffeenchill/.env` with a random JWT key

You also need to open port 5000 in the cloud provider's firewall.

Then either let the [CI/CD pipeline](#cicd-pipeline) deploy, or deploy by hand:

```bash
cd ~/coffeenchill
# copy docker-compose.yml from this repository into this folder, then:
docker compose pull
docker compose up -d
```

Every container has `restart: unless-stopped` and Docker starts on boot, so the app comes back by itself after the server restarts.

## Docker Hub images

| Image | Tag | Built from |
|---|---|---|
| [mandlakazi07/coffeenchill-gateway](https://hub.docker.com/r/mandlakazi07/coffeenchill-gateway) | `part3`, plus one tag per commit | `CoffeeNChillGateway/Dockerfile` |
| [mandlakazi07/coffeenchill-functions](https://hub.docker.com/r/mandlakazi07/coffeenchill-functions) | `part2` | `Dockerfile` |

`docker-compose.yml` pulls these images, so the VPS never needs the source code.

## Testing

- **Unit tests**: `dotnet test CoffeeNChillGateway.Tests/CoffeeNChillGateway.Tests.csproj`. These cover:
  - JWT: claims, wrong signing key, tampered role, expired token, wrong audience, missing or short key
  - BCrypt hashing
  - the rule for who can create Staff and Admin accounts
  - DTO validation
  - register and login, including failure cases
  - the error middleware and the Admin seeding
- **Postman**: import both files in [docs/postman/](docs/postman/). Choose the *CoffeeNChill Part 3* environment, set `baseUrl` (local or the VPS URL) and fill in `adminEmail` / `adminPassword`; these are blank in the committed file. Run the collection from the top. It covers public access, login for each role, 401 and 403 cases, validation errors and the menu changes.
- **From another network**: open the live Swagger URL on a phone using mobile data.

Test results are recorded in [docs/part3-docker-and-postman.md](docs/part3-docker-and-postman.md#test-results).

## CI/CD pipeline

[.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml) runs on every push and pull request:

1. **Build and test**: compiles both projects, runs the unit tests, builds the Docker images, starts the whole stack, and runs the Postman collection against it with Newman.
2. **Push images to Docker Hub** (pushes to `main` only).
3. **Deploy to the VPS** (pushes to `main` only): copies `docker-compose.yml` over SSH, pulls the new images, restarts the stack, and checks the live Swagger URL responds.

The Docker Hub token and the VPS address, user and SSH key are stored as GitHub Actions secrets, never in the repository. The secrets are listed in [docs/part3-docker-and-postman.md](docs/part3-docker-and-postman.md#secrets-to-add).

## Known limitations

- The gateway's menu is kept in memory (it resets when the container restarts). It does not yet read the Functions app's `MenuItems` table.
- The order and staff-document routes are not exposed through the gateway yet. They are still available on the Functions app inside the Docker network.
- Part 1 staff documents use Blob Storage, while the brief asked for an Azure File Share.

## Who worked on what

Based on the commit history. Each member must check their own row before submission. **TBC** marks the parts that only that member can fill in.

| Member | Parts 1 and 2 | Part 3 |
|---|---|---|
| Mandlakazi Fezile Sibiya | Combined Part 1 and 2 implementation, order endpoints, queue retry and failure handling, Docker/Azurite/Postman evidence, Functions image on Docker Hub, README, Part 2 video | Created the gateway project and added its packages; repository and Docker Hub owner. **TBC** |
| Jarrad Rickson | Order queue producer hardening (payload, validation, error handling); order API documentation, order lifecycle, Postman checklist, negative test cases, test results, end-to-end testing guide | Docker and integration (Person 2): gateway Dockerfile, three-service Docker Compose, Part 3 Postman collection, CI/CD pipeline (bonus), VPS setup script, README, video plan |
| Tyrone Seamark | README updates. **TBC** | API gateway and security (Person 1): Users table entity and auth controller, then JWT, BCrypt, roles, DTOs, exception middleware, Swagger and unit tests. **TBC** |

## AI use declarations

**Mandlakazi Fezile Sibiya:** Parts 1 and 2: ChatGPT (OpenAI, 2026) was used to explain errors, suggest code and configuration changes, and help with reference formatting (see [docs/CODE_ATTRIBUTION.md](docs/CODE_ATTRIBUTION.md)). Part 3: *to be completed by Mandlakazi.*

**Jarrad Rickson:** AI used to understand questions in assignment and outline which roles each user was to take. Ai was used to draft readMe.md

**Tyrone Seamark:** *To be completed by Tyrone.*

## Code attribution and references

Every file that uses an outside source has an attribution banner with the title, author, date, version and link. The in-text citations are explained in [docs/CODE_ATTRIBUTION.md](docs/CODE_ATTRIBUTION.md).

### Reference list (IIE Harvard Anglia)

Docker. n.d.-a. *Compose file reference*. [online]. Available at: https://docs.docker.com/reference/compose-file/ [Accessed 12 October 2026].

Docker. n.d.-b. *Define and manage networks in Docker Compose*. [online]. Available at: https://docs.docker.com/reference/compose-file/networks/ [Accessed 12 October 2026].

Docker. n.d.-c. *Define and manage volumes in Docker Compose*. [online]. Available at: https://docs.docker.com/reference/compose-file/volumes/ [Accessed 12 October 2026].

Docker. n.d.-d. *Multi-stage builds*. [online]. Available at: https://docs.docker.com/build/building/multi-stage/ [Accessed 10 November 2026].

Docker. n.d.-e. *Interpolation*. [online]. Available at: https://docs.docker.com/reference/compose-file/interpolation/ [Accessed 10 November 2026].

Docker. n.d.-f. *Fragments*. [online]. Available at: https://docs.docker.com/reference/compose-file/fragments/ [Accessed 10 November 2026].

Docker. n.d.-g. *Merge Compose files*. [online]. Available at: https://docs.docker.com/reference/compose-file/merge/ [Accessed 10 November 2026].

Docker. n.d.-h. *Install Docker Engine on Ubuntu*. [online]. Available at: https://docs.docker.com/engine/install/ubuntu/ [Accessed 10 November 2026].

Docker. n.d.-i. *Linux post-installation steps for Docker Engine*. [online]. Available at: https://docs.docker.com/engine/install/linux-postinstall/ [Accessed 10 November 2026].

EMKNDN. 2026. *emkndn-cldv6212-2026-cldv6212-poe-ndlovu-sena* [Source code]. Available at: https://github.com/EMKNDN/emkndn-cldv6212-2026-cldv6212-poe-ndlovu-sena [Accessed 12 October 2026].

GitHub. n.d. *Workflow syntax for GitHub Actions*. [online]. Available at: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax [Accessed 10 November 2026].

Microsoft. n.d.-a. *Azure Queue storage trigger and bindings for Azure Functions overview*. [online]. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue [Accessed 12 October 2026].

Microsoft. n.d.-b. *Azure Queue storage trigger for Azure Functions*. [online]. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger [Accessed 12 October 2026].

Microsoft. n.d.-c. *Guide for running C# Azure Functions in an isolated worker process*. [online]. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide [Accessed 12 October 2026].

Microsoft. n.d.-d. *TableClient Class (Azure.Data.Tables)*. [online]. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient?view=azure-dotnet [Accessed 12 October 2026].

Microsoft. n.d.-e. *Breaking change: Default ASP.NET Core port changed from 80 to 8080*. [online]. Available at: https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port [Accessed 10 November 2026].

Microsoft. n.d.-f. *Breaking change: New non-root 'app' user in Linux images*. [online]. Available at: https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/app-user [Accessed 10 November 2026].

OpenAI. 2026. *ChatGPT (GPT-6)*. [Large language model]. Available at: https://chatgpt.com/ [Accessed 12 October 2026].

The Independent Institute of Education (The IIE). 2025a. *Guidelines for Responsible AI Use at The IIE*. [PDF]. Available at: https://irp.cdn-website.com/271d35b6/files/uploaded/Guidelines_for_Responsible_AI_Use_at_The_IIE.pdf [Accessed 12 October 2026].

The Independent Institute of Education (The IIE). 2025b. *IIE Quick Reference Guide 2025: Harvard-Anglia Style Reference Guide, Adapted for The IIE*. [PDF]. Available at: https://irp.cdn-website.com/271d35b6/files/uploaded/The_IIE_Harvard_Anglia_Style_Reference_Guide_2025.pdf [Accessed 12 October 2026].
