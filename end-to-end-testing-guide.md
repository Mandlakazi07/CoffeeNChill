# End-to-end testing guide

This walks through one order from submission to `Collected`, and shows how to check each stage. Do it once with Postman (or PowerShell), then again with the logs open, and record what you see in [test-results.md](test-results.md).

What you will test:

```text
POST /api/orders/queue  ->  order-processing-queue  ->  ProcessOrderQueue  ->  Orders table  ->  GET /api/orders/{orderId}
```

Related pages: [order-queue-api.md](order-queue-api.md), [order-lifecycle.md](order-lifecycle.md), [postman-success-checklist.md](postman-success-checklist.md), [negative-test-cases.md](negative-test-cases.md).

## 1. Start the environment

Pick one of the two routes.

### Option A: Docker Compose

From the repository root, as described in the README:

```powershell
docker build -t senandlovu/coffeenchill-functions:part2 .
docker compose up -d --pull never
docker compose ps
docker compose logs functions --tail 100
```

- Both `coffeenchill-part2-azurite` and `coffeenchill-part2-functions` should be `Up`.
- The API is at `http://127.0.0.1:18080`.
- The logs should list `QueueOrder`, `ProcessOrderQueue`, `GetOrderStatus` and `UpdateOrderStatus`.

This route has not been confirmed on this repository yet (see item 13 in [test-results.md](test-results.md)). If you run it, screenshot the results.

### Option B: Local host and Azurite

This is the route that was used for the recorded evidence.

1. Start Azurite in its own terminal. If PowerShell says running scripts is disabled for `azurite.ps1`, call the `.cmd` version instead:

   ```powershell
   $dataPath = Join-Path $env:USERPROFILE ".azurite"
   New-Item -ItemType Directory -Path $dataPath -Force | Out-Null
   & "$env:APPDATA\npm\azurite.cmd" --location "$dataPath"
   ```

   You should see Blob, Queue and Table services "successfully listening" on ports 10000, 10001 and 10002.

2. Make sure `CoffeeNChillFunctions/local.settings.json` exists. It is git-ignored, so it will not be in a fresh clone. Do not commit it.

   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
     }
   }
   ```

   If Azurite complains about the API version, restart it with `--skipApiVersionCheck`. The Compose file does this too.

3. In a second terminal, start the host from the `CoffeeNChillFunctions` folder:

   ```powershell
   func start
   ```

   Look for the list of functions near the end of the start-up output. Screenshot it, since that is currently missing from the evidence. The default address is `http://localhost:7071` unless you pass `--port`.

## 2. Set the base URL

In Postman, set `baseUrl` in the active environment to match step 1 (for example `http://127.0.0.1:18080` or `http://localhost:7071`). Do not trust the file `CoffeeNChill Local.postman_environment.json`, as its value is empty.

For PowerShell, set `$baseUrl` at the top of the script below.

## 3. Submit an order

### In Postman

`POST {{baseUrl}}/api/orders/queue` with a JSON body. Use an ID nobody has used before:

```json
{
  "orderId": "ORD-TEST-001",
  "customerName": "Test Customer",
  "selectedItemSkus": ["COFFEE-001"],
  "totalPrice": 45.00
}
```

### In PowerShell

```powershell
$baseUrl = "http://127.0.0.1:18080"
$orderId = "ORD-TEST-" + (Get-Date -Format "yyyyMMddHHmmss")

$body = @{
    orderId          = $orderId
    customerName     = "Test Customer"
    selectedItemSkus = @("COFFEE-001")
    totalPrice       = 45.00
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$baseUrl/api/orders/queue" -ContentType "application/json" -Body $body
```

### What you should see

- Status `202 Accepted`.
- A body with `OrderId` (your ID), `Status` of `Queued`, an `OrderDate` and a `StatusUrl`. The exact format of the last two is still to be confirmed on the final build, so note what you actually get.
- In the host log: `Order ORD-TEST-... was added to the processing queue.`

If you get a `400`, read the `errors` list. Anything blank or missing is listed there.

## 4. Check the processing status

The order does not exist in the table until the queue trigger has run. Asking straight away will give a `404` ("was not found"). That is normal.

### In Postman

