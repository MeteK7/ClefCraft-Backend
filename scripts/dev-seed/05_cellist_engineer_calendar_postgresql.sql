-- ============================================================
-- ClefCraft demo data: a software engineer who plays the cello
--
-- Owner: 944d0156-cb3d-466f-a1ea-5f53e3a10f8e (admin@localhost.com, created by DevelopmentUserSeeder)
-- Four weeks of the development admin's calendar, Monday 5 October to Sunday 1 November 2026,
-- kept light enough that the month view stays easy to read: a backend engineer on two-week
-- sprints (sprint meetings, a 1:1, a design review, deadlines, a release, an on-call week, a tech
-- talk) who plays in an amateur orchestra (practice, a weekly lesson, rehearsals, a sectional and
-- the autumn concert on 31 October), with a little life in between. No day has more than three
-- events, the number the month view shows before "+N more".
--
-- Times are the persona's wall-clock times in Europe/Istanbul (UTC+3, no daylight saving), stored
-- the way the app stores them: timed events as instants with TimeZoneId 'Europe/Istanbul';
-- all-day events from local midnight to the next local midnight with TimeZoneId 'UTC'; recurring
-- events as a root event plus a RecurrenceSeries with one open-ended segment. The recurring
-- events start in the week of 5 October and continue after 1 November.
--
-- Uses 10 event types: the admin's existing ones where the name matches (02_event_types has
-- "Meeting" and "Deadline"), new ones for the rest. Creates 7 recurring series and 17 one-off
-- events. No explicit ids, so no sequence reset is needed. Colleagues, teachers and venues are
-- fictional.
--
-- Re-runnable: it first removes what an earlier run of this script loaded (its events with their
-- series, reminders, exceptions, notifications and activity, and its event types that are no
-- longer used), then loads the current data. Events you created yourself with one of the
-- script's own event types (Focus Time, On-call, Cello Practice, ...) are removed with it.
--
-- Prerequisite: a migrated database where the development admin exists (start the API once in
-- Development). Loads after 02_event_types or on its own, but 02 refuses to run once the admin
-- has event types, so load 02 first if you want both. Rolls back if any of the consistency checks
-- at the end fail.
--
--   psql -v ON_ERROR_STOP=1 -h <host> -U <user> -d <database> -f scripts/dev-seed/05_cellist_engineer_calendar_postgresql.sql
-- ============================================================

BEGIN;

SET search_path TO public;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public."AspNetUsers" WHERE "Id" = '944d0156-cb3d-466f-a1ea-5f53e3a10f8e') THEN
    RAISE EXCEPTION '05_cellist_engineer: the development admin does not exist. Start the API once in Development first.';
  END IF;
END $$;

