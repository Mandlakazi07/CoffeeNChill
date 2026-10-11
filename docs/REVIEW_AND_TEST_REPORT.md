# CLDV6212 CoffeeNChill — Repository Review and Repair Notes

Review date: 11 October 2026
Source reviewed: ZIP snapshot `emkndn-cldv6212-2026-cldv6212-poe-ndlovu-sena-main.zip`.

## Scope and limitations

The supplied ZIP is a source snapshot, not a Git clone: it contains no `.git` directory, branch refs, or commit history. Therefore this review cannot verify the other GitHub branches, who authored which changes, or the required five meaningful commits per student. The environment used for this review did not have `dotnet` or `docker` executables available, so the application could not be compiled or run in containers here. The changes below are source-level repairs and static checks, not a claim of successful runtime testing.

## High-priority findings from the supplied source

1. **Part 1 staff document routes did not match the assignment/Postman requests.** The upload function was routed at `/api/documents`, while the collection calls `/api/documents/upload`. The download function was routed at `/api/documents/{fileName}`, while the collection calls `/api/documents/download/{fileName}`. These routes were changed to match the documented endpoints.
2. **Part 1 storage service does not meet the stated Azure File Share requirement.** `StaffFileService` uses `Azure.Storage.Blobs` and `BlobContainerClient`; the assignment specifies an Azure File Share. This remains a substantive rubric gap and must be migrated/tested against Azure Files before claiming Part 1 is complete.
3. **Upload accepted extensions that the service then rejected.** The HTTP function advertised PDF, DOC, DOCX and TXT, but `StaffFileService` only validates PDF signatures. The HTTP endpoint now advertises PDF only so the public contract matches current behavior. This is a consistency fix, not a substitute for broader file support if the team intends to implement it.
4. **Part 2 order table partition key did not follow the assignment's order-date partitioning requirement.** The original service used a constant `ORDER` partition. New records use `yyyy-MM-dd` from the order timestamp; lookup/update now query by `RowKey` so status endpoints still work across date partitions.
5. **Part 2 processing stopped at `Ready`.** The assignment describes `Received → Preparing → Ready → Collected`. The queue function now writes the final `Collected` state and logs that transition. Because the transitions happen quickly, the final table state is expected to be `Collected`; intermediate transitions are evidenced in logs unless a deliberate processing delay is added.
6. **Compose included both a published `image` and a local `build`.** The marking notes require the Compose file to reference published Docker Hub images rather than only local build paths. The local `build` block was removed. Verify the published `senandlovu/coffeenchill-functions:part2` tag is publicly pullable and corresponds to the current source.
7. **README had conflicting/placeholder deployment details.** It included placeholder Docker Hub usernames, a sample Compose configuration inconsistent with the actual port mapping, and placeholder video links. The Docker Hub table/base URL and AI disclosure sections were clarified; the video links still need replacing with real unlisted YouTube links after recording.
8. **The supplied archive does not contain Git history.** Restore or use the live group repository and make genuine, meaningful commits from each student's own account. Do not fabricate commit history or claim another member's work.

## Part 1 requirement comparison

- Menu item Azure Table schema: present in `MenuItem` (`PartitionKey`, `RowKey`, `Name`, `Description`, `Price`, `IsAvailable`).
- Menu HTTP CRUD and category filtering: source files are present; runtime behavior remains unverified.
- Staff document upload/list/download endpoints: source files are present and route mismatches were corrected.
- Staff document backing storage: **not compliant yet** — Blob Storage is used instead of the required Azure File Share.
- Standalone Dockerfile and published image: Dockerfile exists; public image publication was not independently verified.
- Postman tests and README: present, but requests and environment must be run locally. Some duplicate/overlapping Postman exports exist in the tree; use a single clearly documented collection per part for the final submission.

## Part 2 requirement comparison

- HTTP order producer and Azure Queue: source is present.
- Queue-triggered processor using `order-processing-queue`: source is present.
- Orders table and status endpoint: source is present; order-date partitioning was corrected at source level.
- Full status lifecycle: source now writes `Collected` after `Ready`; runtime behavior remains unverified.
- Docker Compose orchestration: Azurite and Functions services are configured; local build was removed so Compose relies on the published Functions image as required.
- Docker Hub images: Functions image is referenced as `senandlovu/coffeenchill-functions:part2`; verify pull access. Azurite uses the official Microsoft Container Registry image.
- Postman: multiple formats/exports exist, including hard-coded local URLs. Run the collection and align every request with the selected `baseUrl` environment before recording.
- README/videos: placeholder video URLs remain and must be replaced after real demonstrations.

## Verification performed

- Source and endpoint routes inspected.
- Compose and Postman environment configuration inspected.
- Static JSON/YAML validation should be rerun on the final archive (no application build or live storage tests were possible in this environment).

## Local test sequence before submission

1. Confirm Docker Desktop is running and the Functions image is publicly pullable: `docker pull senandlovu/coffeenchill-functions:part2`.
2. From the repository root, run `docker compose config` and resolve any errors.
3. Run `docker compose pull` followed by `docker compose up`.
4. Check `docker compose ps` and logs for host startup and queue trigger discovery.
5. Import the Part 1 and Part 2 Postman collections with the matching environment (`http://127.0.0.1:18080`). Test menu create/list/category/update/delete; upload/list/download a PDF; queue an order; check status; manually update status to `Collected` if testing the status endpoint independently.
6. Inspect Azurite Tables/Queues and confirm `Orders` uses date-based PartitionKey and OrderId RowKey.
7. Record each required video with a real human voice explaining code and design decisions. Do not use AI-generated narration.
8. Add actual video links, confirm Docker Hub image tags, and review Git commit history per student.

## AI assistance disclosure — suggested wording to review

“AI assistance (ChatGPT, OpenAI) was used on 11 October 2026 to review the supplied source snapshot against the assignment brief, identify potential endpoint/configuration and implementation gaps, suggest source edits, and help prepare review documentation. The student/team reviewed and tested the final changes, verified all references, and remains responsible for understanding and explaining the submitted code.”

Only keep this wording if it accurately describes the team's actual process. Update it to include any further assistance, testing, or code adopted. AI assistance is not a replacement for individual analysis or authentic Git contributions.

## Attribution guidance

Cite third-party code where it is actually copied or adapted. The existing learning-resource list is not proof that code was derived from those resources. Follow the institution's IIE Harvard Anglia format and include author/organisation, year/date if available, title, URL, and access date. Cite official Microsoft documentation for Azure Functions, Queue Storage, Table Storage, Azure Files, and Azurite only if those pages were consulted during implementation.
