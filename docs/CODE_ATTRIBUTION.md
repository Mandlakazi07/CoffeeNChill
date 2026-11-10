# Code Attribution, Technical References and AI-use Disclosure

**Project:** CoffeeNChill - CLDV6212 Cloud Development Part 2
**Prepared:** 12 October 2026

## Purpose and scope

This file identifies the shared project baseline, official technical documentation consulted for framework and configuration behaviour, and generative-AI assistance used during development. Inline source comments cite the relevant items below. The retry settings in `host.json` follow the Azure Queue Storage configuration guidance; `host.json` remains valid JSON and is explained here rather than commented inline. A citation to official documentation means that the documentation informed the use of an API, binding or configuration pattern; it does **not** claim that the entire CoffeeNChill implementation was copied verbatim from that page.

## Project source and implementation attribution

The individual repository began from a shared CoffeeNChill/CLDV6212 group-code baseline (EMKNDN, 2026), as disclosed in the root README. The individual repository contains subsequent project-specific implementation and changes recorded in Git history. Relevant areas include the queue producer, order processing, Orders table persistence, date-based read endpoints and Docker Compose configuration.

Microsoft and Docker documentation below is cited for the technical behaviour of the public APIs, Functions bindings, host configuration, table client and container/build features. The precise business rules, routes, payload contract, status lifecycle and deployment values are CoffeeNChill-specific implementation choices.

`host.json` and Postman collection/environment JSON files intentionally do not receive inline comments because comments are not valid JSON. Their relevant settings and provenance are documented here instead.

## Inline citation key

- Queue Storage producer and binding patterns: (Microsoft, n.d.-a; Microsoft, n.d.-b).
- .NET isolated Azure Functions model: (Microsoft, n.d.-c).
- Azure Tables client API: (Microsoft, n.d.-d).
- Compose services, network and volumes: (Docker, n.d.-a; Docker, n.d.-b; Docker, n.d.-c).
- Compose .env interpolation and YAML fragments: (Docker, n.d.-e; Docker, n.d.-f).
- Compose build override file: (Docker, n.d.-g).
- Docker multi-stage builds: (Docker, n.d.-d).
- .NET container port 8080 and non-root app user: (Microsoft, n.d.-e; Microsoft, n.d.-f).
- GitHub Actions workflow syntax, outputs and secrets: (GitHub, n.d.).
- Shared group-code baseline: (EMKNDN, 2026).
- Generative-AI assistance: (OpenAI, 2026).

## Reference list â€” IIE Harvard Anglia style

Docker. n.d.-a. *Compose file reference*. [online]. Available at: https://docs.docker.com/reference/compose-file/ [Accessed 12 October 2026].

Docker. n.d.-b. *Define and manage networks in Docker Compose*. [online]. Available at: https://docs.docker.com/reference/compose-file/networks/ [Accessed 12 October 2026].

Docker. n.d.-c. *Define and manage volumes in Docker Compose*. [online]. Available at: https://docs.docker.com/reference/compose-file/volumes/ [Accessed 12 October 2026].

Docker. n.d.-d. *Multi-stage builds*. [online]. Available at: https://docs.docker.com/build/building/multi-stage/ [Accessed 10 November 2026].

Docker. n.d.-e. *Interpolation*. [online]. Available at: https://docs.docker.com/reference/compose-file/interpolation/ [Accessed 10 November 2026].

Docker. n.d.-f. *Fragments*. [online]. Available at: https://docs.docker.com/reference/compose-file/fragments/ [Accessed 10 November 2026].

Docker. n.d.-g. *Merge Compose files*. [online]. Available at: https://docs.docker.com/reference/compose-file/merge/ [Accessed 10 November 2026].

EMKNDN. 2026. *emkndn-cldv6212-2026-cldv6212-poe-ndlovu-sena* [Source code]. Available at: https://github.com/EMKNDN/emkndn-cldv6212-2026-cldv6212-poe-ndlovu-sena [Accessed 12 October 2026].

GitHub. n.d. *Workflow syntax for GitHub Actions*. [online]. Available at: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax [Accessed 10 November 2026].

Microsoft. n.d.-a. *Azure Queue storage trigger and bindings for Azure Functions overview*. [online]. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue [Accessed 12 October 2026].

Microsoft. n.d.-b. *Azure Queue storage trigger for Azure Functions*. [online]. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger [Accessed 12 October 2026].

Microsoft. n.d.-c. *Guide for running C# Azure Functions in an isolated worker process*. [online]. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide [Accessed 12 October 2026].

Microsoft. n.d.-d. *TableClient Class (Azure.Data.Tables)*. [online]. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient?view=azure-dotnet [Accessed 12 October 2026].

Microsoft. n.d.-e. *Breaking change: Default ASP.NET Core port changed from 80 to 8080*. [online]. Available at: https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port [Accessed 10 November 2026].

Microsoft. n.d.-f. *Breaking change: New non-root 'app' user in Linux images*. [online]. Available at: https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/app-user [Accessed 10 November 2026].

OpenAI. 2026. *ChatGPT (GPT-6)*. [Large language model]. Available at: https://chatgpt.com/ [Accessed: 12 October 2026].

The Independent Institute of Education (The IIE). 2025a. *Guidelines for Responsible AI Use at The IIE*. [PDF]. Available at: https://irp.cdn-website.com/271d35b6/files/uploaded/Guidelines_for_Responsible_AI_Use_at_The_IIE.pdf [Accessed: 12 October 2026].

The Independent Institute of Education (The IIE). 2025b. *IIE Quick Reference Guide 2025: Harvard-Anglia Style Reference Guide â€” Adapted for The IIE*. [PDF]. Available at: https://irp.cdn-website.com/271d35b6/files/uploaded/The_IIE_Harvard_Anglia_Style_Reference_Guide_2025.pdf [Accessed: 12 October 2026].

## Disclosure of AI Usage in my Assessment

- **Assessment sections:** CoffeeNChill Part 2 source review and debugging; queue retry/failure-handling revisions; date-based order-read endpoint drafting; source-attribution comments, references and repository documentation.
- **AI tool:** ChatGPT (GPT-6) (OpenAI, 2026).
- **Purpose:** Explaining errors and framework behaviour; suggesting and drafting code/configuration changes; producing PowerShell workflows; helping prepare comments and reference formatting.
- **Dates used:** 11â€“12 October 2026.
- **Human review and responsibility:** The student remains responsible for understanding, reviewing and testing the code, verifying citations and adapting the documentation to the final submitted source. Runtime checks completed to date are recorded in the project history and verification notes; untested behaviour must not be represented as tested.
- **Conversation evidence:** The IIE guidance asks for a link to the actual AI conversation or accurately labelled screenshots as part of the AI-use annexure. Add a share link to this conversation or include screenshots with the formal assessment materials before submission. This repository file does not create a conversation-specific share URL automatically.

This repository-level disclosure supports transparency; it does not replace any separate annexure, declaration or format required by the lecturer.
