# Order lifecycle

An order moves through four stored statuses: **Received, Preparing, Ready, Collected**. There is also a fifth value, `Queued`, but it only appears in the response to `POST /api/orders/queue` and is never written to the table.

Source: `Functions/Orders/ProcessOrderQueue.cs`, `Functions/Orders/UpdateOrderStatus.cs`, `Services/OrderTableService.cs`, `Models/OrderStatus.cs`, `host.json`.

## The flow

```text
POST /api/orders/queue
        |
        |  validates, then sends a message to order-processing-queue
        v
 "Queued"  (in the HTTP response only, nothing stored yet)
        |
        |  queue trigger runs ProcessOrderQueue
        v
 Received   row created in the Orders table
        v
 Preparing
        v
 Ready
        v
 Collected  final status
```

## Status reference

| Status | Set by | Meaning |
|---|---|---|
| `Queued` | `QueueOrder` (response only) | The queue accepted the message. Nothing is in the table yet. |
| `Received` | `ProcessOrderQueue` | The row has just been created in the `Orders` table. |
| `Preparing` | `ProcessOrderQueue` or `PUT .../status` | The order is being made. |
| `Ready` | `ProcessOrderQueue` or `PUT .../status` | The order is ready for the customer. |
| `Collected` | `ProcessOrderQueue` or `PUT .../status` | The customer has picked it up. Final state. |

## How the queue function works

All of the transitions happen inside one run of `ProcessOrderQueue`, one straight after the other, with no delay between them. Each step re-reads the row and writes the new status and `lastUpdated`.

The function logs one line per step:

| Step | Log message |
|---|---|
| Start | `Processing queued order {OrderId}.` |
| Row created | `Order {OrderId} stored with Received status.` |
| Preparing | `Order {OrderId} is being prepared.` |
| Ready | `Order {OrderId} is ready for collection.` |
| Collected | `Order {OrderId} has been collected.` |

Because the steps run back to back, `Received`, `Preparing` and `Ready` are almost never visible through the API. Polling `GET /api/orders/{orderId}` will normally show `Collected` the first time the order exists. The earlier statuses can be shown in the function logs instead, or by adding a deliberate delay in the code (not part of this documentation task).

### Messages delivered more than once

Azure Storage queues can deliver the same message twice. The function handles this by resuming from whatever status is already stored instead of starting again:

- No row yet: create it as `Received`, then carry on.
- Row at `Received`, `Preparing` or `Ready`: continue from that point.
- Row already `Collected`: log `Ignoring duplicate queue message for already collected order {OrderId}.` and stop.

### When processing fails

The exception is logged and then re-thrown on purpose so the Functions runtime sees a failed run. From `host.json`:

| Setting | Value | Effect |
|---|---|---|
| `visibilityTimeout` | `00:00:05` | A failed message becomes visible again after 5 seconds. |
| `maxDequeueCount` | `3` | After 3 failed attempts the message is moved to `order-processing-queue-poison`. |

The poison queue has not been tested yet. See [test-results.md](test-results.md).

## Changing status by hand

`PUT /api/orders/{orderId}/status` with a body of `{ "status": "Preparing" }` sets the status directly.

- The accepted values are `Received`, `Preparing`, `Ready` and `Collected`. Matching ignores case, and the stored value is normalised to the proper spelling (`ready` is saved as `Ready`).
- `Queued` is rejected with a 400.
- There is no check on the order of changes. From the code, an order that is `Collected` can be set back to `Preparing`. This is useful for demonstrating the endpoint but means the lifecycle is not enforced.
- A successful call returns:

```json
{
  "orderId": "ORD-PM-1791758475499",
  "status": "Ready",
  "message": "Order status updated successfully."
}
```

That response was observed on 11 October 2026 (`Screenshot 2026-10-12 004228.png`). The order was already `Ready` at the time, so the call confirmed the endpoint works but did not show a change of state.

## Reading an order

| Route | Returns |
|---|---|
| `GET /api/orders/{orderId}` | One order, looked up by `RowKey` across all dates. |
| `GET /api/orders/{orderDate}/{orderId}` | One order, looked up by date and ID. The date must be `yyyy-MM-dd`. |
| `GET /api/orders` or `GET /api/orders?date=yyyy-MM-dd` | A list, newest first, with a `count`. |

Observed `GET /api/orders/{orderId}` response once an order had finished (`Screenshot 2026-10-12 012508.png`):

```json
{
  "orderId": "ORD-PM-1791760754270",
  "customerName": "Postman Demo Customer",
  "selectedItemSkus": [
    "COF-PM-1791760744418"
  ],
  "totalPrice": 50,
  "status": "Collected",
  "orderTimestamp": "2026-10-11T23:19:14.271+00:00",
  "lastUpdated": "2026-10-11T23:19:47.8334686+00:00"
}
```

## Where orders are stored

Azure Table Storage, table `Orders`.

| Column | Value |
|---|---|
| `PartitionKey` | The order's UTC date as `yyyy-MM-dd`, taken from `orderTimestamp`. |
| `RowKey` | The `orderId`. |
| `CustomerName` | From the request. |
| `SelectedItemSkusJson` | The SKU list as a JSON string. |
| `TotalPrice` | Stored as a double because Table Storage has no decimal type. |
| `Status` | One of the four statuses above. |
| `OrderTimestamp` | From the request, or the time it was queued. |
| `LastUpdated` | Set on creation and on every status change. |

The table row has not been inspected directly in any of the evidence so far, so the partition key behaviour is taken from the code only.

## What was actually seen

| Run | Queued (UTC) | What the status endpoint showed | `lastUpdated` |
|---|---|---|---|
| A, order `ORD-PM-1791758475499` | 11 Oct 22:41:15 | `Ready` | 22:41:35 |
| B, order `ORD-PM-1791760754270` | 11 Oct 23:19:14 | `Collected` | 23:19:47 |
| C, order `ORD-PM-1791760567434` | 11 Oct 23:16:07 | `404 Not Found` on the first check | not captured |

What this tells us:

- **Run A stopped at `Ready`.** That matches the note in `REVIEW_AND_TEST_REPORT.md` that the earlier version of the function did not write `Collected`. Run B reached `Collected`, so the current function appears to be working as intended.
- **Processing is not instant.** The final write landed about 20 seconds after queueing in run A and about 33 seconds in run B. The Postman request is labelled "wait ~5 seconds after queueing", which was not long enough here. A likely cause is the queue trigger's polling interval backing off while the queue is idle, but that has not been confirmed.
- **Run C shows why a 404 is not always a bug.** The order is only in the table once the trigger has run, so an immediate lookup can fail. No later check on that order was captured, so we do not know if it did finish.
