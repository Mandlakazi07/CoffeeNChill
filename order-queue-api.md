# Order queue endpoint

`POST /api/orders/queue` is how a customer order gets into the system. It validates the request, builds a complete queue message, Base64-encodes it, places it on the `order-processing-queue` Azure Storage queue and answers straight away. It does not write to the `Orders` table itself. That happens later, when the queue-triggered function picks the message up (see [order-lifecycle.md](order-lifecycle.md)).

Source: `CoffeeNChillFunctions/Functions/Orders/QueueOrder.cs`, `Validation/OrderRequestValidator.cs`, `Serialization/OrderQueueMessageCodec.cs`, `Services/OrderQueueService.cs`, `Models/OrderQueueMessage.cs`, `DTOs/OrderRequest.cs`.

## Quick reference

| | |
|---|---|
| Method and route | `POST /api/orders/queue` |
| Auth | Anonymous (no key needed) |
| Content type | `application/json` (max 32 KB) |
| Success | `202 Accepted`, with a `Location` header pointing at the status URL |
| Local base URL (Docker Compose) | `http://127.0.0.1:18080` |
| Queue used | `order-processing-queue` (messages are Base64-encoded UTF-8 JSON) |

## Producer pipeline

1. **Read and parse.** The body is read in full. An empty body, a body over 32 KB, invalid JSON, a wrong value type, or an **unknown field** (for example a typo such as `totalPrize`) is rejected with `400`/`413` before anything else runs.
2. **Validate.** `OrderRequestValidator` checks every field and returns all failures at once (see the rules below).
3. **Build the message.** Values are trimmed, `totalPrice` is rounded to 2 decimals, and the server adds `messageId` (a GUID), `itemCount`, `currency`, `schemaVersion` and `queuedAtUtc`. If the client left out `orderTimestamp`, the current UTC time is used.
4. **Serialise and encode.** `OrderQueueMessageCodec` serialises with fixed camelCase property names, refuses to encode if any key attribute is missing, and Base64-encodes the UTF-8 JSON. The message is decoded again and compared with the original before it is sent (round-trip check).
5. **Send.** `OrderQueueService` connects lazily, creates the queue if needed and sends the message. Connection problems (missing or invalid connection string, storage/Azurite not reachable, Storage rejecting the request) become a `503` with `Retry-After: 10`, not an unhandled `500`.
6. **Consume.** The queue trigger (`host.json`: `messageEncoding: base64`) removes the Base64 layer and `ProcessOrderQueue` decodes the JSON with the same codec. A message that is missing a key attribute fails, is retried 3 times and then moves to `order-processing-queue-poison`.

## Request body

Property names are matched case-insensitively, so `orderId` and `OrderId` both work. Fields not listed here are rejected.

| Field | Type | Required | Rules |
|---|---|---|---|
| `orderId` | string | yes | Not blank, at most 64 characters, no `/ \ # ?` or control characters (it becomes the table `RowKey`). Trimmed. |
| `customerName` | string | yes | Not blank, at most 100 characters, no control characters. Trimmed. |
| `selectedItemSkus` | array of strings | yes | 1 to 50 entries, none blank, each at most 64 characters with no `/ \ # ?` or control characters. Not checked against the menu table. |
| `totalPrice` | number | yes | Greater than zero, at most 100 000.00, at most 2 decimal places. |
| `orderTimestamp` | ISO 8601 date-time | no | Not before 2000-01-01 and not more than 5 minutes in the future. Defaults to the current UTC time. Its UTC date becomes the `PartitionKey` in the `Orders` table. |

### Example request

```http
POST {{baseUrl}}/api/orders/queue
Content-Type: application/json

{
  "orderId": "ORD-4002",
  "customerName": "Sena Ndlovu",
  "selectedItemSkus": ["COFFEE-001", "CAKE-002"],
  "totalPrice": 129.90,
  "orderTimestamp": "2026-10-11T12:20:00Z"
}
```

## Queue message (what is stored on the queue)

The queue holds the Base64 text. Decoded, it is this JSON (property names are fixed by `[JsonPropertyName]`):

```json
{
  "schemaVersion": 1,
  "messageId": "3f1c0e5a9b2d4c7e8f6a1b2c3d4e5f60",
  "orderId": "ORD-4002",
  "customerName": "Sena Ndlovu",
  "selectedItemSkus": ["COFFEE-001", "CAKE-002"],
  "itemCount": 2,
  "totalPrice": 129.90,
  "currency": "ZAR",
  "orderTimestamp": "2026-10-11T12:20:00+00:00",
  "queuedAtUtc": "2026-11-02T10:00:00.1234567+00:00"
}
```

