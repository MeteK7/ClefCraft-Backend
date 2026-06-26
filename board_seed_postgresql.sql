-- ============================================================
-- ClefCraft Seed Data — AI/Software Engineer Workspace
-- User ID: 944d0156-cb3d-466f-a1ea-5f53e3a10f8e
-- Run with: psql -U <user> -d <db> -f clefcraft_seed.sql
-- ============================================================

SET search_path TO public;

-- ────────────────────────────────────────────────────────────
-- 1. STATUSES
-- ────────────────────────────────────────────────────────────
INSERT INTO public."Statuses" ("Id", "Name", "DateCreated", "CreatedBy") VALUES
  (1, 'Backlog',     NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2, 'To Do',       NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3, 'In Progress', NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4, 'In Review',   NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5, 'Done',        NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 2. PRIORITIES
-- ────────────────────────────────────────────────────────────
INSERT INTO public."Priorities" ("Id", "Name", "DateCreated", "CreatedBy") VALUES
  (1, 'Critical', NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2, 'High',     NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3, 'Medium',   NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4, 'Low',      NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 3. TAGS
-- ────────────────────────────────────────────────────────────
INSERT INTO public."Tags" ("Id", "Name", "DateCreated", "CreatedBy") VALUES
  (1,  'AI/ML',         NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  'Backend',       NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  'Frontend',      NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  'DevOps',        NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  'Research',      NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  'API',           NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  'Database',      NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  'Testing',       NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  'Performance',   NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 'Documentation', NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 4. BOARDS
-- ────────────────────────────────────────────────────────────
INSERT INTO public."Boards" ("Id", "Title", "DateCreated", "CreatedBy") VALUES
  (1, 'AI Platform Sprint',           NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2, 'Infrastructure & DevOps',      NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3, 'Research & Experimentation',   NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 5. BOARD STATUSES
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardStatuses" ("Id", "BoardId", "StatusId", "DateCreated", "CreatedBy") VALUES
  (1,  1, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  1, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  1, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  1, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  1, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  2, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  2, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  2, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  2, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 3, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (11, 3, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (12, 3, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (13, 3, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 6. BOARD PRIORITIES
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardPriorities" ("Id", "BoardId", "PriorityId", "DateCreated", "CreatedBy") VALUES
  (1,  1, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  1, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  1, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  1, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  2, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  2, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  2, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  2, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  3, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 3, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (11, 3, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 7. BOARD TAGS
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardTags" ("Id", "BoardId", "TagId", "DateCreated", "CreatedBy") VALUES
  (1,  1, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  1, 2,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  1, 3,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  1, 6,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  1, 8,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  2, 4,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  2, 7,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  2, 9,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  3, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 3, 5,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (11, 3, 10, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 8. BOARD COLUMNS
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardColumns" ("Id", "Title", "DateCreated", "CreatedBy") VALUES
  (1,  'Backlog',     NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  'To Do',       NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  'In Progress', NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  'In Review',   NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  'Done',        NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  'Backlog',     NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  'To Do',       NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  'In Progress', NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  'Done',        NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 'Ideas',       NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (11, 'Running',     NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (12, 'Concluded',   NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 9. BOARD COLUMN MAPPINGS
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardColumnMappings" ("Id", "BoardId", "BoardColumnId", "DateCreated", "CreatedBy") VALUES
  (1,  1, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  1, 2,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  1, 3,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  1, 4,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  1, 5,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  2, 6,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  2, 7,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  2, 8,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  2, 9,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 3, 10, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (11, 3, 11, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (12, 3, 12, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 10. BOARD ITEMS
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardItems" (
  "Id", "BoardId", "BoardColumnId",
  "Title", "Description",
  "AssigneeId", "DueDate", "EstimatedTime", "TimeSpent",
  "DateCreated", "CreatedBy"
) VALUES
-- ── BOARD 1: AI Platform Sprint ──────────────────────────────
(1, 1, 1,
 'Implement RAG pipeline with pgvector',
 'Build retrieval-augmented generation pipeline using pgvector extension. Includes chunking strategy, embedding generation via OpenAI ada-002, and similarity search endpoint.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '14 days', 16.0, 0.0,
 NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(2, 1, 1,
 'Streaming SSE responses for chat completions',
 'Add Server-Sent Events support to the chat endpoint so the frontend can stream LLM tokens as they arrive instead of waiting for the full response.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '21 days', 8.0, 0.0,
 NOW() - INTERVAL '2 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(3, 1, 1,
 'Add model fallback logic (GPT-4o → GPT-4 Turbo)',
 'If primary model returns a rate-limit error, automatically retry with fallback model. Track which model served each request in the audit log.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '30 days', 6.0, 0.0,
 NOW() - INTERVAL '1 day', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(4, 1, 2,
 'JWT refresh token rotation',
 'Implement silent refresh flow with refresh token rotation. Invalidate old tokens after use and store revocation list in Redis.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '5 days', 10.0, 0.0,
 NOW() - INTERVAL '3 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(5, 1, 2,
 'Rate limiting middleware per API key',
 'Sliding-window rate limiter using Redis. Per-key limits configurable via environment. Return Retry-After header on 429.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '4 days', 5.0, 0.0,
 NOW() - INTERVAL '4 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(6, 1, 2,
 'Prompt template versioning system',
 'Store prompt templates in DB with semver. API should accept template ID + version. Admin endpoint to promote a version to production.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '7 days', 12.0, 0.0,
 NOW() - INTERVAL '1 day', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(7, 1, 3,
 'Vector embeddings endpoint (/v1/embed)',
 'REST endpoint that accepts text (or batch of texts), calls embedding model, and returns vectors. Support both sync and async (queue) modes.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '2 days', 8.0, 5.5,
 NOW() - INTERVAL '5 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(8, 1, 3,
 'Conversation history context window management',
 'Trim conversation history to fit within model context limit. Use token counting to decide what to drop. Preserve system prompt and last N turns.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '3 days', 6.0, 3.0,
 NOW() - INTERVAL '2 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(9, 1, 4,
 'OpenAI function calling integration',
 'Map domain actions (DB queries, API calls) to OpenAI function schemas. Parse function_call responses and execute the matching handler.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '1 day', 14.0, 13.5,
 NOW() - INTERVAL '10 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(10, 1, 5,
 'Project scaffolding: .NET 8 + EF Core + PostgreSQL',
 'Set up solution structure, DbContext, migrations baseline, and environment-based configuration. Health check endpoint.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() - INTERVAL '5 days', 4.0, 4.0,
 NOW() - INTERVAL '14 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(11, 1, 5,
 'Global error handling middleware',
 'ProblemDetails-compliant error responses. Map domain exceptions to HTTP status codes. Structured logging with correlation IDs.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() - INTERVAL '3 days', 3.0, 3.0,
 NOW() - INTERVAL '12 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
-- ── BOARD 2: Infrastructure & DevOps ─────────────────────────
(12, 2, 6,
 'Kubernetes HPA for inference pods',
 'Configure Horizontal Pod Autoscaler based on GPU memory utilisation metric. Min 2, max 10 replicas. Custom metrics via Prometheus adapter.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '20 days', 12.0, 0.0,
 NOW() - INTERVAL '1 day', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(13, 2, 6,
 'Disaster recovery runbook',
 'Document RTO/RPO targets, backup restoration steps, and cross-region failover procedure for the Postgres primary.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '25 days', 6.0, 0.0,
 NOW() - INTERVAL '2 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(14, 2, 7,
 'GitHub Actions: multi-stage Docker build + push',
 'Build .NET API image in CI. Tag with commit SHA and semver. Push to GHCR on merge to main. Cache layers between runs.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '6 days', 5.0, 0.0,
 NOW() - INTERVAL '3 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(15, 2, 7,
 'Postgres WAL-G backup to S3 (daily + hourly)',
 'Configure WAL-G with S3 backend. Daily base backups, continuous WAL archiving. Retention: 30 days. Alert if backup is older than 25h.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '8 days', 8.0, 0.0,
 NOW() - INTERVAL '2 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(16, 2, 8,
 'Prometheus + Grafana observability stack',
 'Deploy kube-prometheus-stack. Create dashboards for API latency (p50/p95/p99), error rate, token throughput, and GPU utilisation.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '3 days', 10.0, 7.0,
 NOW() - INTERVAL '6 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(17, 2, 9,
 'Terraform: VPC, subnets, security groups',
 'IaC for networking layer. Public/private subnet split. Bastion host in public subnet. All infra in ap-southeast-1.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() - INTERVAL '7 days', 8.0, 9.0,
 NOW() - INTERVAL '20 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(18, 2, 9,
 'Secrets management via HashiCorp Vault',
 'App reads API keys and DB credentials from Vault at startup via the AppRole auth method. Rotate secrets without restart using SIGHUP.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() - INTERVAL '4 days', 6.0, 6.5,
 NOW() - INTERVAL '15 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
-- ── BOARD 3: Research & Experimentation ───────────────────────
(19, 3, 10,
 'Fine-tuning GPT-3.5 on domain Q&A pairs',
 'Collect 500+ curated Q&A pairs from internal docs. Fine-tune via OpenAI API. Evaluate with ROUGE and human eval. Compare cost vs GPT-4 prompting.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '45 days', 24.0, 0.0,
 NOW() - INTERVAL '3 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(20, 3, 10,
 'Evaluate Mistral 7B vs LLaMA 3 8B for local inference',
 'Benchmark on latency, memory footprint, and accuracy on our eval set. Run on a single A10G. Decision: which to deploy for offline mode.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '30 days', 16.0, 0.0,
 NOW() - INTERVAL '1 day', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(21, 3, 11,
 'RLHF data collection pipeline',
 'Build annotation UI where domain experts rank model outputs. Store preference pairs in DB. Script to export in Anthropic HH format.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '10 days', 20.0, 8.0,
 NOW() - INTERVAL '8 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(22, 3, 11,
 'Chunking strategy A/B test (fixed vs semantic)',
 'Compare fixed-size 512-token chunks vs semantic paragraph chunking for RAG retrieval accuracy. Measure MRR@5 on 100-query eval set.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() + INTERVAL '5 days', 8.0, 4.0,
 NOW() - INTERVAL '4 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(23, 3, 12,
 'Baseline eval: GPT-4o on internal benchmark',
 'Ran 200 test cases from our QA benchmark against GPT-4o (2024-05-13). Accuracy 87.3%, avg latency 1.4s. Stored results as baseline JSON.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() - INTERVAL '5 days', 4.0, 4.0,
 NOW() - INTERVAL '12 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
(24, 3, 12,
 'Token cost analysis across model providers',
 'Compared OpenAI, Anthropic, Cohere, and Mistral pricing for our usage patterns. Recommended hybrid: Claude Haiku for classification, GPT-4o for generation.',
 '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NOW() - INTERVAL '2 days', 3.0, 3.5,
 NOW() - INTERVAL '10 days', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 11. BOARD ITEM STATUSES
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardItemStatuses" ("Id", "BoardItemId", "StatusId", "DateCreated", "CreatedBy") VALUES
  (1,  1,  1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  2,  1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  3,  1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  4,  2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  5,  2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  6,  2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  7,  3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  8,  3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  9,  4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 10, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (11, 11, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (12, 12, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (13, 13, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (14, 14, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (15, 15, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (16, 16, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (17, 17, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (18, 18, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (19, 19, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (20, 20, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (21, 21, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (22, 22, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (23, 23, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (24, 24, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 12. BOARD ITEM PRIORITIES
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardItemPriorities" ("Id", "BoardItemId", "PriorityId", "DateCreated", "CreatedBy") VALUES
  (1,  1,  2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  2,  2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  3,  3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  4,  1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  5,  2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  6,  3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  7,  1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  8,  2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  9,  2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 10, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (11, 11, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (12, 12, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (13, 13, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (14, 14, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (15, 15, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (16, 16, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (17, 17, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (18, 18, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (19, 19, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (20, 20, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (21, 21, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (22, 22, 3, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (23, 23, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (24, 24, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- 13. BOARD ITEM TAGS
-- ────────────────────────────────────────────────────────────
INSERT INTO public."BoardItemTags" ("Id", "BoardItemId", "TagId", "DateCreated", "CreatedBy") VALUES
  (1,  1, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (2,  1, 2,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (3,  1, 7,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (4,  2, 6,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (5,  2, 2,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (6,  3, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (7,  3, 6,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (8,  4, 2,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (9,  4, 8,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (10, 5, 6,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (11, 5, 9,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (12, 6, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (13, 6, 2,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (14, 7, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (15, 7, 6,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (16, 8, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (17, 8, 9,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (18, 9, 1,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (19, 9, 6,  NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (20, 10, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (21, 10, 7, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (22, 11, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (23, 11, 10,NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (24, 12, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (25, 12, 9, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (26, 13, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (27, 13, 10,NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (28, 14, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (29, 14, 8, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (30, 15, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (31, 15, 7, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (32, 16, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (33, 16, 9, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (34, 16, 10,NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (35, 17, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (36, 18, 4, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (37, 18, 2, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (38, 19, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (39, 19, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (40, 20, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (41, 20, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (42, 20, 9, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (43, 21, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (44, 21, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (45, 22, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (46, 22, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (47, 22, 7, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (48, 23, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (49, 23, 5, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (50, 23, 10,NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (51, 24, 1, NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
  (52, 24, 10,NOW(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- Sequence resets
-- ────────────────────────────────────────────────────────────
SELECT setval(pg_get_serial_sequence('public."Statuses"',            'Id'), 5);
SELECT setval(pg_get_serial_sequence('public."Priorities"',          'Id'), 4);
SELECT setval(pg_get_serial_sequence('public."Tags"',                'Id'), 10);
SELECT setval(pg_get_serial_sequence('public."Boards"',              'Id'), 3);
SELECT setval(pg_get_serial_sequence('public."BoardStatuses"',       'Id'), 13);
SELECT setval(pg_get_serial_sequence('public."BoardPriorities"',     'Id'), 11);
SELECT setval(pg_get_serial_sequence('public."BoardTags"',           'Id'), 11);
SELECT setval(pg_get_serial_sequence('public."BoardColumns"',        'Id'), 12);
SELECT setval(pg_get_serial_sequence('public."BoardColumnMappings"', 'Id'), 12);
SELECT setval(pg_get_serial_sequence('public."BoardItems"',          'Id'), 24);
SELECT setval(pg_get_serial_sequence('public."BoardItemStatuses"',   'Id'), 24);
SELECT setval(pg_get_serial_sequence('public."BoardItemPriorities"', 'Id'), 24);
SELECT setval(pg_get_serial_sequence('public."BoardItemTags"',       'Id'), 52);