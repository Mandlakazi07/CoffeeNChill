# Part 3: Docker and Postman

**Role:** Person 2, Docker and integration
**Last updated:** 8 November 2026

This covers how the Part 3 stack is containerised, how to run it locally and on the VPS, the Postman collection for the gateway, and what the test run actually showed. It is based on the `part-3` branch as of commit `73680fd` plus the fixes listed below.

## What runs where

```text
                    Internet / Postman / Swagger
                               |
                     :5000 (public, GATEWAY_PORT)
                               |
                 +-------------v--------------+
                 |  gateway  (ASP.NET Core)   |   coffeenchill-part3-gateway
                 |  JWT auth, roles, Swagger  |
                 +------+--------------+------+
                        |              |
          Users table   |              |  FunctionsApi__BaseUrl = http://functions:80
                        |              |  (not used by the gateway yet)
                 +------v-----+  +-----v--------------------+
                 |  azurite   |<-+  functions (Part 1 + 2)  |   coffeenchill-part2-functions
                 | blob/queue/|  |  menu, documents, orders |   127.0.0.1:18080 only
                 |   table    |  +--------------------------+
                 +------------+   coffeenchill-part2-azurite, 127.0.0.1:11000-11002 only
```

Only the gateway is published on a public port. Azurite and the Functions app are bound to `127.0.0.1`, so on the VPS they cannot be reached from outside. Everything public goes through the gateway, which is the point of Part 3.

## Files

| File | Purpose |
|---|---|
| `CoffeeNChillGateway/Dockerfile` | Multi-stage build: .NET 10 SDK to publish, `aspnet:10.0` to run. Listens on 8080 as the non-root `app` user. |
| `CoffeeNChillGateway/.dockerignore` | Keeps `bin/`, `obj/`, launch settings and any local `appsettings.json` (which can hold secrets) out of the image. |
| `docker-compose.yml` | The whole stack, using images from Docker Hub only. This is the file the VPS uses. |
| `docker-compose.build.yml` | Override that adds `build:` sections, for building the images from this repo. |
| `.env.example` | Template for `.env`. Copy it and set `JWT_KEY`. `.env` is git-ignored. |
| `Postman/CoffeeNChill-Part3-Gateway.postman_collection.json` | Every gateway endpoint, including the Bearer token flows. |
| `Postman/CoffeeNChill-Part3.postman_environment.json` | `baseUrl` for local Docker, plus a `vpsBaseUrl` placeholder. |

## Settings

All gateway settings come from environment variables. Nothing secret is in the repository.

| `.env` variable | Becomes | Default | Notes |
|---|---|---|---|
| `JWT_KEY` | `Jwt__Key` | none, required | Signing key. Must be at least 32 characters for HMAC-SHA256. Compose refuses to start without it. |
| `JWT_ISSUER` | `Jwt__Issuer` | `CoffeeNChillGateway` | |
| `JWT_AUDIENCE` | `Jwt__Audience` | `CoffeeNChillClients` | |
| `GATEWAY_PORT` | host port | `5000` | The port in the public URL. |
| `DOCKERHUB_NAMESPACE` | image names | `mandlakazi07` | Docker Hub account the images live under. |
| `FUNCTIONS_TAG` / `GATEWAY_TAG` | image tags | `part2` / `part3` | |

Set directly in `docker-compose.yml`:

| Variable | Value | Why |
|---|---|---|
| `ConnectionStrings__AzureStorage` | Azurite connection string | Read by `AuthController` for the `Users` table. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Swagger is still on, because `Program.cs` enables it in every environment. |
| `FunctionsApi__BaseUrl` | `http://functions:80` | For Person 1. Read it with `builder.Configuration["FunctionsApi:BaseUrl"]` when the gateway starts calling the Functions app. |

To make a key, run this in PowerShell and paste the output after `JWT_KEY=`:

```powershell
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
```

On the Linux VPS, use `openssl rand -base64 48`.

## Running it locally (build from source)

From the repository root, with Docker Desktop running:

```powershell
Copy-Item .env.example .env
# edit .env and set JWT_KEY
docker compose -f docker-compose.yml -f docker-compose.build.yml up -d --build
docker compose ps
```

Then open <http://localhost:5000/swagger>.

Stop it with `docker compose down`. Add `-v` only if you also want to wipe the Azurite data, including registered users.

If Docker complains that a container name like `coffeenchill-part2-azurite` is already in use, an older Part 2 stack is still running from another folder. Stop that one first with `docker compose down` in its own folder.

## Publishing the images (Person 3)

Someone logged in to the `mandlakazi07` Docker Hub account runs:

```powershell
docker login
docker compose -f docker-compose.yml -f docker-compose.build.yml build
docker compose -f docker-compose.yml -f docker-compose.build.yml push gateway functions
```

As of 8 November 2026, `mandlakazi07/coffeenchill-functions:part2` is on Docker Hub, but **`mandlakazi07/coffeenchill-gateway:part3` is not**. The VPS cannot pull the gateway until this is done.

## Running it on the VPS

On the VPS you only need `docker-compose.yml` and a `.env`. No source code or build tools are required:

```bash
mkdir coffeenchill && cd coffeenchill
# copy docker-compose.yml here, then create .env from .env.example with a fresh JWT_KEY
docker compose pull
docker compose up -d
docker compose ps
```

