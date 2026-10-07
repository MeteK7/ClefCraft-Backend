-- ============================================================
-- ClefCraft demo data 4/4: "Relay" relationship-graph showcase board
--
-- A single board for an AI-powered customer support platform, built to demonstrate the item
-- Relationship Graph: 50 items and 72 relationships forming one connected dependency network
-- (ingestion -> embeddings -> search -> RAG -> streaming -> UI, auth chain, observability,
-- CI/CD, and a production launch that several Critical items block).
--
-- Owner: the development admin 944d0156-cb3d-466f-a1ea-5f53e3a10f8e (admin@localhost.com,
--        created by DevelopmentUserSeeder)
-- Team:  7 display-only accounts (@relay-demo.test) with no password, so they can be assigned
--        and shown by name but can't sign in.
-- Statuses and priorities are the global ones from the Initial migration, resolved by name.
-- Tags are reused by name and created when missing. No explicit ids, so it can be loaded after
-- 01-03 or on its own, and no sequence reset is needed.
--
-- Relationship direction: "source <Type> target" reads as
--   DependsOn  source depends on target          Blocks     source blocks target
--   Parent     source is the parent of target    SplitFrom  source was split out of target
--   Duplicate  source duplicates target          Related    no direction
--
-- Demo entry point: "Implement RAG orchestration service" (all of its relations are outgoing).
--
-- Prerequisite: a migrated database where the admin account exists (start the API once in
-- Development). The script refuses to run if the Relay board or the demo accounts already exist,
-- and rolls back if any of the consistency checks at the end fail.
--
--   psql -v ON_ERROR_STOP=1 -h <host> -U <user> -d <database> -f scripts/dev-seed/04_relay_demo_postgresql.sql
-- ============================================================

BEGIN;

SET search_path TO public;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public."AspNetUsers" WHERE "Id" = '944d0156-cb3d-466f-a1ea-5f53e3a10f8e') THEN
    RAISE EXCEPTION '04_relay_demo: the development admin does not exist. Start the API once in Development first.';
  END IF;
  IF EXISTS (SELECT 1 FROM public."Boards" WHERE "Title" = 'Relay Support Platform') THEN
    RAISE EXCEPTION '04_relay_demo: the Relay Support Platform board already exists.';
  END IF;
  IF EXISTS (SELECT 1 FROM public."AspNetUsers" WHERE "NormalizedEmail" LIKE '%@RELAY-DEMO.TEST') THEN
    RAISE EXCEPTION '04_relay_demo: the Relay demo accounts already exist.';
  END IF;
END $$;

-- ────────────────────────────────────────────────────────────
-- 1. TEAM
-- ────────────────────────────────────────────────────────────
CREATE TEMP TABLE relay_user ("Key" text PRIMARY KEY, "Id" text NOT NULL, "FirstName" text, "LastName" text, "Email" text) ON COMMIT DROP;