`GET {{baseUrl}}/api/orders/{{orderId}}`. Send it, and if it is a 404, wait 10 seconds and send it again. Allow up to a minute or so. The two runs on record took about 20 and 33 seconds.

### In PowerShell

This keeps checking every 5 seconds for up to 90 seconds, using the `$baseUrl` and `$orderId` from the last step:

```powershell
foreach ($attempt in 1..18) {
    try {
        $order = Invoke-RestMethod -Uri "$baseUrl/api/orders/$orderId"
        $order | ConvertTo-Json
        break
    }
    catch {
        Write-Host "Attempt $attempt of 18: not there yet, waiting 5 seconds..."
        Start-Sleep -Seconds 5
    }
}
```

### What you should see

```json
{
  "orderId": "ORD-TEST-...",
  "customerName": "Test Customer",
  "selectedItemSkus": ["COFFEE-001"],
  "totalPrice": 45,
  "status": "Collected",
  "orderTimestamp": "...",
  "lastUpdated": "..."
}
```

- `status` should be `Collected`. The processing function moves through all four statuses in one go, so you will rarely catch the middle ones through the API.
- `lastUpdated` should be a little after `orderTimestamp`.
- If `status` is `Ready`, the host is running an older build of the function.

## 5. Check the logs for each stage

This is where the middle statuses show up. In the host terminal (or `docker compose logs functions`), find these lines for your order:

```text
Order ORD-TEST-... was added to the processing queue.
Processing queued order ORD-TEST-....
Order ORD-TEST-... stored with Received status.
Order ORD-TEST-... is being prepared.
Order ORD-TEST-... is ready for collection.
Order ORD-TEST-... has been collected.
```

The wording above is taken from the source. Check it against what you actually see and screenshot the real output.

## 6. Change the status by hand (optional)

```powershell
Invoke-RestMethod -Method Put -Uri "$baseUrl/api/orders/$orderId/status" -ContentType "application/json" -Body '{ "status": "Preparing" }'
```

Expect `orderId`, `status` set to `Preparing`, and `message` of `Order status updated successfully.` Then run step 4 again to confirm the order now shows `Preparing`. This moves a finished order backwards. The API allows it, which is worth mentioning in a demo.

## 7. Look inside the storage (optional but useful)

Open Azure Storage Explorer, connect to the local emulator and look for:

- Table `Orders`: one row with `PartitionKey` set to the order's date (`yyyy-MM-dd`) and `RowKey` set to your order ID.
- Queue `order-processing-queue`: should be empty once the order has been processed.
- Queue `order-processing-queue-poison`: should not exist, or be empty. A message here means processing failed three times.

## 8. Try a few bad requests

Run at least these three from [negative-test-cases.md](negative-test-cases.md) and record the results:

1. N7: send `{}` and check that four errors come back.
2. R1: look up an order ID that does not exist and check for a 404.
3. P2: send `{ "status": "Cooking" }` to the status endpoint and check for a 400.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `404` on the status request, even after a minute | The order never got queued (check the response from step 3), the host is not running, or the trigger cannot reach the queue. Check the host log for errors and confirm Azurite is up. |
| Connection refused | Wrong port. The project uses 18080 (Compose), 7071 (default `func start`) and 7017 (`launchSettings.json`). |
| Host fails to start with a storage error | `AzureWebJobsStorage` is missing. Check `local.settings.json` (local) or the Compose environment. |
| Host logs a storage version error | Start Azurite with `--skipApiVersionCheck`. |
| `azurite.ps1 cannot be loaded because running scripts is disabled` | Use `azurite.cmd` as shown in step 1. |
| Status stuck at `Received`, `Preparing` or `Ready` | Processing failed part-way. Check the host log for `Failed to process queued order`. The runtime will retry up to 3 times. |
| Same order ID reused | The queue accepts it without complaint, then the function carries on from the old row. Use a new ID. |

## Evidence to keep

For each run, keep a screenshot of:

1. The 202 response from the queue request.
2. The status response showing `Collected`.
3. The host log lines for the order.
4. The `Orders` row in Storage Explorer.

Then add the result to [test-results.md](test-results.md), saying clearly whether it passed, failed or could not be checked.