`orderTimestamp` is when the customer placed the order; `queuedAtUtc` is when the API put it on the queue. Messages written by older builds (PascalCase, no `messageId`/`queuedAtUtc`) are still accepted by the consumer.

## Responses

### 202 Accepted

The response echoes the full order that was queued, plus the queue receipt. All property names are camelCase, matching the status endpoints.

```json
{
  "message": "Order accepted and placed on the processing queue.",
  "orderId": "ORD-4002",
  "status": "Queued",
  "customerName": "Sena Ndlovu",
  "selectedItemSkus": ["COFFEE-001", "CAKE-002"],
  "itemCount": 2,
  "totalPrice": 129.90,
  "currency": "ZAR",
  "orderTimestamp": "2026-10-11T12:20:00+00:00",
  "queuedAtUtc": "2026-11-02T10:00:00.1234567+00:00",
  "orderDate": "2026-10-11",
  "statusUrl": "/api/orders/2026-10-11/ORD-4002",
  "queue": {
    "name": "order-processing-queue",
    "messageId": "3f1c0e5a9b2d4c7e8f6a1b2c3d4e5f60",
    "storageMessageId": "b2c9e0a4-...",
    "insertedOnUtc": "2026-11-02T10:00:00+00:00",
    "encoding": "base64"
  }
}
```

- `Queued` only ever appears in this response. It is never stored in the table. The first stored status is `Received`.
- A 202 means the message was accepted by the queue, not that the order has been processed. Asking for the order straight away can give a 404 (see [order-lifecycle.md](order-lifecycle.md)).

### 400 Bad Request: validation failed

Every rule that fails is reported in one response:

```json
{
  "message": "Order validation failed.",
  "errors": [
    "SelectedItemSkus cannot contain empty values."
  ]
}
```

### Other error responses

| Status | Body | When |
|---|---|---|
| 400 | `{ "error": "The request body is empty. A JSON order is required." }` | No body, or only whitespace. |
| 400 | `{ "error": "The request body contains invalid JSON at '$.totalPrice'. Check the field name and value type." }` | Wrong value type, or an unknown field. The path names the problem field. |
| 400 | `{ "error": "The request body contains invalid JSON." }` | Body is not parseable JSON. |
| 400 | `{ "error": "A valid order request is required." }` | Body is the JSON literal `null`. |
| 413 | `{ "error": "The request body cannot exceed 32 KB." }` | Body larger than 32 KB. |
| 503 | `{ "error": "The order queue is temporarily unavailable. Please try again shortly." }` + `Retry-After: 10` | Connection string missing or invalid, storage/Azurite unreachable, or Storage rejected the message. |
| 500 | `{ "error": "The order could not be queued because its payload was incomplete." }` | Safety net: the built message failed the codec's completeness or round-trip check. |
| 500 | `{ "error": "The order could not be queued." }` | Anything else unexpected. |

The full list of bad-input cases is in [negative-test-cases.md](negative-test-cases.md).

## Validation messages

| Rule | Message |
|---|---|
| `orderId` missing or blank | `OrderId is required.` |
| `orderId` too long | `OrderId cannot exceed 64 characters.` |
| `orderId` has a forbidden character | `OrderId cannot contain /, \, #, ? or control characters.` |
| `customerName` missing or blank | `CustomerName is required.` |
| `customerName` too long | `CustomerName cannot exceed 100 characters.` |
| `customerName` has control characters | `CustomerName cannot contain control characters.` |
| `selectedItemSkus` missing or empty | `At least one menu item SKU is required.` |
| More than 50 SKUs | `An order cannot contain more than 50 items.` |
| A blank value inside `selectedItemSkus` | `SelectedItemSkus cannot contain empty values.` |
| A SKU over 64 characters | `Each SKU cannot exceed 64 characters.` |
| A SKU with a forbidden character | `SKUs cannot contain /, \, #, ? or control characters.` |
| `totalPrice` missing | `TotalPrice is required.` |
| `totalPrice` zero or negative | `TotalPrice must be greater than zero.` |
| `totalPrice` over the limit | `TotalPrice cannot exceed 100000.00.` |
| `totalPrice` with 3+ decimals | `TotalPrice cannot have more than two decimal places.` |
| `orderTimestamp` before 2000 | `OrderTimestamp must be on or after 2000-01-01.` |
| `orderTimestamp` in the future | `OrderTimestamp cannot be in the future.` |

## What happens after a 202

1. `ProcessOrderQueue` picks the message up, decodes it, creates the row in the `Orders` table with status `Received`, then moves it through `Preparing` and `Ready` to `Collected`.
2. The order can be read with `GET /api/orders/{orderId}` or the `statusUrl` from the response.

For the full walkthrough, including how to wait for processing, see [end-to-end-testing-guide.md](end-to-end-testing-guide.md).
