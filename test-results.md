# Test results

**Last updated:** 12 October 2026

This is a record of what the evidence actually shows for the order-processing part of the project. It sits alongside [VERIFICATION_STATUS.md](VERIFICATION_STATUS.md), which tracks the project as a whole.

## How to read this

- **Passed**: there is a screenshot or log showing the expected result.
- **Failed**: there is evidence of the result being wrong, or of a test not passing.
- **Unverified**: nothing in the evidence shows it either way. This does not mean it works or that it doesn't.

I did not run these tests. The results below come from the screenshots saved in the repository root (taken 11 and 12 October 2026 on the machine that has the Postman workspace and the local Functions host). Every claim names the screenshot it comes from. If a result is not in a screenshot, it is listed as unverified.

## Environment seen in the evidence

| | |
|---|---|
| Storage | Azurite, started from the npm install with `azurite.cmd --location "$env:USERPROFILE\.azurite"`. Blob on 10000, Queue on 10001, Table on 10002. |
| Functions host | Run locally from the project's `bin\output` folder, not in Docker. The worker config dump shows `WorkerIndexing: true` and the .NET isolated worker. |
| Postman | Workspace collection "CoffeeNChill - Combined Local Tests (Part 1 + Part 2)", environment "CoffeeNChill Local". |
| Base URL | Not visible in any screenshot. |

## Summary

| Result | Count |
|---|---|
| Passed | 11 (plus 2 menu requests that belong to Part 1, listed separately) |
| Failed or not met | 3 (O2, O6, O8) |
| Unverified | 16 (S3, plus the 15 items listed at the end) |

## Setup

| ID | Test | Result | Evidence | Notes |
|---|---|---|---|---|
| S1 | Azurite starts with Blob, Queue and Table services | Passed | `Screenshot 2026-10-11 204852.png` | The first attempt (`azurite`) was blocked by the PowerShell execution policy. Running `azurite.cmd` worked. Worth putting in the setup notes. |
| S2 | Functions host starts and its queue trigger begins polling | Passed | `Screenshot 2026-10-11 205414.png` | The Azurite log shows the host taking its lock lease and repeatedly checking `order-processing-queue` (404 until the queue exists). |
| S3 | The host's list of discovered functions | Unverified | `Screenshot 2026-10-11 205349.png` | Only the worker configuration dump was captured, not the "Functions:" list. |

## Order endpoints

| ID | Test | Result | Evidence | Notes |
|---|---|---|---|---|
| O1 | Queue a valid order, expect `202` with `Status: "Queued"` | Passed | `...004142.png`, `...005748.png` (same response seen again in `...005956.png`), `...011614.png` | Three separate orders. 140, 194 and 154 ms. Postman tests 3/3 each time. |
| O2 | Queue response matches the current source (date-only `OrderDate`, `StatusUrl` containing the date) | **Failed** | Same screenshots | All three show `OrderDate` as a full timestamp and `StatusUrl` as `/api/orders/{orderId}`. The source builds something different. See [order-queue-api.md](order-queue-api.md). Re-run on the final build. |
| O3 | Order appears in the table after processing | Passed | `...004205.png`, `...012508.png` | Seen indirectly through the status endpoint. The table itself was not opened. |
| O4 | `GET /api/orders/{orderId}` returns `200` with all fields | Passed | `...004205.png` (and again in `...004621.png`), `...012508.png` | 43 ms and 57 ms. Postman tests 3/3. |
| O5 | Order reaches `Collected` | Passed | `...012508.png` | Order `ORD-PM-1791760754270`, `lastUpdated` 23:19:47 UTC. |
| O6 | Order reaches `Collected` (earlier run) | **Failed** | `...004205.png` | Order `ORD-PM-1791758475499` stopped at `Ready`. Consistent with the earlier build described in `REVIEW_AND_TEST_REPORT.md`. Superseded by O5. |
| O7 | `PUT /api/orders/{orderId}/status` with a valid status returns `200` | Passed | `...004228.png` | Sent `Ready` to an order that was already `Ready`. 30 ms, Postman 1/1. The response body was `orderId`, `status`, `message`. A real change of state was not shown. |
| O8 | Processing finishes within the time the Postman request promises ("wait ~5 seconds") | Not met | `...004142.png` vs `...004205.png`, `...012508.png` | The last write was about 20 s and about 33 s after queueing. |

