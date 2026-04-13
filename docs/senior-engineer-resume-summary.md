# CV Update Pack (Corrected)

This update addresses your feedback explicitly:

1. Experience updated to **10 years**.
2. Stack emphasis now includes **.NET Core, Spring Boot, and Node.js**.
3. BET bullet now includes the **DB/persistence used by admin workflows** (not only Redis).
4. Cryptography and payment-security expertise are restored: **AES/TDES DUKPT, TR31, Pinblock, device attestation**.

## Corrected Summary Paragraph
Senior Software Engineer with **10 years** of full-stack experience across betting, fintech, and enterprise systems. Strong backend delivery across **.NET Core, Spring Boot, and Node.js**, with proven ownership of secure, high-performance APIs, payment integrations, and identity/access platforms (OAuth 2.0/OIDC). Hands-on expertise in **AES/TDES DUKPT, TR31, Pinblock**, and **device attestation**, combined with a practical leadership style focused on mentoring, TDD, and production reliability.

## Corrected BET Role Bullets (Concise)
- Built a secure secrets management library in .NET Core with Redis caching, API key encryption, environment-aware retrieval, and relational persistence for admin application workflows; packaged as reusable NuGet with CLI support.
- Re-engineered legacy services into modern .NET Core APIs and contributed to Angular/SolidJS modernization work, improving maintainability and delivery speed.
- Delivered integrations across betting, gaming, live-stream, and payment ecosystems (including UK49s and payment gateway integrations for the Hollywoodbets payment portal).
- Contributed to secure identity/access service patterns aligned to OAuth 2.0/OIDC.
- Mentored junior/intermediate engineers and improved engineering quality through TDD-aligned code reviews and delivery practices.

## New BET Achievement to Add: Global Suppression List + Communication Platform
Yes — this is absolutely worth adding. It shows **system design**, **event-driven architecture**, **operational ownership**, and **business impact** (delivery reliability + visibility).

### Why it strengthens your CV
- Demonstrates ownership of a multi-component production system (Worker, API, Webhooks, Admin UI).
- Shows practical distributed architecture using Kafka, provider integrations, async state transitions, and DB log consistency.
- Adds strong evidence of communication-domain expertise (delivery lifecycle, status reconciliation, observability/visibility).

### Resume-ready bullet options (pick 1–2)
- Designed and delivered a **Global Suppression List** communication platform (Worker, API, Report Webhooks, Admin UI) to centralize outbound communication controls and improve delivery governance.
- Implemented an **event-driven communication pipeline** using Kafka where Resource APIs publish events, workers process SMS/email requests, and provider callbacks reconcile final delivery states into DB logs.
- Built provider-integrated communication services (e.g., SendGrid, Africa’s Talking) with webhook-based final-status processing, improving traceability and operational visibility through an admin tool.

### Suggested insertion in BET section
Add this as a new bullet after modernization/integration bullets:
- Delivered a Global Suppression List and communication services platform (API, Worker, Webhooks, Admin UI) using Kafka-driven workflows and provider callback reconciliation to maintain accurate end-to-end message delivery status.

## Exported CV Files (PR-safe)
- `docs/ntokozo-cv-concise.md` (concise general CV version)
- `docs/ntokozo-cv-project-detailed.md` (detailed CV version with specific project mentions)


> Note: `.docx` files are binary files. Some PR or web editors do not support binary uploads/diffs; Markdown files are text-based and PR-friendly.
