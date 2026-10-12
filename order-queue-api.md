# Order queue endpoint

`POST /api/orders/queue` is how a customer order gets into the system. It checks the request, drops it on the `order-processing-queue` Azure Storage queue and answers straight away. It does not write to the `Orders` table itself. That happens later, when the queue-triggered function picks the message up (see [order-lifecycle.md](order-lifecycle.md)).

Source: `CoffeeNChillFunctions/Functions/Orders/QueueOrder.cs`, `Validation/OrderRequestValidator.cs`, `DTOs/OrderRequest.cs`.

## Quick reference

| | |
|---|---|
| Method and route | `POST /api/orders/queue` |
| Auth | Anonymous (no key needed) |
| Content type | `application/json` |
| Success | `202 Accepted` |
| Local base URL (Docker Compose) | `http://127.0.0.1:18080` |
| Queue used | `order-processing-queue` (messages are Base64-encoded JSON) |

## Request body

Property names are matched case-insensitively, so `orderId` and `OrderId` both work.

| Field | Type | Required | Rules |
|---|---|---|---|
| `orderId` | string | yes | Not empty or whitespace. Trimmed before it is queued. |
| `customerName` | string | yes | Not empty or whitespace. Trimmed before it is queued. |
| `selectedItemSkus` | array of strings | yes | At least one entry, and no entry may be blank. The SKUs are not checked against the menu table. |
| `totalPrice` | number | yes | Must be greater than zero. |
| `orderTimestamp` | ISO 8601 date-time | no | Defaults to the current UTC time if left out. Its UTC date becomes the `PartitionKey` in the `Orders` table. |

### Example request

This is the order from the Postman run on 11 October 2026 (screenshot `Screenshot 2026-10-12 004142.png`). In Postman the body used variables (`{{orderId}}`, `{{menuItemId}}`, `{{orderTimestamp}}`). The values below are what those variables resolved to, worked out from the order that came back from the status endpoint, so the timestamp is the value the API stored rather than something typed out by hand.

```http
POST {{baseUrl}}/api/orders/queue
Content-Type: application/json

{
  "orderId": "ORD-PM-1791758475499",
  "customerName": "Postman Demo Customer",
  "selectedItemSkus": ["COF-PM-1791758384872"],
  "totalPrice": 50.00,
  "orderTimestamp": "2026-10-11T22:41:15.5Z"
}
```

## Responses

### 202 Accepted (observed)

Returned in 140 ms for the request above:

```json
{
  "OrderId": "ORD-PM-1791758475499",
  "Status": "Queued",
  "OrderDate": "2026-10-11T22:41:15.5000000+00:00",
  "StatusUrl": "/api/orders/ORD-PM-1791758475499"
}
```

Things worth knowing about this response:

- The property names come back in PascalCase (`OrderId`, `Status`), while the status endpoints return camelCase (`orderId`, `status`). A Postman test written as `response.orderId` against this endpoint will not find anything.
- `Queued` only ever appears in this response. It is never stored in the table. The first stored status is `Received`.
- A 202 means the message was accepted by the queue, not that the order has been processed. Asking for the order straight away can give a 404 (see [order-lifecycle.md](order-lifecycle.md)).

### Mismatch with the current source

The response above does not match what `QueueOrder.cs` currently builds. In the source, `OrderDate` is the date only (`yyyy-MM-dd`) and `StatusUrl` includes that date:

```json
{
  "OrderId": "ORD-PM-1791758475499",
  "Status": "Queued",
  "OrderDate": "2026-10-11",
  "StatusUrl": "/api/orders/2026-10-11/ORD-PM-1791758475499"
}
```

That second shape has not been seen from a running build. The screenshots were most likely taken against an earlier build, but that is a guess. The request should be re-run on the final build and the example above replaced if it changes. Until then, treat the observed response as correct for the build that was tested and the source as correct for the code in the repository.

### 400 Bad Request: validation failed (observed)

Every rule that fails is reported in one response. This one came back in 244 ms when a blank SKU was sent (`Screenshot 2026-10-12 003229.png`):

```json
{
  "message": "Order validation failed.",
  "errors": [
    "SelectedItemSkus cannot contain empty values."
  ]
}
```

### Other error responses (from the source, not yet run)

| Status | Body | When |
|---|---|---|
| 400 | `{ "error": "The request body contains invalid JSON." }` | Body is not valid JSON, is empty, or a value has the wrong type (for example `"totalPrice": "abc"`). |
| 400 | `{ "error": "A valid order request is required." }` | Body is the JSON literal `null`. |
| 500 | `{ "error": "The order could not be queued." }` | Anything unexpected, for example the storage account being unreachable. |

The full list of bad-input cases is in [negative-test-cases.md](negative-test-cases.md).

## Validation messages

These are the exact strings the validator returns:

| Rule | Message |
|---|---|
| `orderId` missing or blank | `OrderId is required.` |
| `customerName` missing or blank | `CustomerName is required.` |
| `selectedItemSkus` missing or empty | `At least one menu item SKU is required.` |
| A blank value inside `selectedItemSkus` | `SelectedItemSkus cannot contain empty values.` |
| `totalPrice` zero, negative or missing | `TotalPrice must be greater than zero.` |

## What happens after a 202

1. The order is serialised to JSON and sent to `order-processing-queue`.
2. `ProcessOrderQueue` picks it up, creates the row in the `Orders` table with status `Received`, then moves it through `Preparing` and `Ready` to `Collected`.
3. The order can be read with `GET /api/orders/{orderId}`.

For the full walkthrough, including how to wait for processing, see [end-to-end-testing-guide.md](end-to-end-testing-guide.md).