INSERT INTO relay_user VALUES
  ('admin',  '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NULL,     NULL,        NULL),
  ('priya',  '5e1a7c3d-2b4f-4c8e-9a10-7d3e5f1b2c01', 'Priya',  'Raman',     'priya.raman@relay-demo.test'),
  ('marcus', '5e1a7c3d-2b4f-4c8e-9a10-7d3e5f1b2c02', 'Marcus', 'Oyelaran',  'marcus.oyelaran@relay-demo.test'),
  ('elena',  '5e1a7c3d-2b4f-4c8e-9a10-7d3e5f1b2c03', 'Elena',  'Petrova',   'elena.petrova@relay-demo.test'),
  ('tomas',  '5e1a7c3d-2b4f-4c8e-9a10-7d3e5f1b2c04', 'Tomás',  'Herrera',   'tomas.herrera@relay-demo.test'),
  ('hannah', '5e1a7c3d-2b4f-4c8e-9a10-7d3e5f1b2c05', 'Hannah', 'Lindqvist', 'hannah.lindqvist@relay-demo.test'),
  ('daniel', '5e1a7c3d-2b4f-4c8e-9a10-7d3e5f1b2c06', 'Daniel', 'Kim',       'daniel.kim@relay-demo.test'),
  ('aisha',  '5e1a7c3d-2b4f-4c8e-9a10-7d3e5f1b2c07', 'Aisha',  'Bello',     'aisha.bello@relay-demo.test');

-- PasswordHash stays NULL: Identity rejects every password for these accounts.
INSERT INTO public."AspNetUsers" (
  "Id", "FirstName", "LastName", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
  "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
  "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount"
)
SELECT u."Id", u."FirstName", u."LastName", u."Email", UPPER(u."Email"), u."Email", UPPER(u."Email"),
       true, NULL, UPPER(REPLACE(gen_random_uuid()::text, '-', '')), gen_random_uuid()::text,
       false, false, true, 0
FROM relay_user u
WHERE u."Key" <> 'admin';

-- ────────────────────────────────────────────────────────────
-- 2. TAGS (reused by name, created when missing)
-- ────────────────────────────────────────────────────────────
INSERT INTO public."Tags" ("Name", "DateCreated", "CreatedBy")
SELECT v."Name", NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM (VALUES ('AI/ML'), ('Backend'), ('Frontend'), ('DevOps'), ('API'), ('Database'), ('Testing'),
             ('Performance'), ('Documentation'), ('Security'), ('Observability')) AS v("Name")
WHERE NOT EXISTS (SELECT 1 FROM public."Tags" t WHERE t."Name" = v."Name");

-- ────────────────────────────────────────────────────────────
-- 3. BOARD, MEMBERS, COLUMNS
-- ────────────────────────────────────────────────────────────
INSERT INTO public."Boards" ("Title", "OwnerUserId", "DateCreated", "CreatedBy")
VALUES ('Relay Support Platform', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() - INTERVAL '62 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e');

CREATE TEMP TABLE relay_board ON COMMIT DROP AS
SELECT "Id" FROM public."Boards" WHERE "Title" = 'Relay Support Platform';

-- Board access is membership-based; the owner needs a row too.
INSERT INTO public."BoardMembers" ("BoardId", "UserId", "DateCreated", "CreatedBy")
SELECT b."Id", u."Id", NOW() - INTERVAL '62 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM relay_board b CROSS JOIN relay_user u;

WITH cols AS (
  INSERT INTO public."BoardColumns" ("Title", "DateCreated", "CreatedBy") VALUES
    ('Backlog',     NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
    ('To Do',       NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
    ('In Progress', NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
    ('In Review',   NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
    ('Done',        NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
  RETURNING "Id"
)
INSERT INTO public."BoardColumnMappings" ("BoardId", "BoardColumnId", "DateCreated", "CreatedBy")
SELECT b."Id", c."Id", NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM relay_board b CROSS JOIN cols c;

INSERT INTO public."BoardTags" ("BoardId", "TagId", "DateCreated", "CreatedBy")
SELECT b."Id", t."Id", NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM relay_board b
JOIN public."Tags" t ON t."Name" IN ('AI/ML', 'Backend', 'Frontend', 'DevOps', 'API', 'Database', 'Testing',
                                     'Performance', 'Documentation', 'Security', 'Observability')
WHERE t."Id" = (SELECT MIN(x."Id") FROM public."Tags" x WHERE x."Name" = t."Name");

-- ────────────────────────────────────────────────────────────
-- 4. ITEMS
-- ────────────────────────────────────────────────────────────
-- The column is the one named like the status. Due and created dates are relative to the load
-- time: Done items are due in the past, open items in the future, some backlog items have none.
CREATE TEMP TABLE relay_item (
  "Key"         text PRIMARY KEY,
  "Status"      text NOT NULL,
  "Priority"    text NOT NULL,
  "Assignee"    text,
  "Creator"     text NOT NULL,
  "Estimated"   double precision,
  "Spent"       double precision,
  "DueDays"     int,
  "CreatedDays" int NOT NULL,
  "Tags"        text[] NOT NULL,
  "Title"       text NOT NULL,
  "Description" text NOT NULL
) ON COMMIT DROP;

INSERT INTO relay_item VALUES
-- ── Platform foundations ─────────────────────────────────────
('DB-SCHEMA', 'Done', 'Critical', 'priya', 'priya', 12, 14, -38, 58, ARRAY['Database', 'Backend'],
 'Provision PostgreSQL schema for conversation, message and ticket persistence',
 'Create the core relational schema with EF Core migrations: tenants, customers, conversations, messages (role, content, token count) and support tickets. Document storage, the job queue, vector search and conversation context loading all build on these tables.'),
('API-HOST', 'Done', 'High', 'priya', 'priya', 6, 6, -40, 60, ARRAY['Backend', 'API'],
 'Set up ASP.NET Core API host with correlation IDs and ProblemDetails errors',
 'Clean-architecture solution layout, health checks, global exception handling mapped to RFC 7807 ProblemDetails, and a correlation id propagated through logs and outgoing HTTP calls. This is the middleware pipeline that rate limiting, tracing and the AI endpoints plug into.'),
('KEYVAULT', 'Done', 'Critical', 'daniel', 'daniel', 5, 5, -42, 57, ARRAY['Security', 'DevOps'],
 'Move API keys and connection strings into a managed secret store',
 'Load database credentials, token signing keys and LLM provider API keys from the cloud secret store through a managed identity. Nothing secret remains in app settings or pipeline variables.'),
('REDIS', 'Done', 'Medium', 'hannah', 'hannah', 4, 5, -33, 52, ARRAY['DevOps', 'Performance'],
 'Provision Redis for caching and distributed locks',
 'Managed Redis per environment with TLS and connection multiplexing through StackExchange.Redis. Shared by response caching, rate-limit counters and the session revocation list.'),
('WORKERS', 'Done', 'High', 'priya', 'priya', 8, 10, -26, 47, ARRAY['Backend'],
 'Set up background job workers for asynchronous processing',
 'Separate worker service with a PostgreSQL-backed job queue: retries, exponential backoff and a dead-letter table. Embedding generation, re-indexing and notification delivery run here instead of in the request path.'),
('CACHE-KB', 'To Do', 'Medium', 'marcus', 'marcus', 5, 0, 18, 9, ARRAY['Performance', 'Backend'],
 'Add Redis-backed response caching for frequently requested knowledge articles',
 'Cache the article payloads served to the help widget for 10 minutes, keyed by tenant and article version, and invalidate them on re-index. About 60% of widget traffic hits the 50 most-read articles.'),
('RATE-LIMIT', 'In Progress', 'High', 'marcus', 'marcus', 6, 3, 5, 14, ARRAY['API', 'Security'],
 'Add rate limiting middleware to public API endpoints',
 'Sliding-window limits per API key and per tenant using Redis counters, so the limits hold across API replicas. Returns 429 with a Retry-After header. Protects the LLM budget from abusive widget traffic.'),
('THROTTLE-DUP', 'Backlog', 'Medium', NULL, 'tomas', NULL, 0, NULL, 6, ARRAY['API'],
 'Add API request throttling for the public chat widget',
 'Filed from the frontend side after the widget was flooded with requests during a customer demo. Covered by the rate limiting middleware task, whose per-key sliding window also applies to the widget endpoints; kept for reference until that ships.'),
('NOTIFY', 'To Do', 'Medium', 'marcus', 'priya', 8, 0, 20, 12, ARRAY['Backend'],
 'Implement notification service for ticket escalation events',
 'Send email and in-app (SignalR) notifications when a conversation is escalated, a ticket is assigned or an SLA is about to breach. Sends are queued as background jobs so a slow mail provider never blocks the API.'),
('MIGRATION-TENANT', 'In Progress', 'Critical', 'marcus', 'priya', 10, 6, 7, 15, ARRAY['Database', 'Security'],
 'Add database migration for tenant isolation on conversations and documents',
 'Add a non-null TenantId to conversations, messages, documents and chunks, backfill it from the owning customer, and enable row-level security policies. Has to run before production: v1.0 is multi-tenant from day one.'),
-- ── Authentication and authorization ─────────────────────────
('AUTH-INFRA', 'Done', 'Critical', 'daniel', 'daniel', 16, 18, -36, 58, ARRAY['Security', 'Backend'],
 'Configure authentication and authorization infrastructure',
 'Wire up ASP.NET Core Identity with an OpenID Connect provider, issue tokens carrying tenant and role claims, and register the Agent, Supervisor and Admin authorization policies every endpoint uses. The tenant claim is also what scopes knowledge-base retrieval to a single customer.'),
('AUTH-JWT', 'Done', 'Critical', 'daniel', 'daniel', 8, 9, -30, 50, ARRAY['Security', 'API'],
 'Implement JWT access-token validation middleware',
 'Validate signature, issuer, audience and expiry on every request, map the claims to the current user and tenant, and return a 401 ProblemDetails on failure. Access tokens live 15 minutes; signing keys come from the secret store.'),
('AUTH-REFRESH', 'In Review', 'Critical', 'daniel', 'daniel', 10, 9, 2, 21, ARRAY['Security', 'Backend'],
 'Implement refresh-token rotation and session revocation',
 'Issue a new refresh token on every refresh and invalidate the old one; reusing a rotated token revokes the whole session family. Revoked sessions are kept in Redis so revocation applies on every API instance before the access token expires. Required by the launch security review.'),
('AUTH-RBAC', 'In Progress', 'High', 'daniel', 'priya', 8, 4, 6, 16, ARRAY['Security', 'API'],
 'Add role-based access control to administration endpoints',
 'Protect the /admin endpoints (knowledge-base management, tenant settings, user management) with the Admin and Supervisor policies, with integration tests asserting 403 for agent tokens. The knowledge-base admin console can''t ship without it.'),
-- ── Document ingestion and retrieval ─────────────────────────
('INGEST-EPIC', 'In Progress', 'High', 'elena', 'elena', 40, 22, 12, 45, ARRAY['AI/ML', 'Backend'],
 'Implement complete document ingestion pipeline for the knowledge base',
 'The original end-to-end ingestion task: upload, text extraction, chunking and indexing of customer knowledge-base content. Too large for one sprint, so extraction, chunking and the help-center import were split out of it. Stays open until the HTML import lands.'),
('DOC-UPLOAD', 'Done', 'Medium', 'marcus', 'marcus', 6, 7, -28, 44, ARRAY['API', 'Backend'],
 'Build knowledge-base document upload API with blob storage',
 'Multipart upload endpoint (PDF, DOCX and Markdown, up to 25 MB) that stores the original in blob storage, records document metadata per tenant and enqueues processing. The entry point for everything the RAG pipeline knows.'),
('DOC-EXTRACT', 'Done', 'Medium', 'elena', 'elena', 8, 9, -21, 40, ARRAY['AI/ML', 'Backend'],
 'Implement PDF and DOCX text extraction for uploaded documents',
 'Extract text with headings and page numbers preserved (PdfPig and OpenXML), so every chunk can cite the page it came from. Split from the document ingestion pipeline task.'),
('DOC-CHUNK', 'Done', 'High', 'elena', 'elena', 10, 12, -16, 40, ARRAY['AI/ML', 'Backend'],
 'Implement document chunking pipeline for uploaded knowledge-base files',
 'Heading-aware chunking at about 500 tokens with a 50-token overlap, keeping source document, section title and page on every chunk. Chunk boundaries directly affect retrieval quality and citation accuracy. Split from the document ingestion pipeline task.'),
('DOC-HTML', 'To Do', 'Low', 'elena', 'elena', 6, 0, 24, 40, ARRAY['AI/ML'],
 'Import help-center HTML articles into the knowledge base',
 'Crawl a tenant''s public help center from its sitemap, strip the navigation chrome and feed article bodies into the existing chunking pipeline. Split from the ingestion pipeline task; lower priority because most launch customers upload PDFs.'),
('PGVECTOR', 'Done', 'High', 'hannah', 'marcus', 4, 5, -24, 38, ARRAY['Database', 'AI/ML'],
 'Enable pgvector and create an HNSW index for chunk embeddings',
 'Enable the pgvector extension, add a vector(1536) column to document chunks and build an HNSW index with cosine distance. Keeps vector search inside PostgreSQL instead of adding a separate vector database.'),
('EMBED', 'Done', 'High', 'elena', 'elena', 8, 10, -9, 30, ARRAY['AI/ML'],
 'Generate vector embeddings for indexed document chunks',
 'Background job that batches new chunks (up to 96 per request), calls the embedding model and writes the vectors to the pgvector column. Retries on provider rate limits and re-embeds a document''s chunks when it is uploaded again.'),
('SEARCH', 'In Review', 'High', 'elena', 'elena', 10, 9, 1, 20, ARRAY['AI/ML', 'API'],
 'Implement semantic vector search endpoint',
 'Accept a user query, generate its embedding, search the tenant''s chunks in the HNSW index and return the top-k chunks with scores and source metadata. Used by the RAG orchestration service and by the related-articles panel in the conversation workspace.'),
('EVAL', 'Backlog', 'Medium', 'elena', 'elena', 12, 0, 35, 10, ARRAY['AI/ML', 'Testing'],
 'Evaluate retrieval quality against a 200-question golden set',
 'Build a labelled set of 200 real support questions with their expected source chunks, and track MRR@5 and recall@10 per release. Gives a regression signal before changing chunk size, embedding model or index parameters.'),
-- ── AI answers ───────────────────────────────────────────────
('RAG', 'In Progress', 'High', 'priya', 'priya', 24, 11, 9, 18, ARRAY['AI/ML', 'Backend'],
 'Implement RAG orchestration service',
 'The core answer pipeline: retrieve relevant chunks through semantic search, assemble a grounded prompt with citations, trim the conversation history to the token budget, call the LLM through the provider gateway and return the answer with its sources. Must only retrieve documents from the caller''s tenant and never send unredacted PII to the provider.'),
('RAG-PROMPT', 'In Progress', 'High', 'elena', 'priya', 8, 3, 6, 14, ARRAY['AI/ML'],
 'Assemble grounded prompts with retrieved chunks and citation markers',
 'Build the system prompt and context block from the retrieved chunks, tagging each with a [n] marker that maps back to its document and page. The model is instructed to answer only from that context and to say so when it is insufficient.'),
('RAG-CONTEXT', 'To Do', 'Medium', 'marcus', 'priya', 6, 0, 13, 14, ARRAY['AI/ML', 'Backend'],
 'Trim conversation history to the model''s token budget',
 'Load the conversation''s earlier messages and drop or summarize the oldest turns so prompt, history and retrieved context fit the model''s context window. The system prompt and the last three exchanges are always kept.'),
('LLM-GATEWAY', 'To Do', 'High', 'priya', 'priya', 10, 0, 11, 14, ARRAY['AI/ML', 'Backend'],
 'Add LLM provider abstraction with fallback on rate-limit errors',
 'One interface for chat-completion and streaming calls, with provider API keys from the secret store, retries with jitter, and automatic fallback to a secondary model on 429 and 5xx responses. Emits token usage and latency for every call so cost can be tracked.'),
('STREAM', 'To Do', 'High', 'marcus', 'priya', 8, 0, 15, 13, ARRAY['API', 'AI/ML'],
 'Implement streaming AI response endpoint (SSE)',
 'Expose answers as Server-Sent Events so the workspace renders tokens as they arrive, with a final event carrying citations and token usage. A client disconnect cancels the upstream LLM call.'),
('PII', 'To Do', 'Critical', 'daniel', 'daniel', 12, 0, 16, 11, ARRAY['Security', 'AI/ML'],
 'Redact PII from conversation text before it is sent to the LLM provider',
 'Detect and mask email addresses, phone numbers, card numbers and national ids in customer messages before prompt assembly, and restore them in the rendered answer where needed. A contractual requirement in the launch customers'' data processing agreements.'),
-- ── Frontend ─────────────────────────────────────────────────
('UI-SHELL', 'Done', 'Medium', 'tomas', 'tomas', 8, 8, -27, 48, ARRAY['Frontend'],
 'Scaffold Angular app shell with routing, auth guard and design tokens',
 'Standalone-component Angular app with lazy-loaded routes, an HTTP interceptor that attaches and refreshes access tokens, a route guard, and shared design tokens for the light and dark themes.'),
('UI-WORKSPACE', 'In Progress', 'High', 'tomas', 'tomas', 20, 8, 10, 17, ARRAY['Frontend'],
 'Implement Angular AI conversation workspace',
 'Three-pane agent workspace: the conversation list, the live conversation with AI-suggested answers, and a related-articles panel. Consumes the SSE streaming endpoint for answers and the semantic search endpoint for related articles.'),
('UI-STREAM-RENDER', 'To Do', 'Medium', 'tomas', 'tomas', 6, 0, 17, 12, ARRAY['Frontend', 'AI/ML'],
 'Render streamed answers with inline citations and source previews',
 'Append streamed tokens without layout jumps, turn [n] citation markers into chips, and show the cited chunk and document page in a hover preview. Subtask of the conversation workspace.'),
('UI-ADMIN', 'To Do', 'Medium', 'tomas', 'admin', 12, 0, 22, 11, ARRAY['Frontend'],
 'Build knowledge-base admin console for uploads and re-indexing',
 'Admin screen to upload documents, follow each document''s processing state (extracting, chunking, embedding, indexed, failed) and trigger a re-index. Only visible to the Admin and Supervisor roles.'),
('UI-HANDOFF', 'Backlog', 'Medium', 'tomas', 'admin', 6, 0, 32, 8, ARRAY['Frontend'],
 'Add "escalate to human agent" handoff to the conversation workspace',
 'Let the customer or the AI hand a conversation over to a human agent, carrying the transcript and an AI-written summary along, and notify the supervisor on duty.'),
('UI-POLISH', 'Backlog', 'Low', NULL, 'tomas', 3, 0, NULL, 7, ARRAY['Frontend'],
 'Polish empty states and loading skeletons in the conversation list',
 'Replace the blank list with an illustrated empty state and show skeleton rows while conversations load. Raised in usability testing; not needed for the launch.'),
-- ── Observability ────────────────────────────────────────────
('OTEL', 'In Progress', 'High', 'hannah', 'hannah', 10, 5, 8, 19, ARRAY['Observability', 'DevOps'],
 'Configure OpenTelemetry tracing across API and background workers',
 'Instrument ASP.NET Core, HttpClient, EF Core and the job workers with OpenTelemetry and export to the collector, so one trace follows a question from the API through retrieval, the LLM call and any queued jobs.'),
('OTEL-DUP', 'Backlog', 'Low', NULL, 'aisha', NULL, 0, NULL, 5, ARRAY['Observability'],
 'Instrument background workers with distributed tracing',
 'Opened while debugging a slow re-index job. Already in scope of the OpenTelemetry tracing task, which covers the background workers as well as the API.'),
('DASHBOARDS', 'To Do', 'Medium', 'hannah', 'hannah', 6, 0, 19, 12, ARRAY['Observability'],
 'Build Grafana dashboards for LLM latency, token usage and cost',
 'Dashboards for time-to-first-token, full answer latency (p50/p95), tokens and spend per tenant and per model, and the fallback rate. Built on the OpenTelemetry metrics and the usage data emitted by the LLM gateway.'),
('SLO', 'Backlog', 'Medium', 'hannah', 'admin', 4, 0, 30, 10, ARRAY['Observability'],
 'Define SLOs and alerting for AI answer latency and error rate',
 'Agree on launch SLOs (for example: 95% of answers start streaming within 2 s, under 1% failed answers) and wire burn-rate alerts to the on-call rotation. A launch requirement from the customer success team.'),
('RUNBOOK', 'Backlog', 'Low', 'hannah', 'hannah', 3, 0, 40, 7, ARRAY['Documentation', 'Observability'],
 'Write on-call runbook for LLM provider outages',
 'What to check and do when the primary LLM provider degrades: confirm the fallback model is serving, watch spend on the secondary model, and decide when to switch the widget to "leave a message" mode.'),
-- ── CI/CD, testing, security, release ────────────────────────
('CI', 'Done', 'High', 'aisha', 'aisha', 8, 9, -31, 50, ARRAY['Testing', 'DevOps'],
 'Create CI pipeline for backend unit and integration tests',
 'Runs on every pull request: build, unit tests, and integration tests against an ephemeral PostgreSQL with pgvector through Testcontainers. A red build blocks the merge.'),
('E2E', 'Backlog', 'Medium', 'aisha', 'aisha', 10, 0, 28, 9, ARRAY['Testing', 'Frontend'],
 'Add Playwright end-to-end tests for the conversation workspace',
 'Cover sign-in, asking a question, receiving a streamed answer with citations, and escalation, with a stubbed LLM provider so the suite stays deterministic. Runs in CI against a seeded environment.'),
('ZAP', 'To Do', 'Medium', 'daniel', 'daniel', 4, 0, 21, 9, ARRAY['Security', 'Testing'],
 'Run an OWASP ZAP baseline scan in the CI pipeline',
 'Nightly ZAP baseline scan against staging, with the findings published to the pipeline summary. High-severity findings fail the run.'),
('IMAGES', 'In Review', 'Medium', 'hannah', 'hannah', 5, 5, 2, 15, ARRAY['DevOps'],
 'Build and publish container images on merge to main',
 'Multi-stage Docker builds for the API, the workers and the Angular app, tagged with the commit SHA and pushed to the container registry once CI passes. The deployment pipelines consume these images.'),
('TERRAFORM', 'In Progress', 'Critical', 'hannah', 'hannah', 16, 10, 9, 29, ARRAY['DevOps'],
 'Provision staging and production environments with Terraform',
 'Kubernetes cluster, managed PostgreSQL with pgvector, Redis, blob storage and networking, all defined in Terraform. Staging is live; production is waiting on the network review.'),
('CD-MIGRATIONS', 'To Do', 'Critical', 'hannah', 'priya', 6, 0, 14, 12, ARRAY['DevOps', 'Database'],
 'Run database migrations automatically in the deployment pipeline',
 'Apply EF Core migration bundles in a pre-deploy job with a lock and a failure gate, so schema changes reach staging and production in the same pipeline run as the code that needs them.'),
('LOADTEST', 'Backlog', 'High', 'aisha', 'aisha', 8, 0, 30, 8, ARRAY['Testing', 'Performance'],
 'Load-test the streaming endpoint at 500 concurrent conversations',
 'k6 scenario with 500 concurrent streaming conversations against staging and a stubbed LLM, measuring time-to-first-token, connection limits and worker queue depth. The results gate the production launch.'),
('PROD', 'Backlog', 'Critical', 'admin', 'admin', 8, 0, 45, 20, ARRAY['DevOps'],
 'Enable production deployment for the v1.0 launch',
 'Go-live for the first three customers. Gated on tenant isolation, automated migrations, session revocation, PII redaction, a passing load test, and agreed SLOs with alerting.'),
-- ── Documentation ────────────────────────────────────────────
('ADR-RAG', 'In Review', 'Medium', 'elena', 'elena', 3, 3, 3, 12, ARRAY['Documentation', 'AI/ML'],
 'Write architecture decision record for the RAG retrieval design',
 'Records why we chose pgvector over a dedicated vector database, heading-aware chunking, top-k = 8 and citation markers, and the conditions under which each choice should be revisited.'),
('API-DOCS', 'To Do', 'Medium', 'admin', 'admin', 6, 0, 26, 10, ARRAY['Documentation', 'API'],
 'Publish public API reference with authentication and rate-limit examples',
 'Generate the reference from OpenAPI and add guides for obtaining and refreshing tokens and for handling 429 responses with Retry-After. Needed by customers integrating the chat widget API.');

INSERT INTO public."BoardItems" (
  "BoardId", "BoardColumnId", "Title", "Description",
  "AssigneeId", "DueDate", "EstimatedTime", "TimeSpent",
  "DateCreated", "CreatedBy"
)
SELECT b."Id", c."Id", i."Title", i."Description",
       a."Id", NOW() + i."DueDays" * INTERVAL '1 day', i."Estimated", i."Spent",
       NOW() - i."CreatedDays" * INTERVAL '1 day', cr."Id"
FROM relay_item i
CROSS JOIN relay_board b
LEFT JOIN relay_user a ON a."Key" = i."Assignee"
LEFT JOIN relay_user cr ON cr."Key" = i."Creator"
LEFT JOIN public."BoardColumnMappings" m ON m."BoardId" = b."Id"
LEFT JOIN public."BoardColumns" c ON c."Id" = m."BoardColumnId" AND c."Title" = i."Status"
WHERE c."Id" IS NOT NULL;

CREATE TEMP TABLE relay_item_id ON COMMIT DROP AS
SELECT i."Key", bi."Id"
FROM relay_item i
JOIN public."BoardItems" bi ON bi."Title" = i."Title" AND bi."BoardId" = (SELECT "Id" FROM relay_board);

-- ────────────────────────────────────────────────────────────
-- 5. STATUS, PRIORITY, TAGS
-- ────────────────────────────────────────────────────────────
-- Resolved by name against the GLOBAL values (availability row with BoardId IS NULL). A missing
-- name leaves the id NULL and fails the NOT NULL column.
INSERT INTO public."BoardItemStatuses" ("BoardItemId", "StatusId", "DateCreated", "CreatedBy")
SELECT ids."Id", g."Id", NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM relay_item i
JOIN relay_item_id ids ON ids."Key" = i."Key"
LEFT JOIN (
  SELECT x."Id", x."Name" FROM public."Statuses" x
  JOIN public."BoardStatuses" a ON a."StatusId" = x."Id" AND a."BoardId" IS NULL
) g ON g."Name" = i."Status";

INSERT INTO public."BoardItemPriorities" ("BoardItemId", "PriorityId", "DateCreated", "CreatedBy")
SELECT ids."Id", g."Id", NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM relay_item i
JOIN relay_item_id ids ON ids."Key" = i."Key"
LEFT JOIN (
  SELECT x."Id", x."Name" FROM public."Priorities" x
  JOIN public."BoardPriorities" a ON a."PriorityId" = x."Id" AND a."BoardId" IS NULL
) g ON g."Name" = i."Priority";

INSERT INTO public."BoardItemTags" ("BoardItemId", "TagId", "DateCreated", "CreatedBy")
SELECT ids."Id", (SELECT MIN(t."Id") FROM public."Tags" t WHERE t."Name" = tag."Name"), NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM relay_item i
JOIN relay_item_id ids ON ids."Key" = i."Key"
CROSS JOIN LATERAL unnest(i."Tags") AS tag("Name");

-- ────────────────────────────────────────────────────────────
-- 6. RELATIONSHIPS
-- ────────────────────────────────────────────────────────────
CREATE TEMP TABLE relay_relation ("Source" text NOT NULL, "Type" text NOT NULL, "Target" text NOT NULL) ON COMMIT DROP;

INSERT INTO relay_relation VALUES
-- Hero: every relation of the RAG service is outgoing
('RAG', 'DependsOn', 'SEARCH'),
('RAG', 'DependsOn', 'API-HOST'),
('RAG', 'DependsOn', 'AUTH-INFRA'),        -- tenant claim scopes retrieval
('RAG', 'DependsOn', 'PII'),
('RAG', 'Parent',    'RAG-PROMPT'),
('RAG', 'Parent',    'RAG-CONTEXT'),
('RAG', 'Parent',    'LLM-GATEWAY'),
('RAG', 'Blocks',    'STREAM'),
('RAG', 'Related',   'ADR-RAG'),
('RAG', 'Related',   'OTEL'),
-- Retrieval chain: ingestion -> chunks -> embeddings -> search, converging on the schema
('SEARCH',      'DependsOn', 'EMBED'),
('SEARCH',      'DependsOn', 'PGVECTOR'),
('SEARCH',      'Related',   'EVAL'),
('EMBED',       'DependsOn', 'DOC-CHUNK'),
('EMBED',       'DependsOn', 'PGVECTOR'),
('EMBED',       'DependsOn', 'WORKERS'),
('DOC-CHUNK',   'DependsOn', 'DOC-EXTRACT'),
('DOC-EXTRACT', 'DependsOn', 'DOC-UPLOAD'),
('DOC-UPLOAD',  'DependsOn', 'DB-SCHEMA'),
('PGVECTOR',    'DependsOn', 'DB-SCHEMA'),
('WORKERS',     'DependsOn', 'DB-SCHEMA'),
('RAG-CONTEXT', 'DependsOn', 'DB-SCHEMA'),
('DOC-EXTRACT', 'SplitFrom', 'INGEST-EPIC'),
('DOC-CHUNK',   'SplitFrom', 'INGEST-EPIC'),
('DOC-HTML',    'SplitFrom', 'INGEST-EPIC'),
('DOC-HTML',    'DependsOn', 'DOC-CHUNK'),
-- Auth chain
('AUTH-REFRESH', 'DependsOn', 'AUTH-JWT'),
('AUTH-JWT',     'DependsOn', 'AUTH-INFRA'),
('AUTH-INFRA',   'DependsOn', 'KEYVAULT'),
('AUTH-RBAC',    'DependsOn', 'AUTH-INFRA'),
('AUTH-REFRESH', 'DependsOn', 'REDIS'),    -- revocation list
('UI-SHELL',     'DependsOn', 'AUTH-JWT'),
-- API, caching, async work, LLM access
('RATE-LIMIT',   'DependsOn', 'API-HOST'),
('RATE-LIMIT',   'DependsOn', 'REDIS'),
('THROTTLE-DUP', 'Duplicate', 'RATE-LIMIT'),
('CACHE-KB',     'DependsOn', 'REDIS'),
('NOTIFY',       'DependsOn', 'WORKERS'),
('LLM-GATEWAY',  'DependsOn', 'KEYVAULT'),
('STREAM',       'DependsOn', 'LLM-GATEWAY'),
('MIGRATION-TENANT', 'DependsOn', 'DB-SCHEMA'),
-- Frontend
('UI-WORKSPACE',     'DependsOn', 'STREAM'),
('UI-WORKSPACE',     'DependsOn', 'SEARCH'),
('UI-WORKSPACE',     'DependsOn', 'UI-SHELL'),
('UI-WORKSPACE',     'Parent',    'UI-STREAM-RENDER'),
('UI-WORKSPACE',     'Related',   'UI-POLISH'),
('UI-STREAM-RENDER', 'DependsOn', 'RAG-PROMPT'),   -- citation marker format
('UI-HANDOFF',       'DependsOn', 'NOTIFY'),
('UI-HANDOFF',       'DependsOn', 'UI-WORKSPACE'),
('UI-ADMIN',         'DependsOn', 'DOC-UPLOAD'),
('UI-ADMIN',         'DependsOn', 'AUTH-RBAC'),
-- Observability
('OTEL',       'DependsOn', 'API-HOST'),
('OTEL-DUP',   'Duplicate', 'OTEL'),
('DASHBOARDS', 'DependsOn', 'OTEL'),
('DASHBOARDS', 'DependsOn', 'LLM-GATEWAY'),
('SLO',        'DependsOn', 'DASHBOARDS'),
('RUNBOOK',    'Related',   'LLM-GATEWAY'),
-- CI/CD, testing, security, docs
('E2E',           'DependsOn', 'UI-WORKSPACE'),
('E2E',           'DependsOn', 'CI'),
('ZAP',           'DependsOn', 'CI'),
('IMAGES',        'DependsOn', 'CI'),
('CD-MIGRATIONS', 'DependsOn', 'IMAGES'),
('CD-MIGRATIONS', 'DependsOn', 'TERRAFORM'),
('LOADTEST',      'DependsOn', 'STREAM'),
('API-DOCS',      'DependsOn', 'AUTH-JWT'),
('API-DOCS',      'DependsOn', 'RATE-LIMIT'),
-- Production launch gates
('MIGRATION-TENANT', 'Blocks',    'PROD'),
('CD-MIGRATIONS',    'Blocks',    'PROD'),
('AUTH-REFRESH',     'Blocks',    'PROD'),
('PII',              'Blocks',    'PROD'),
('LOADTEST',         'Blocks',    'PROD'),
('SLO',              'Blocks',    'PROD'),
('PROD',             'DependsOn', 'TERRAFORM');

-- BoardItemRelationType: Parent 0, Blocks 1, DependsOn 2, Related 3, Duplicate 4, SplitFrom 5
INSERT INTO public."BoardItemRelations" ("SourceBoardItemId", "TargetBoardItemId", "RelationType", "DateCreated", "CreatedBy")
SELECT s."Id", t."Id",
       CASE r."Type" WHEN 'Parent' THEN 0 WHEN 'Blocks' THEN 1 WHEN 'DependsOn' THEN 2
                     WHEN 'Related' THEN 3 WHEN 'Duplicate' THEN 4 WHEN 'SplitFrom' THEN 5 END,
       NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM relay_relation r
LEFT JOIN relay_item_id s ON s."Key" = r."Source"
LEFT JOIN relay_item_id t ON t."Key" = r."Target";

-- ────────────────────────────────────────────────────────────
-- 7. CONSISTENCY CHECKS (any failure rolls the whole script back)
-- ────────────────────────────────────────────────────────────
DO $$
DECLARE
  n int;
BEGIN
  SELECT COUNT(*) INTO n FROM relay_item_id;
  IF n <> 50 OR (SELECT COUNT(*) FROM relay_item) <> 50 THEN
    RAISE EXCEPTION '04_relay_demo: expected 50 items with unique titles, resolved %.', n;
  END IF;

  SELECT COUNT(*) INTO n FROM relay_item i LEFT JOIN relay_user u ON u."Key" = i."Assignee"
  WHERE i."Assignee" IS NOT NULL AND u."Id" IS NULL;
  IF n > 0 THEN RAISE EXCEPTION '04_relay_demo: % items have an unknown assignee.', n; END IF;

  SELECT COUNT(*) INTO n FROM relay_relation r
  WHERE r."Source" NOT IN (SELECT "Key" FROM relay_item_id) OR r."Target" NOT IN (SELECT "Key" FROM relay_item_id);
  IF n > 0 THEN RAISE EXCEPTION '04_relay_demo: % relationships refer to an unknown item key.', n; END IF;

  SELECT COUNT(*) INTO n FROM public."BoardItemRelations" WHERE "SourceBoardItemId" IN (SELECT "Id" FROM relay_item_id);
  IF n <> 72 THEN RAISE EXCEPTION '04_relay_demo: expected 72 relationships, found %.', n; END IF;

  -- At most one relationship per pair of items, in either direction.
  SELECT COUNT(*) - COUNT(DISTINCT (LEAST("Source", "Target"), GREATEST("Source", "Target"))) INTO n FROM relay_relation;
  IF n > 0 THEN RAISE EXCEPTION '04_relay_demo: % item pairs have more than one relationship.', n; END IF;

  -- A finished item blocks nothing, nothing blocks a finished item, and a finished item only
  -- depends on finished work.
  SELECT COUNT(*) INTO n FROM relay_relation r
  JOIN relay_item s ON s."Key" = r."Source" JOIN relay_item t ON t."Key" = r."Target"
  WHERE (r."Type" = 'Blocks' AND (s."Status" = 'Done' OR t."Status" = 'Done'))
     OR (r."Type" = 'DependsOn' AND s."Status" = 'Done' AND t."Status" <> 'Done');
  IF n > 0 THEN RAISE EXCEPTION '04_relay_demo: % relationships contradict the item statuses.', n; END IF;

  -- No dependency cycles. "A DependsOn B" is the edge A -> B; "A Blocks B" means B depends on A.
  WITH RECURSIVE dep(a, b) AS (
    SELECT "Source", "Target" FROM relay_relation WHERE "Type" = 'DependsOn'
    UNION ALL
    SELECT "Target", "Source" FROM relay_relation WHERE "Type" = 'Blocks'
  ),
  walk(start, node, path) AS (
    SELECT a, b, ARRAY[a, b] FROM dep
    UNION ALL
    SELECT w.start, d.b, w.path || d.b FROM walk w JOIN dep d ON d.a = w.node WHERE NOT d.b = ANY(w.path)
  )
  SELECT COUNT(*) INTO n FROM walk w JOIN dep d ON d.a = w.node AND d.b = w.start;
  IF n > 0 THEN RAISE EXCEPTION '04_relay_demo: the DependsOn/Blocks relationships contain a cycle.'; END IF;

  -- One connected network: every item is reachable from the hero.
  WITH RECURSIVE edge(a, b) AS (
    SELECT "Source", "Target" FROM relay_relation
    UNION
    SELECT "Target", "Source" FROM relay_relation
  ),
  reach(k) AS (
    SELECT 'RAG'::text
    UNION
    SELECT e.b FROM reach r JOIN edge e ON e.a = r.k
  )
  SELECT COUNT(*) INTO n FROM reach;
  IF n <> 50 THEN RAISE EXCEPTION '04_relay_demo: only % of 50 items are connected to the hero item.', n; END IF;

  -- The hero's own relationships all start at the hero, so its first graph view reads correctly.
  IF EXISTS (SELECT 1 FROM relay_relation WHERE "Target" = 'RAG') THEN
    RAISE EXCEPTION '04_relay_demo: the hero item has an incoming relationship.';
  END IF;
END $$;

COMMIT;
