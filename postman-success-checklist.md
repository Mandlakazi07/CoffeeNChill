# Postman checklist: successful requests

Use this when running the order requests in Postman. Tick a box only when you have seen the response on screen. For the bad-input cases see [negative-test-cases.md](negative-test-cases.md), and for the results that have been recorded so far see [test-results.md](test-results.md).

## Before you start

- [ ] Azurite is running (Blob, Queue and Table all report "successfully listening").
- [ ] The Functions host is running, either through Docker Compose or locally. The log should show the queue trigger starting.
- [ ] A Postman environment is selected and `baseUrl` is filled in. The Docker Compose value is `http://127.0.0.1:18080`. The exported `CoffeeNChill Local.postman_environment.json` at the repository root has an **empty** `baseUrl`, so it will not work until you set one.
- [ ] You are using a new `orderId` for this run. Reusing an ID is not rejected by the queue endpoint (see N12 in the negative cases), so an old ID can hide a real problem.
- [ ] The SKU in the request is something sensible. The API does not check it against the menu.

## The requests

Run them in this order. Step 3 needs a pause after step 1.

### 1. Queue an order: `POST {{baseUrl}}/api/orders/queue`

- [ ] Status is `202 Accepted`
- [ ] `Content-Type` is JSON
- [ ] `OrderId` in the response equals the ID you sent
- [ ] `Status` is `Queued`
- [ ] `OrderDate` is present
- [ ] `StatusUrl` starts with `/api/orders/`
- [ ] Response arrives quickly (about 150 to 200 ms was seen)

Keys in this response are PascalCase. Postman tests need `pm.response.json().OrderId`, not `.orderId`.

### 2. Get the status straight away (optional)

- [ ] Note what you get. A `404` here is normal, because the trigger has not run yet. It is a useful thing to show, not a failure.

### 3. Wait, then get the order: `GET {{baseUrl}}/api/orders/{{orderId}}`

Wait at least 40 seconds. The two recorded runs took roughly 20 and 33 seconds. Five seconds was not enough.

- [ ] Status is `200 OK`
- [ ] `orderId` equals the ID you sent
- [ ] `customerName` equals what you sent
- [ ] `selectedItemSkus` contains the SKU(s) you sent
- [ ] `totalPrice` equals what you sent
- [ ] `status` is `Collected` (an older build stopped at `Ready`)
- [ ] `orderTimestamp` and `lastUpdated` are both present, and `lastUpdated` is later than `orderTimestamp`

### 4. Get the same order by date: `GET {{baseUrl}}/api/orders/{{orderDate}}/{{orderId}}`

`orderDate` is the UTC date of the timestamp, as `yyyy-MM-dd`.

- [ ] Status is `200 OK`
- [ ] Response has an extra `orderDate` field matching the date you used
- [ ] Everything else matches step 3

No request for this exists in the repository's Postman files yet.

### 5. List orders: `GET {{baseUrl}}/api/orders?date={{orderDate}}`

- [ ] Status is `200 OK`
- [ ] `count` equals the length of `orders`
- [ ] Your order is in `orders`
- [ ] Orders are newest first
- [ ] With no `date` parameter, you get orders from every date

No request for this exists in the repository's Postman files yet.

### 6. Change the status: `PUT {{baseUrl}}/api/orders/{{orderId}}/status`

Body: `{ "status": "Preparing" }`

- [ ] Status is `200 OK`
- [ ] `orderId` equals the ID in the URL
- [ ] `status` is `Preparing`
- [ ] `message` is `Order status updated successfully.`

### 7. Confirm the change stuck: repeat step 3

- [ ] `status` is now `Preparing`
- [ ] `lastUpdated` has moved forward

## Post-response test scripts

The repository already has scripts for the status requests. These are a starting point for the ones that are missing.

Queue order (note the capital letters):

```javascript
pm.test("Status code is 202", function () {
    pm.response.to.have.status(202);
});

pm.test("Order is reported as Queued", function () {
    const body = pm.response.json();
    pm.expect(body.OrderId).to.eql(pm.environment.get("orderId"));
    pm.expect(body.Status).to.eql("Queued");
});
```

Get by date:

```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Order date and ID match", function () {
    const body = pm.response.json();
    pm.expect(body.orderDate).to.eql(pm.environment.get("orderDate"));
    pm.expect(body.orderId).to.eql(pm.environment.get("orderId"));
});
```

## Problems found in the repository's Postman files

I went through the order requests under `Postman/` and compared them with the source. Nothing was changed. These need a decision from whoever owns the collection.

| # | Finding | Why it matters |
|---|---|---|
| 1 | The collection used in the test screenshots, "CoffeeNChill - Combined Local Tests (Part 1 + Part 2)", is not in the repository. A search for that name finds nothing. | The evidence cannot be reproduced from what is committed. It should be exported and added. |
| 2 | Order requests are spread across several places: `Postman/postman/collections/CoffeeNChill/Orders/`, `Postman/CoffeeNChill Part 2/Orders/` and `Postman/postman/collections/CoffeeNChill Part 2 API/`. `REVIEW_AND_TEST_REPORT.md` already says to keep one collection per part. | Easy to run an out-of-date copy. |
| 3 | In `.../CoffeeNChill/Orders/`, `Queue Order` queues `ORD-4002`, but `Get Order Status` and `Update Order Status` both use `ORD-5001`. | Running the folder in order would look up an order that was never queued and return `404`. |
| 4 | The same folder hard-codes `http://127.0.0.1:18080` instead of `{{baseUrl}}`. | Breaks if the host is run locally on a different port. |
| 5 | `Update Order Status` expects `Preparing`, but a processed order is already `Collected`. | It passes (the endpoint has no transition rules), but it moves a finished order backwards. |
| 6 | `New Request.request.yaml` has an empty URL. | Leftover file, will fail if run. |
| 7 | `Queue-Customer-Order` uses a fixed timestamp of `2026-10-11T11:00:00Z`. | Every order lands in the same date partition. Fine for a demo, but worth knowing. |
| 8 | Ports differ across the project: 18080 (Docker Compose), 7017 (`launchSettings.json`) and 7071 (`menu-api.md`). | Check which one your host is actually using before blaming the API. |
| 9 | No request exists for `GET /api/orders/{date}/{id}`, `GET /api/orders` or any bad-input case. | Those endpoints are untested in Postman. |
| 10 | `CoffeeNChill-Part1.postman_collection.json` at the root only covers Part 1 (no order requests). | Do not import it expecting Part 2. |