## Error cases that were seen

| ID | Test | Result | Evidence | Notes |
|---|---|---|---|---|
| E1 | Queue with a blank SKU returns `400` and the validation body | Passed | `...003229.png` | 244 ms. Postman showed 0/3 because its assertions expect a 202. |
| E2 | `GET` for an order that was never queued returns `404` | Passed | `...003316.png` | `ORD-PM-1791757932678`, 59 ms. Postman 0/3 for the same reason as E1. |
| E3 | `PUT` status for an order that does not exist returns `404` | Passed | `...003336.png` | Same order ID, 42 ms. Postman 0/1. |
| E4 | `GET` straight after queueing returns `404` because the trigger has not run | Passed (as expected behaviour) | `...011652.png` | `ORD-PM-1791760567434`, 101 ms. This is the same timing issue as O8. |

## Seen but outside this task

Menu requests appear in some of the same screenshots. They are included so nothing is lost, and belong to the Part 1 documentation.

| Test | Result | Evidence |
|---|---|---|
| Create menu item, `201 Created` | Passed | `...003954.png`, `...011553.png` (Postman 3/3) |
| Delete menu item, `200 OK` | Passed | `...004319.png` (Postman 2/2) |

## Unverified

None of the following appear in any screenshot or log.

**Order behaviour**

1. Log lines for each status step (`Received`, `Preparing`, `Ready`, `Collected`). This is the only way to show the middle states.
2. The `Orders` table contents: the `PartitionKey` being the order date, and `RowKey` being the order ID.
3. A message arriving in `order-processing-queue` before the trigger takes it.
4. A message ending up in `order-processing-queue-poison` after three failures.
5. Submitting the same `orderId` twice (N12).
6. Whether order `ORD-PM-1791760567434` (E4) was processed eventually. No later check was captured.
7. The queue response on the **final** build (see O2).

**Endpoints**

8. `GET /api/orders/{orderDate}/{orderId}`.
9. `GET /api/orders` and `GET /api/orders?date=`.
10. `PUT` with `Preparing` or `Collected`, and a `PUT` that actually changes the status.
11. Every `PUT` and `GET` validation case: P1 to P5, P7 to P9, R3 to R6.
12. Every queue validation case apart from N4: N1 to N3, N5 to N11.

**Environment and collection**

13. Docker Compose: building or pulling the image, `docker compose up`, the containers running, and the Docker Hub image `senandlovu/coffeenchill-functions:part2` being pullable. `PART2_DOCKER_POSTMAN_TESTING.md` says these worked, but `VERIFICATION_STATUS.md` says the opposite, and no Docker screenshot or log is in the repository. Until one is added, treat Docker as unverified.
14. The "Combined Local Tests" Postman collection is not in the repository, so nobody else can re-run it. See [postman-success-checklist.md](postman-success-checklist.md).
15. A full Postman Collection Runner run with a results summary.

## What to do next

In rough order of value:

1. Re-run the queue request on the final build and update the example in [order-queue-api.md](order-queue-api.md) (O2).
2. Run P7 and P8 from [negative-test-cases.md](negative-test-cases.md). If they return 500, tell the owner of the `.cs` files.
3. Capture the function logs for one order, showing all four status lines (unverified item 1).
4. Open the `Orders` table in Storage Explorer and screenshot a row (item 2).
5. Run through [end-to-end-testing-guide.md](end-to-end-testing-guide.md) once on Docker, and add the screenshots here.
6. Export the combined Postman collection and commit it.