Open port 5000 (or whatever `GATEWAY_PORT` is) in the VPS firewall. The public URL is then `http://<vps-ip>:5000/swagger`. Use a different `JWT_KEY` on the VPS from the one used locally.

## Postman

Import both files from `Postman/`, select the **CoffeeNChill Part 3** environment, and run the collection from the top. For the VPS, set `baseUrl` to the VPS address.

| Folder | What it shows |
|---|---|
| 00 Setup and health | Gateway is up and serving Swagger. Creates unique emails for this run, so the collection can be run again and again. |
| 01 Auth: register and login | Registers a Customer, a Staff member and an Admin, logs each in, checks the token has Name, Email and Role claims, and saves `customerToken`, `staffToken` and `adminToken`. |
| 02 Auth: error cases | Duplicate email gives 409, missing password 400, wrong password 401, unknown email 401. |
| 03 Menu: public | `GET /api/menu` with no token gives 200. |
| 04 Menu: Staff and Admin | Add, update and delete menu items with Staff and Admin tokens give 200. |
| 05 Access control | No token gives 401, Customer token 403, tampered token 401. |
| 06 Planned gateway routes | Orders and documents routes from the brief that the gateway does not have yet. They are **skipped**, not failed, while they return 404, and start testing for real once Person 1 adds them. |

Tokens are set by request-level Bearer auth (`{{staffToken}}` etc.), so in the demo you can open a request's Authorization tab and show which token it uses.

## Test results

Run on 8 November 2026 against images built from this branch, using Docker Engine 29.8.1 (Docker Desktop on Windows) and Newman 6.2.1.

| Check | Result |
|---|---|
| Gateway and Functions images build | Passed |
| Compose refuses to start with no `JWT_KEY` | Passed (stops with the message from `docker-compose.yml`) |
| All three containers start and stay up | Passed |
| Gateway listens on 8080 inside the container, 5000 on the host | Passed |
| Swagger UI and `swagger.json` served | Passed |
| Postman collection, first run | 26 requests, 37 assertions passed, 0 failed, 6 skipped (folder 06) |
| Postman collection, second run straight after | Same result, so the collection can be re-run without conflicts |
| Gateway image on Docker Hub | **Not yet** (see above) |
| Running on the VPS | Not tested. That is Person 3's step. |

The test stack was run under a separate Compose project name with different container names and Azurite/Functions host ports, because an older Part 2 stack was already running on the test machine. The gateway port (5000), images, settings and network were exactly as committed.

## Fix made to get the gateway building

`part-3` did not compile at `73680fd`. The header comments added on 30 October contained a literal PowerShell `` `n ``, which put the `namespace` line inside the comment in `AuthController.cs`, `MenuController.cs` and `UserProfile.cs`. That is fixed in its own commit (`fix(gateway): restore namespace lines ...`), which only splits those three lines. Nothing else in Person 1's code was changed.

## For Person 1: things found while testing

These were all seen during testing, not just by reading the code. Changing them means editing the gateway code, so they are left for Person 1.

**Security**

1. **Anyone can register as Admin or Staff.** `POST /api/auth/register` is anonymous and takes `role` from the request body. A request with `"role": "Admin"` returned 200 and that user can then change the menu. Suggested fix: force self-registration to `Customer`, and create Staff/Admin users another way, such as an Admin-only endpoint or a seed on start-up.
2. **The same email can be registered once per role.** Users are keyed by `PartitionKey = Role`, `RowKey = email`, so `student@...` as Customer and again as Staff both returned 200. Login then queries by email only and uses whichever row comes back first. Check across all partitions before inserting.
3. **Any role string is accepted.** `"role": "Banana"` registered fine. Restrict it to `Admin`, `Staff` or `Customer`.

**Consistent responses (DTOs)**

4. Successes return JSON (`{ "message": ... }` or `{ "token": ... }`), but errors from `AuthController` are plain text, for example `User already exists.` with no JSON wrapper. Clients and Postman tests have to handle both. Suggest one shape for errors, for example `{ "message": "..." }` or ASP.NET's built-in `ProblemDetails`.
5. `RegisterRequest` and `LoginRequest` live inside `AuthController.cs`. The brief asks for clean DTOs, so moving them into a `DTOs/` folder (as in the Functions project) would match.
6. `POST /api/menu` and `PUT /api/menu/{id}` take a bare JSON string (`"Flat White"`) instead of an object. When the menu is wired to the real `MenuItems` table, the Functions project's `CreateMenuItemDto` and `UpdateMenuItemDto` already define the fields (category, id, name, description, price, isAvailable). Reusing those field names keeps the gateway and Functions consistent.
7. Property naming: the gateway returns camelCase (ASP.NET default). The Part 2 queue endpoint returned PascalCase (`OrderId`, `Status`; see `order-queue-api.md`). When the gateway proxies orders, return camelCase so the whole public API matches.

**Not built yet** (from the brief, covered by folder 06 in Postman)

8. `POST /api/orders/queue` (anonymous), order status update (Admin/Staff), and document upload (Admin/Staff). The menu is still hard-coded in the controller rather than coming from the Functions app or the `MenuItems` table.

**Minor**

9. `CoffeeNChillGateway.http` still calls the template `/weatherforecast` endpoint, which does not exist.
10. `app.UseHttpsRedirection()` does nothing in the container because only HTTP is configured. It is harmless, but if the VPS later sits behind HTTPS, redirection should be handled by the proxy.
