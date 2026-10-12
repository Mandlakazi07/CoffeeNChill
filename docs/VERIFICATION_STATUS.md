# CoffeeNChill verification status

**Last updated:** 11 October 2026  
**Repository:** `Mandlakazi07/CoffeeNChill`  
**Purpose:** Separate confirmed evidence from work that is still outstanding.

## Confirmed

- The reviewed ZIP was extracted and the project files were copied into a clone of `https://github.com/Mandlakazi07/CoffeeNChill.git`.
- The initial source import was committed and pushed to branch `main` (commit shown in the local output: `3913f08`).
- On Windows, this command completed successfully:

  ```powershell
  dotnet build .\CoffeeNChillFunctions\CoffeeNChillFunctions.csproj
  ```

  The recorded output reported `CoffeeNChillFunctions net10.0 succeeded` and `Build succeeded`.

## Not yet verified at runtime

- Azure Functions host starts and discovers all expected functions.
- Menu CRUD endpoints work against the storage emulator.
- Staff document upload/list/download work against the chosen storage implementation.
- Order submission returns the expected response and creates a queue message.
- The queue-trigger function consumes a message and writes/updates an Orders table entity.
- Order status GET/PUT endpoints work end to end.
- Postman collection requests pass.
- Docker Compose starts successfully, the source-matching image runs, and the image pushed to Docker Hub is pullable.

Docker Desktop was closed during the current work because startup troubleshooting was stressful. No claim of a successful Docker runtime test is made in this report.

## Known requirement gap

The current `StaffFileService` and dependency registration use Azure Blob Storage (`Azure.Storage.Blobs` / `BlobContainerClient`). The stated brief requires staff documents in an Azure File Share. Migrate the implementation to Azure Files and test upload/list/download before claiming this requirement is satisfied.

## Evidence to capture before submission

1. Successful build output.
2. Functions host startup output showing discovered functions.
3. Postman results for menu create/list/update/delete/category routes.
4. Document upload/list/download responses using the required Azure File Share implementation.
5. Order queue response, queue-trigger processing logs, and order status response.
6. Docker Compose container status/logs if Docker is used for the final demonstration.
7. Screenshots or video sections that match the test results actually observed.
