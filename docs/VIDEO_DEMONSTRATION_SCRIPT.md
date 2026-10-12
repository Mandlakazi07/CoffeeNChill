# CoffeeNChill — individual demonstration video script

**Target length:** 6–8 minutes  
**Presenter:** [Insert official name]  
**Module:** CLDV6212 — Cloud Development  
**Recording style:** Screen recording with the student's real voice. Do not use a synthetic voiceover if the assessment requires the student's own voice.

> **Recording gate:** Do not record a final “everything works” demonstration yet. The available evidence confirms compilation, but the Functions host and endpoints have not been runtime-tested. The staff document code also currently uses Blob Storage rather than the required Azure File Share. Complete those checks/fixes first, or state limitations honestly and ask the lecturer how to handle an incomplete requirement.

## 0:00–0:35 — Introduction

**On screen:** Show the repository on GitHub and the project title in VS Code.

**Voiceover:**

“Hello, my name is [name], and this is my CLDV6212 Cloud Development demonstration for CoffeeNChill. This is my individual repository combining the Part 1 menu and staff-document APIs with the Part 2 queue-based order workflow. I will show the project structure, the implementation, and the test results that I was able to verify.”

## 0:35–1:20 — Repository and disclosure

**On screen:** Show the README sections for project overview, individual submission disclosure, AI disclosure and known requirement gap.

**Voiceover:**

“The repository is maintained as an individual combined submission. Some implementation and structure were carried over or adapted from the previous shared group Part 1 and Part 2 project; I have documented that provenance in the README rather than claiming every file was originally written from scratch. I also disclose the AI assistance used during code review, documentation preparation and troubleshooting.”

## 1:20–2:00 — Project structure and build

**On screen:** Expand `Functions/Menu`, `Functions/Documents`, `Functions/Orders`, `Models`, `Services`, `Validation`, the Postman files and `docker-compose.yml`. Run the build command and show the output.

```powershell
dotnet build .\CoffeeNChillFunctions\CoffeeNChillFunctions.csproj
```

**Voiceover:**

“The solution is organised into HTTP-triggered functions, data models, storage services and validation. Part 1 contains the menu and staff-document routes. Part 2 adds the order queue endpoint, queue-triggered processing and order-status endpoints. The build succeeded on this machine. A successful build confirms compilation, but I treat API behavior as verified only after running the host and exercising the endpoints.”

## 2:00–3:25 — Part 1 menu tests

**On screen:** With the Functions host already running, use Postman to show these operations and the actual response codes and bodies:

1. `POST /api/menu` — create a uniquely named test item.
2. `GET /api/menu` — confirm the item appears.
3. `GET /api/menu/category/{category}` — filter by category.
4. `PUT /api/menu/{category}/{id}` — update a field.
5. `DELETE /api/menu/{category}/{id}` — remove the test item.

**Voiceover:**

“For Part 1, I am using Postman to test menu item creation, listing, category filtering, updating and deletion. The response shown here is the actual response from the running application.”

**Production note:** Do not say an operation passed until its response is visible. Use an item identifier that does not collide with existing test data.

## 3:25–4:10 — Staff documents

**On screen:** Show upload, list and download requests after the code has been migrated to and tested with Azure File Share, as required by the brief.

**Voiceover:**

“The staff-document endpoints support upload, listing and downloading. The assignment requires an Azure File Share, so I verified these operations against the required file-share service.”

**Important:** This wording may only be used after the Blob Storage implementation has actually been migrated to Azure Files and the tests pass. If it has not, do not use this section as a successful demonstration; show the current gap honestly instead.

## 4:10–5:35 — Part 2 order queue

**On screen:** Submit `POST /api/orders/queue` in Postman. Show its real response. Then show Function logs for the queue-trigger function and query `GET /api/orders/{orderId}`. Optionally show `PUT /api/orders/{orderId}/status` with a valid status value.

Example order body (confirm it matches the current DTO before recording):

```json
{
  "orderId": "ORD-VIDEO-1001",
  "customerName": "Video Demo Customer",
  "selectedItemSkus": ["COF-001", "PAS-104"],
  "totalPrice": 65.00,
  "orderTimestamp": "2026-10-11T10:00:00Z"
}
```

**Voiceover:**

“For Part 2, the HTTP function validates the order and adds a JSON message to `order-processing-queue`. The queue-triggered function consumes the message and writes the order to the Orders table. I am showing the actual function logs and the current status returned for this order.”

**Production note:** Confirm the sample SKU names are acceptable for this implementation. If the code reaches `Collected` almost immediately, explain that the current implementation performs sequential status updates; do not imply it is a time-delayed real-world barista simulation.

## 5:35–6:25 — Docker Compose / architecture

**On screen:** If Docker is working, show `docker compose ps`, selected logs, the Compose file and the API port `http://127.0.0.1:18080`. If Docker is still unavailable, do not show a fabricated container result; describe the intended setup only and disclose that runtime verification remains outstanding.

**Voiceover:**

“The Docker Compose configuration is intended to run the Functions container with Azurite. The local API port is 18080, while Azurite exposes local Blob, Queue and Table ports. The Compose file references a published Functions image, so I rebuild or publish the image matching the final source before relying on it for the final demonstration.”

## 6:25–7:10 — Wrap-up and limitations

**On screen:** Show the verification status and the README disclosures.

**Voiceover:**

“To summarise, the .NET project compiles, while runtime behavior must be supported by the tests shown in this recording. I have also documented the reuse of parts of the earlier group implementation and the AI assistance used. Any outstanding requirement or failed test is listed rather than being described as complete. Thank you.”

## Final recording checklist

- [ ] Replace presenter and student-number placeholders.
- [ ] Build succeeds on the same code that will be submitted.
- [ ] Functions host starts with no unresolved startup errors.
- [ ] Show actual Postman requests and response codes.
- [ ] Show queue message processing logs and order status retrieval.
- [ ] Migrate and test staff documents with Azure File Share, or do not claim this requirement is complete.
- [ ] If Docker is demonstrated, show running containers and logs; do not rely on a cached old image.
- [ ] Use the student's real voice and record in one clear take or clearly edited sections.
- [ ] Upload as Unlisted to YouTube, then paste the real video URL into the README.
- [ ] Check that all disclosures, attributions and references meet the lecturer's instructions.