-- ────────────────────────────────────────────────────────────
-- 0. REMOVE AN EARLIER RUN
--    Its events are the admin's events with one of this script's own event types, plus its
--    "Meeting" and "Deadline" events (types shared with 02_event_types), found by subject.
-- ────────────────────────────────────────────────────────────
CREATE TEMP TABLE cello_own_type ("Name" text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO cello_own_type VALUES
  ('Focus Time'), ('Code Review'), ('On-call'), ('Cello Practice'), ('Music Lesson'),
  ('Ensemble'), ('Leisure'), ('Exercise'), ('Errand');

CREATE TEMP TABLE cello_shared_subject ("Subject" text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO cello_shared_subject VALUES
  ('Daily standup'), ('Sprint planning'), ('Sprint review & retro'), ('1:1 with Selin'),
  ('Design review: moving order events to a transactional outbox'),
  ('Deadline: feedback on the API rate-limit RFC'),
  ('Incident review: checkout timeouts on 9 October'),
  ('Release 3.8 code freeze'), ('Deadline: Q4 OKR draft to Selin'), ('Release 3.8 go/no-go'),
  ('Tech talk: the outbox pattern in practice (presenting)'),
  ('Deadline: dead-letter dashboard ready for the sprint review demo');

CREATE TEMP TABLE cello_old_event ON COMMIT DROP AS
SELECT e."Id", e."SeriesUid"
FROM "CalendarEvents" e
JOIN "EventTypes" t ON t."Id" = e."EventTypeId"
WHERE e."UserId" = '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
  AND (t."Name" IN (SELECT "Name" FROM cello_own_type)
       OR (t."Name" IN ('Meeting', 'Deadline') AND e."Subject" IN (SELECT "Subject" FROM cello_shared_subject)));

-- References without a foreign key first; reminders, attachments and exception history cascade
-- with the events, segments with their series.
DELETE FROM "CalendarEventExceptions" WHERE "SeriesUid" IN (SELECT "SeriesUid" FROM cello_old_event);
DELETE FROM "RecurrenceSeries"
WHERE "UserId" = '944d0156-cb3d-466f-a1ea-5f53e3a10f8e' AND "SeriesUid" IN (SELECT "SeriesUid" FROM cello_old_event);
DELETE FROM "CalendarEventCollaborators" WHERE "CalendarEventId" IN (SELECT "Id" FROM cello_old_event);
DELETE FROM "NotificationQueues" WHERE "CalendarEventId" IN (SELECT "Id" FROM cello_old_event);
DELETE FROM "UserInteractionSignals" WHERE "EntityType" = 'CalendarEvent' AND "EntityId" IN (SELECT "Id" FROM cello_old_event);
DELETE FROM "ActivityLogs" WHERE "EntityType" = 'CalendarEvent' AND "EntityId" IN (SELECT "Id" FROM cello_old_event);
DELETE FROM "CalendarEvents" WHERE "Id" IN (SELECT "Id" FROM cello_old_event);

-- ────────────────────────────────────────────────────────────
-- 1. EVENT TYPES
-- ────────────────────────────────────────────────────────────
CREATE TEMP TABLE cello_type ("Name" text PRIMARY KEY, "Color" text NOT NULL) ON COMMIT DROP;

INSERT INTO cello_type VALUES
  -- Work
  ('Meeting',        '#3B82F6'),
  ('Focus Time',     '#6366F1'),
  ('Deadline',       '#DC2626'),
  ('On-call',        '#EA580C'),
  -- Music
  ('Cello Practice', '#B45309'),
  ('Music Lesson',   '#D97706'),
  ('Ensemble',       '#7C3AED'),
  -- Life
  ('Leisure',        '#16A34A'),
  ('Exercise',       '#0D9488'),
  ('Errand',         '#64748B');

-- Only the types the admin doesn't have yet: 02_event_types already has "Meeting" and "Deadline",
-- and an earlier run of this script left its own.
INSERT INTO "EventTypes" ("Name", "Color", "UserId", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT t."Name", t."Color", '944d0156-cb3d-466f-a1ea-5f53e3a10f8e',
       '2026-09-27 21:10:00+03', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e',
       '2026-09-27 21:10:00+03', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM cello_type t
WHERE NOT EXISTS (SELECT 1 FROM "EventTypes" et
                  WHERE et."UserId" = '944d0156-cb3d-466f-a1ea-5f53e3a10f8e' AND et."Name" = t."Name");

-- Name -> the admin's event type id, one per name (the oldest, should a name exist twice).
CREATE TEMP TABLE cello_type_id ON COMMIT DROP AS
SELECT DISTINCT ON (et."Name") et."Name", et."Id"
FROM "EventTypes" et
JOIN cello_type t ON t."Name" = et."Name"
WHERE et."UserId" = '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
ORDER BY et."Name", et."Id";

-- ────────────────────────────────────────────────────────────
-- 2. RECURRING EVENTS (the weekly rhythm)
--    "Rule" is the app's RecurrenceRule JSON. DaysOfWeek: 0 = Sunday ... 6 = Saturday.
--    "Start"/"End" are the first occurrence, in Europe/Istanbul wall-clock time.
-- ────────────────────────────────────────────────────────────
CREATE TEMP TABLE cello_series (
  "Uid" text PRIMARY KEY DEFAULT gen_random_uuid()::text,
  "Subject" text NOT NULL, "Location" text, "Comment" text,
  "Type" text NOT NULL REFERENCES cello_type, "Importance" int NOT NULL,
  "Start" timestamp NOT NULL, "End" timestamp NOT NULL, "Rule" text NOT NULL
) ON COMMIT DROP;

INSERT INTO cello_series ("Subject", "Location", "Comment", "Type", "Importance", "Start", "End", "Rule") VALUES
  ('Sprint planning', 'Meeting room Galata + Google Meet',
   'Two-week sprints. Bring estimates for the tickets you refined; capacity is lower in the on-call week.',
   'Meeting', 1, '2026-10-05 10:00', '2026-10-05 11:30',
   '{"Frequency":"WEEKLY","Interval":2,"DaysOfWeek":[1]}'),

  ('Sprint review & retro', 'Meeting room Galata + Google Meet',
   'Demo first, then the retro. Selin runs the retro board.',
   'Meeting', 1, '2026-10-16 14:00', '2026-10-16 15:30',
   '{"Frequency":"WEEKLY","Interval":2,"DaysOfWeek":[5]}'),

  ('1:1 with Selin', 'Small room 2',
   'Engineering manager 1:1. Running notes in our shared doc: growth goals, the tech talk, on-call load.',
   'Meeting', 1, '2026-10-06 14:00', '2026-10-06 14:30',
   '{"Frequency":"WEEKLY","Interval":2,"DaysOfWeek":[2]}'),

  ('Cello practice', 'Home',
   'Scales with a drone, the Popper etude, then the Elgar concerto and the Brahms 2 orchestra part. Practice mute after 21:00.',
   'Cello Practice', 1, '2026-10-05 20:00', '2026-10-05 21:00',
   '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[1,3]}'),

  ('Orchestra rehearsal', 'Moda Amateur Symphony - rehearsal hall, Kadıköy',
   'Autumn programme: Brahms Symphony No. 2 and the Saint-Saëns Cello Concerto No. 1. Bring a pencil and the mute.',
   'Ensemble', 1, '2026-10-08 19:30', '2026-10-08 22:00',
   '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[4]}'),

  ('Cello lesson with Lena', 'Lena Arslan''s studio, Moda',
   'Bring the Elgar and the Popper book. Ask about the thumb-position shift in bar 23 and vibrato in the slow movement.',
   'Music Lesson', 1, '2026-10-10 11:00', '2026-10-10 12:00',
   '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[6]}'),

  ('Sunday run', 'Moda coast to Fenerbahçe and back',
   'Easy 7 km. Stretch the shoulders and forearms afterwards.',
   'Exercise', 0, '2026-10-11 09:00', '2026-10-11 10:00',
   '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[0]}');

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance",
  "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "TimeZoneId",
  "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT s."Subject", s."Location",
       s."Start" AT TIME ZONE 'Europe/Istanbul', s."End" AT TIME ZONE 'Europe/Istanbul',
       false, et."Id", s."Importance", s."Comment", '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NULL,
       s."Uid", true, s."Rule", 'Europe/Istanbul',
       '2026-09-27 21:30:00+03', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e',
       '2026-09-27 21:30:00+03', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM cello_series s
JOIN cello_type_id et ON et."Name" = s."Type";

INSERT INTO "RecurrenceSeries" ("UserId", "CreatedAt", "SeriesUid", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '2026-09-27 21:30:00+03', s."Uid",
       '2026-09-27 21:30:00+03', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e',
       '2026-09-27 21:30:00+03', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM cello_series s;

-- One open-ended segment per series, effective from the first occurrence (as the create endpoint does).
INSERT INTO "CalendarEventSegments" ("RecurrenceSeriesId", "EffectiveFrom", "EffectiveTo", "Subject", "Location", "Comment",
  "IsRecurring", "RecurrenceRuleJson", "TimeZoneId", "StartDate", "EndDate", "Importance", "EventTypeId",
  "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT rs."Id", s."Start" AT TIME ZONE 'Europe/Istanbul', NULL, s."Subject", s."Location", s."Comment",
       true, s."Rule", 'Europe/Istanbul',
       s."Start" AT TIME ZONE 'Europe/Istanbul', s."End" AT TIME ZONE 'Europe/Istanbul', s."Importance", et."Id",
       '2026-09-27 21:30:00+03', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e',
       '2026-09-27 21:30:00+03', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM cello_series s
JOIN "RecurrenceSeries" rs ON rs."SeriesUid" = s."Uid"
JOIN cello_type_id et ON et."Name" = s."Type";

-- ────────────────────────────────────────────────────────────
-- 3. ONE-OFF EVENTS
--    Timed events: "Start"/"End" in Europe/Istanbul wall-clock time.
--    All-day events: "Start" is the first day, "End" the day after the last (end exclusive).
-- ────────────────────────────────────────────────────────────
CREATE TEMP TABLE cello_event (
  "Subject" text NOT NULL, "Location" text, "Comment" text,
  "Type" text NOT NULL REFERENCES cello_type, "Importance" int NOT NULL,
  "Start" timestamp NOT NULL, "End" timestamp NOT NULL, "AllDay" boolean NOT NULL DEFAULT false
) ON COMMIT DROP;

INSERT INTO cello_event ("Subject", "Location", "Comment", "Type", "Importance", "Start", "End", "AllDay") VALUES
  -- ── Week 1: 5-11 October (sprint 21 starts) ──────────────
  ('Design review: moving order events to a transactional outbox', 'Meeting room Galata',
   'Presenting the one-pager. Open questions: publisher polling interval, ordering per aggregate, cleanup job.',
   'Meeting', 2, '2026-10-07 11:00', '2026-10-07 12:00', false),
  ('Deadline: feedback on the API rate-limit RFC', NULL,
   'Comments in the RFC doc. Main point: per-tenant limits, not per-key.',
   'Deadline', 2, '2026-10-09 17:00', '2026-10-09 17:30', false),
  ('Piano trio read-through with Can and Mira', 'Mira''s flat, Cihangir',
   'Haydn "Gypsy" trio and Beethoven op. 1 no. 1. Just for fun - bring the music stand.',
   'Ensemble', 0, '2026-10-10 15:00', '2026-10-10 17:30', false),

  -- ── Week 2: 12-18 October (release 3.8) ──────────────────
  ('Coding: migrate order events to the outbox', 'Desk, 4th floor',
   'Switch OrderPlaced and OrderCancelled to the outbox behind a feature flag. Slack on do-not-disturb.',
   'Focus Time', 1, '2026-10-13 13:30', '2026-10-13 17:00', false),
  ('Luthier: bridge adjustment and a new A string', 'Luthier''s workshop, Galata',
   'The bridge leans toward the fingerboard. Ask about a soundpost check before the concert.',
   'Errand', 1, '2026-10-14 18:30', '2026-10-14 19:00', false),
  ('Release 3.8 code freeze', NULL,
   'Only release-blocker fixes merge after today.',
   'Deadline', 2, '2026-10-15 00:00', '2026-10-16 00:00', true),
  ('Release 3.8 go/no-go', 'Google Meet',
   'Checklist in the release ticket. I sign off on the payments changes.',
   'Meeting', 2, '2026-10-16 10:00', '2026-10-16 10:30', false),
  ('Concert: Dvořák Cello Concerto with the City Philharmonic', 'Concert hall, Harbiye',
   'Tickets are in the email from 28 September, row F. Listen for how the soloist paces the first-movement cadenza.',
   'Leisure', 1, '2026-10-16 20:00', '2026-10-16 22:30', false),

  -- ── Week 3: 19-25 October (on call, sprint 22 starts) ────
  ('On call (primary)', NULL,
   'Primary on-call, Monday 09:00 to Monday 09:00. Laptop and charger everywhere; the cello sectional is on Sunday.',
   'On-call', 2, '2026-10-19 00:00', '2026-10-26 00:00', true),
  ('Tech talk: the outbox pattern in practice (presenting)', 'Town hall room + live stream',
   'Engineering-wide talk: twenty minutes plus questions. Test the clicker and screen sharing at 14:45.',
   'Meeting', 2, '2026-10-23 15:00', '2026-10-23 16:00', false),
  ('Dinner at Elif''s', 'Elif''s place, Üsküdar',
   'Bring dessert. Phone on loud - still on call.',
   'Leisure', 0, '2026-10-24 20:00', '2026-10-24 23:00', false),
  ('Cello sectional: Brahms 2, movements 2 and 4', 'Moda Amateur Symphony - rehearsal hall, Kadıköy',
   'With the principal cellist. Bowings for the second-movement opening, and the fast passage at letter K.',
   'Ensemble', 1, '2026-10-25 14:00', '2026-10-25 16:30', false),

  -- ── Week 4: 26 October - 1 November (concert week) ───────
  ('Coding: dead-letter queue dashboard', 'Desk, 4th floor',
   'List, inspect and replay messages. Replay needs an audit log entry.',
   'Focus Time', 1, '2026-10-26 13:30', '2026-10-26 17:00', false),
  ('Deadline: dead-letter dashboard ready for the sprint review demo', NULL,
   'Deployed to staging, with seeded failed messages to replay in the demo.',
   'Deadline', 2, '2026-10-30 13:00', '2026-10-30 13:30', false),
  ('Dress rehearsal - autumn concert', 'Moda Stage, Kadıköy',
   'Full run in concert order on the real stage. Check the chair height and the endpin stop.',
   'Ensemble', 2, '2026-10-31 14:00', '2026-10-31 17:00', false),
  ('Autumn concert - Moda Amateur Symphony (performing)', 'Moda Stage, Kadıköy',
   'Call time 18:45, concert black. Brahms Symphony No. 2 and the Saint-Saëns concerto. Family has tickets in row C.',
   'Ensemble', 2, '2026-10-31 19:30', '2026-10-31 21:30', false),
  ('Post-concert brunch with the cello section', 'Café by the pier, Moda',
   'Celebrate, and pick the spring programme wish list.',
   'Leisure', 0, '2026-11-01 12:00', '2026-11-01 14:00', false);

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance",
  "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "TimeZoneId",
  "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT e."Subject", e."Location",
       e."Start" AT TIME ZONE 'Europe/Istanbul', e."End" AT TIME ZONE 'Europe/Istanbul',
       e."AllDay", et."Id", e."Importance", e."Comment", '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', NULL,
       gen_random_uuid()::text, false, NULL,
       -- All-day events are anchored to UTC, as the app does.
       CASE WHEN e."AllDay" THEN 'UTC' ELSE 'Europe/Istanbul' END,
       -- Planned a few days ahead.
       (e."Start" - interval '4 days') AT TIME ZONE 'Europe/Istanbul', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e',
       (e."Start" - interval '4 days') AT TIME ZONE 'Europe/Istanbul', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
FROM cello_event e
JOIN cello_type_id et ON et."Name" = e."Type";

-- An earlier run's event types that the current data no longer uses (and nothing else uses either).
DELETE FROM "EventTypes" et
WHERE et."UserId" = '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'
  AND et."Name" IN (SELECT "Name" FROM cello_own_type)
  AND et."Name" NOT IN (SELECT "Name" FROM cello_type)
  AND NOT EXISTS (SELECT 1 FROM "CalendarEvents" e WHERE e."EventTypeId" = et."Id")
  AND NOT EXISTS (SELECT 1 FROM "CalendarEventSegments" s WHERE s."EventTypeId" = et."Id");

-- ────────────────────────────────────────────────────────────
-- 4. CONSISTENCY CHECKS (any failure rolls the whole script back)
-- ────────────────────────────────────────────────────────────
DO $$
DECLARE n int;
BEGIN
  -- Every type the events use resolved to one of the admin's event types (so no event was dropped by a join).
  SELECT COUNT(*) INTO n FROM cello_type_id;
  IF n <> 10 THEN RAISE EXCEPTION '05_cellist_engineer: expected 10 event types, found %.', n; END IF;

  SELECT COUNT(*) INTO n FROM "CalendarEventSegments" seg
  JOIN "RecurrenceSeries" rs ON rs."Id" = seg."RecurrenceSeriesId"
  JOIN cello_series s ON s."Uid" = rs."SeriesUid";
  IF n <> 7 THEN RAISE EXCEPTION '05_cellist_engineer: expected 7 recurring series with a segment, found %.', n; END IF;

  SELECT COUNT(*) INTO n FROM cello_event;
  IF n <> 17 THEN RAISE EXCEPTION '05_cellist_engineer: expected 17 one-off events, found %.', n; END IF;

  -- Every event ends after it starts, and all-day events cover whole local days.
  SELECT COUNT(*) INTO n FROM (
    SELECT "Start", "End", false AS "AllDay" FROM cello_series
    UNION ALL SELECT "Start", "End", "AllDay" FROM cello_event
  ) x
  WHERE x."End" <= x."Start" OR (x."AllDay" AND (x."Start"::time <> '00:00' OR x."End"::time <> '00:00'));
  IF n > 0 THEN RAISE EXCEPTION '05_cellist_engineer: % events have an invalid start/end.', n; END IF;

  -- Every one-off falls in the four weeks, 5 October to 1 November.
  SELECT COUNT(*) INTO n FROM cello_event
  WHERE "Start" < '2026-10-05' OR "End" > '2026-11-02';
  IF n > 0 THEN RAISE EXCEPTION '05_cellist_engineer: % one-off events fall outside 5 Oct - 1 Nov.', n; END IF;

  -- Each day's events in the four weeks: one-offs (an all-day event on every day it covers) and every
  -- occurrence of the recurring events, expanded like RecurrenceHelper.GenerateWeeklyByDayCandidates
  -- does: the listed weekdays, in every "Interval"-th Sunday-to-Saturday week from the first one.
  CREATE TEMP TABLE cello_day_event ON COMMIT DROP AS
    SELECT "Subject", "Start", "End", "AllDay", "Start"::date AS "Day" FROM cello_event WHERE NOT "AllDay"
    UNION ALL
    SELECT e."Subject", e."Start", e."End", true, d::date
    FROM cello_event e
    CROSS JOIN generate_series(e."Start", e."End" - interval '1 day', interval '1 day') d
    WHERE e."AllDay"
    UNION ALL
    SELECT s."Subject", d + s."Start"::time, d + s."End"::time, false, d::date
    FROM cello_series s
    CROSS JOIN generate_series(timestamp '2026-10-05', timestamp '2026-11-01', interval '1 day') d
    WHERE d >= s."Start"::date
      AND (s."Rule"::jsonb -> 'DaysOfWeek') @> to_jsonb(extract(dow FROM d)::int)
      AND (((d::date - extract(dow FROM d)::int) - (s."Start"::date - extract(dow FROM s."Start")::int)) / 7)
          % (s."Rule"::jsonb ->> 'Interval')::int = 0;

  -- No two timed events overlap.
  SELECT COUNT(*) INTO n FROM cello_day_event a JOIN cello_day_event b
    ON NOT a."AllDay" AND NOT b."AllDay"
   AND (a."Start", a."Subject") < (b."Start", b."Subject")
   AND a."Start" < b."End" AND b."Start" < a."End";
  IF n > 0 THEN RAISE EXCEPTION '05_cellist_engineer: % pairs of timed events overlap.', n; END IF;

  -- No day has more than the three events the month view shows before "+N more".
  SELECT COUNT(*) INTO n FROM (
    SELECT "Day" FROM cello_day_event GROUP BY "Day" HAVING COUNT(*) > 3
  ) busy;
  IF n > 0 THEN RAISE EXCEPTION '05_cellist_engineer: % days have more than three events.', n; END IF;
END $$;

COMMIT;
