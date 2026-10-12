# Negative test cases

These are the ways the order endpoints should refuse or fail on bad input. Each case has an **Evidence** value so it is clear what is known and what is not:

- **Observed**: a screenshot of the real response exists (see [test-results.md](test-results.md)).
- **Source**: the expected result comes from reading the code. It has not been run.
- **Source, risky**: reading the code suggests the app may not behave well. Run it before trusting the prediction.

Messages are copied exactly from the source, so they can be pasted into Postman assertions.

## Queue endpoint: `POST /api/orders/queue`

Start from this valid body and change one thing at a time.

```json
{
  "orderId": "ORD-NEG-001",
  "customerName": "Test Customer",
  "selectedItemSkus": ["COFFEE-001"],
  "totalPrice": 45.00
}
```

Validation failures return `400` with `{ "message": "Order validation failed.", "errors": [ ... ] }`.

| ID | What to send | Expected | Evidence |
|---|---|---|---|
| N1 | Remove `orderId` (or set it to `""` or `"   "`) | 400, errors contain `OrderId is required.` | Source |
| N2 | Remove `customerName`, or set it to blank | 400, errors contain `CustomerName is required.` | Source |
| N3 | Remove `selectedItemSkus`, or send `[]` | 400, errors contain `At least one menu item SKU is required.` | Source |
| N4 | `"selectedItemSkus": [""]` (or a list with one blank entry) | 400, errors contain `SelectedItemSkus cannot contain empty values.` | **Observed** |
| N5 | `"totalPrice": 0` or `-5` | 400, errors contain `TotalPrice must be greater than zero.` | Source |
| N6 | Remove `totalPrice` | Same as N5, because a missing number defaults to 0 | Source |
| N7 | Send `{}` | 400 with four errors, in this order: `OrderId is required.`, `CustomerName is required.`, `At least one menu item SKU is required.`, `TotalPrice must be greater than zero.` | Source |

N4 happened by accident on 11 October 2026 (`Screenshot 2026-10-12 003229.png`). The response was a 400 in 244 ms with exactly the body shown in [order-queue-api.md](order-queue-api.md). The Postman body uses `{{menuItemId}}` for the SKU, so the most likely cause is that variable being empty because the menu item had not been created yet. That is an inference, as the request body itself is not visible in the screenshot.

### Malformed requests

These return `400` with `{ "error": "..." }`.

| ID | What to send | Expected | Evidence |
|---|---|---|---|
| N8 | Broken JSON, for example a missing closing brace | 400, `The request body contains invalid JSON.` | Source |
| N9 | Empty body | 400, `The request body contains invalid JSON.` | Source |
| N10 | Body is just `null` | 400, `A valid order request is required.` | Source |
| N11 | Wrong types, for example `"totalPrice": "abc"` or `"orderTimestamp": "not-a-date"` | 400, `The request body contains invalid JSON.` | Source |

For N11 the JSON itself is fine, so the message is misleading. It would be clearer if it named the field. This is a wording issue, not a crash.

### Behaviour that may surprise people

These are not errors, but a tester should know about them.

| ID | What to send | What the code does | Evidence |
|---|---|---|---|
| N12 | The same `orderId` twice | The queue endpoint accepts it again with another `202`. The function then finds the existing row and carries on from its status, or ignores it if the order is already `Collected`. The caller is never told it was a duplicate. | Source, risky |
| N13 | A SKU that is not on the menu, such as `"NOT-A-REAL-SKU"` | `202 Accepted`. SKUs are not checked against the menu table. | Source |
| N14 | `GET /api/orders/queue` (wrong method on the queue route) | The route matches `GET /api/orders/{orderId}` with an ID of `queue`, so the answer is `404`, `Order 'queue' was not found.` | Source |

## Reading orders

Error bodies are `{ "error": "..." }`.

| ID | Request | Expected | Evidence |
|---|---|---|---|
| R1 | `GET /api/orders/ORD-DOES-NOT-EXIST` | 404, `Order 'ORD-DOES-NOT-EXIST' was not found.` | **Observed** (with other IDs, see below) |
| R2 | `GET /api/orders/{id}` straight after queueing, before the trigger has run | 404, same message | **Observed** |
| R3 | `GET /api/orders/11-10-2026/{id}` (date not `yyyy-MM-dd`) | 400, `Date must be in yyyy-MM-dd format.` | Source |
| R4 | `GET /api/orders/2026-10-11/ORD-DOES-NOT-EXIST` (valid date, no such order) | 404, `Order 'ORD-DOES-NOT-EXIST' was not found.` | Source |
| R5 | `GET /api/orders?date=2026-13-45` | 400, `Date must be in yyyy-MM-dd format.` | Source |
| R6 | `GET /api/orders?date=` (empty value) | 400, same message, because an empty string is not a valid date | Source |

R1 and R2 were seen with real order IDs. `ORD-PM-1791757932678` returned a 404 in 59 ms (`Screenshot 2026-10-12 003316.png`). That order was never queued, because the request just before it had failed validation (N4). `ORD-PM-1791760567434` returned a 404 in 101 ms (`Screenshot 2026-10-12 011652.png`) a few seconds after it was queued successfully, which is R2.

Both returned this body:

```json
{
  "error": "Order 'ORD-PM-1791760567434' was not found."
}
```

Note that the Postman request for this call showed **0/3 tests passed**. That is the success assertions (expecting a 200) running against a correct 404. It is not an API fault.

## Updating status: `PUT /api/orders/{orderId}/status`

| ID | What to send | Expected | Evidence |
|---|---|---|---|
| P1 | `{}` (no `status`) | 400, `The status field is required.` | Source |
| P2 | `{ "status": "Cooking" }` | 400, `Status must be Received, Preparing, Ready or Collected.` | Source |
| P3 | `{ "status": "Queued" }` | 400, same message. `Queued` is not a stored status. | Source |
| P4 | `{ "status": "" }` or `{ "status": null }` | 400, same message | Source |
| P5 | Broken JSON, or an empty body | 400, `The request body contains invalid JSON.` | Source |
| P6 | A valid body for an order that does not exist | 404, `Order '{orderId}' was not found.` | **Observed** |
| P7 | `{ "status": 5 }` (a number instead of a string) | Not handled in the code. Expect an unhandled error and a `500`. | Source, risky |
| P8 | A body that is a JSON array, such as `[]` | Same problem as P7, expect a `500` | Source, risky |
| P9 | `{ "status": "ready" }` on an existing order | Not an error. 200, and the stored status becomes `Ready`. | Source |

P6 was observed for `ORD-PM-1791757932678` (42 ms, `Screenshot 2026-10-12 003336.png`). Postman showed 0/1 tests passed there for the same reason as above.

P7 and P8 are the two that deserve attention. `UpdateOrderStatus.cs` only catches `JsonException`. Calling `GetString()` on a number, or looking up a property on an array, throws a different exception type, so a bad client can trigger a 500 instead of a 400. Please run these two and report back, since fixing them means editing a `.cs` file, which is outside this task.

## Reusable Postman script for 400 responses

```javascript
pm.test("Status code is 400", function () {
    pm.response.to.have.status(400);
});

pm.test("Validation message is returned", function () {
    const body = pm.response.json();
    pm.expect(body.errors).to.include("OrderId is required.");
});
```

Change the string in the second test for each case. For the `{ "error": "..." }` responses, check `body.error` instead of `body.errors`.

## Not covered here

- Messages that fail repeatedly and end up in `order-processing-queue-poison`. This needs the storage emulator open, not just Postman.
- Very large bodies, special characters in `orderId` (such as `/` or spaces) and very long names. None of these are validated and none have been tried.
