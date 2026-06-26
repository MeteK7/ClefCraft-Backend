-- ============================================
-- CALENDAR SEED DATA FOR USER: 72582737-0837-450a-80ff-2e6e02720690
-- Generated for June 2026 (current month view)
-- ============================================

-- ============================================
-- EVENT TYPES (Color-coded categories)
-- ============================================
INSERT INTO "EventTypes" ("Name", "Color", "UserId", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") VALUES
('Work', '#4A90D9', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690'),
('Personal', '#50C878', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690'),
('Health & Fitness', '#FF6B6B', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690'),
('Social', '#FFD93D', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690'),
('Travel', '#9B59B6', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690'),
('Education', '#F39C12', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690'),
('Finance', '#1ABC9C', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690'),
('Family', '#E91E63', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 08:00:00', '72582737-0837-450a-80ff-2e6e02720690');

-- ============================================
-- RECURRING EVENTS (Work week rhythm)
-- ============================================

-- Daily Standup (Mon-Fri)
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Daily Standup',
    'Zoom Meeting Room A',
    '2026-06-01 09:00:00+00',
    '2026-06-01 09:15:00+00',
    false,
    et."Id",
    2,
    'Quick sync with the engineering team',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'a1b2c3d4-1111-4000-8000-000000000001',
    true,
    '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[1,2,3,4,5]}',
    '2026-05-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Weekly Team Meeting (Wed)
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Weekly Team Sync',
    'Conference Room B - 3rd Floor',
    '2026-06-03 14:00:00+00',
    '2026-06-03 15:30:00+00',
    false,
    et."Id",
    3,
    'Sprint review and planning session. Bring your updates!',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'a1b2c3d4-2222-4000-8000-000000000002',
    true,
    '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[3]}',
    '2026-05-20 14:30:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-20 14:30:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Yoga Class (Tue, Thu)
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Morning Yoga',
    'Zen Studio Downtown',
    '2026-06-02 07:00:00+00',
    '2026-06-02 08:00:00+00',
    false,
    et."Id",
    1,
    'Vinyasa flow with Sarah',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'a1b2c3d4-3333-4000-8000-000000000003',
    true,
    '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[2,4]}',
    '2026-05-10 06:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-10 06:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Health & Fitness' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Spanish Lessons (Mon, Wed, Fri)
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Spanish Language Practice',
    'Duolingo Live Session',
    '2026-06-01 18:00:00+00',
    '2026-06-01 18:45:00+00',
    false,
    et."Id",
    1,
    'Intermediate conversation group',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'a1b2c3d4-4444-4000-8000-000000000004',
    true,
    '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[1,3,5]}',
    '2026-04-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-04-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Education' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Bi-weekly 1:1 with Manager
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    '1:1 with David (Manager)',
    'Virtual Coffee Chat',
    '2026-06-01 11:00:00+00',
    '2026-06-01 11:30:00+00',
    false,
    et."Id",
    3,
    'Career growth and project updates',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'a1b2c3d4-5555-4000-8000-000000000005',
    true,
    '{"Frequency":"WEEKLY","Interval":2,"DaysOfWeek":[1]}',
    '2026-05-01 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-01 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- ============================================
-- SINGLE EVENTS - June 2026
-- ============================================

-- Week 1: June 1-7
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Q2 Review Preparation',
    'Home Office',
    '2026-06-01 08:00:00+00',
    '2026-06-01 10:00:00+00',
    false,
    et."Id",
    3,
    'Compile metrics and prepare slides for quarterly review',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'b2c3d4e5-1111-4000-8000-000000000001',
    false,
    NULL,
    '2026-05-28 16:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-28 16:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Dentist Appointment',
    'SmileCare Dental, 123 Main St',
    '2026-06-02 15:00:00+00',
    '2026-06-02 16:00:00+00',
    false,
    et."Id",
    2,
    'Regular checkup and cleaning',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'b2c3d4e5-2222-4000-8000-000000000001',
    false,
    NULL,
    '2026-05-15 09:30:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-15 09:30:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Health & Fitness' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Lunch with Alex',
    'Italian Bistro Downtown',
    '2026-06-03 12:30:00+00',
    '2026-06-03 13:30:00+00',
    false,
    et."Id",
    1,
    'Catching up with old friend',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'b2c3d4e5-3333-4000-8000-000000000001',
    false,
    NULL,
    '2026-05-30 20:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-30 20:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Social' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Product Launch Webinar',
    'Online - StreamYard',
    '2026-06-04 10:00:00+00',
    '2026-06-04 12:00:00+00',
    false,
    et."Id",
    3,
    'Presenting new feature release to stakeholders. Demo and Q&A.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'b2c3d4e5-4444-4000-8000-000000000001',
    false,
    NULL,
    '2026-05-25 11:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-25 11:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Sarah Birthday Party 🎂',
    'Rooftop Bar - The Grand Hotel',
    '2026-06-05 19:00:00+00',
    '2026-06-05 23:00:00+00',
    false,
    et."Id",
    2,
    'Surprise party! Don''t forget to bring the gift',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'b2c3d4e5-5555-4000-8000-000000000001',
    false,
    NULL,
    '2026-05-20 18:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-20 18:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Social' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Weekend Hiking Trip 🏔️',
    'Rocky Mountain Trail',
    '2026-06-06 06:00:00+00',
    '2026-06-07 18:00:00+00',
    true,
    et."Id",
    1,
    'Camping overnight. Pack light, bring water and snacks!',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'b2c3d4e5-6666-4000-8000-000000000001',
    false,
    NULL,
    '2026-05-10 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-10 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Travel' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Week 2: June 8-14
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Client Presentation - Acme Corp',
    'Board Room A',
    '2026-06-08 10:00:00+00',
    '2026-06-08 11:30:00+00',
    false,
    et."Id",
    3,
    'Final proposal presentation. Dress formal, bring handouts.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'c3d4e5f6-1111-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-01 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Code Review Session',
    'Microsoft Teams',
    '2026-06-09 14:00:00+00',
    '2026-06-09 16:00:00+00',
    false,
    et."Id",
    2,
    'Review PRs for the new auth module',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'c3d4e5f6-2222-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-02 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-02 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Gym - Personal Training',
    'FitLife Gym',
    '2026-06-10 17:00:00+00',
    '2026-06-10 18:00:00+00',
    false,
    et."Id",
    1,
    'Session with trainer Mike - focus on upper body',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'c3d4e5f6-3333-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-05 07:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-05 07:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Health & Fitness' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Book Club Meeting 📚',
    'Public Library - Meeting Room 2',
    '2026-06-11 18:30:00+00',
    '2026-06-11 20:30:00+00',
    false,
    et."Id",
    1,
    'Discussing "Atomic Habits" by James Clear',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'c3d4e5f6-4444-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-01 14:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 14:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Social' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Investment Portfolio Review',
    'Home Office',
    '2026-06-12 09:00:00+00',
    '2026-06-12 10:00:00+00',
    false,
    et."Id",
    2,
    'Monthly review with financial advisor (Zoom)',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'c3d4e5f6-5555-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-08 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-08 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Finance' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Mom''s Birthday Dinner',
    'La Maison Restaurant',
    '2026-06-13 19:00:00+00',
    '2026-06-13 21:30:00+00',
    false,
    et."Id",
    3,
    'Reservation for 8 people. Ordered cake - pickup at 5pm.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'c3d4e5f6-6666-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-01 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Family' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Car Service Appointment',
    'AutoCare Service Center',
    '2026-06-14 08:00:00+00',
    '2026-06-14 12:00:00+00',
    false,
    et."Id",
    2,
    'Oil change, tire rotation, brake inspection',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'c3d4e5f6-7777-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-07 15:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-07 15:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Personal' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Week 3: June 15-21
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Deadline: Q2 Report Submission',
    NULL,
    '2026-06-15 00:00:00+00',
    '2026-06-15 23:59:59+00',
    true,
    et."Id",
    3,
    'FINAL DEADLINE. Submit to compliance team by EOD.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'd4e5f6a7-1111-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-10 00:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-10 00:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Tech Conference 2026',
    'Convention Center, Hall D',
    '2026-06-16 08:00:00+00',
    '2026-06-18 18:00:00+00',
    true,
    et."Id",
    3,
    '3-day AI & Cloud Computing Conference. Badge pickup at 7:30am.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'd4e5f6a7-2222-4000-8000-000000000001',
    false,
    NULL,
    '2026-05-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Education' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Networking Mixer',
    'Skyline Lounge',
    '2026-06-17 18:00:00+00',
    '2026-06-17 21:00:00+00',
    false,
    et."Id",
    1,
    'Conference after-party. Great networking opportunity!',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'd4e5f6a7-3333-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-10 14:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-10 14:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Social' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Database Optimization Workshop',
    'Conference Room C',
    '2026-06-19 09:00:00+00',
    '2026-06-19 17:00:00+00',
    false,
    et."Id",
    2,
    'Full-day hands-on workshop. Bring laptop with PostgreSQL installed.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'd4e5f6a7-4444-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-12 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-12 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Education' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Summer Solstice Party ☀️',
    'Beach House - 456 Ocean Drive',
    '2026-06-20 16:00:00+00',
    '2026-06-20 23:59:00+00',
    false,
    et."Id",
    2,
    'BBQ, bonfire, and stargazing! Bring your favorite drink.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'd4e5f6a7-5555-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-01 16:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 16:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Social' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Father''s Day Brunch',
    'Garden Terrace Restaurant',
    '2026-06-21 10:00:00+00',
    '2026-06-21 12:30:00+00',
    false,
    et."Id",
    3,
    'Reservation confirmed. Gift wrapped and ready!',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'd4e5f6a7-6666-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-15 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-15 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Family' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Week 4: June 22-28
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Sprint Planning',
    'War Room - 5th Floor',
    '2026-06-22 10:00:00+00',
    '2026-06-22 12:00:00+00',
    false,
    et."Id",
    3,
    'Plan sprint 27. Review backlog and assign tasks.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'e5f6a7b8-1111-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-18 15:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-18 15:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Dermatologist Appointment',
    'SkinHealth Clinic',
    '2026-06-23 11:00:00+00',
    '2026-06-23 11:45:00+00',
    false,
    et."Id",
    1,
    'Annual skin check',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'e5f6a7b8-2222-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-10 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-10 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Health & Fitness' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Team Building - Escape Room',
    'Escape Quest Downtown',
    '2026-06-24 14:00:00+00',
    '2026-06-24 16:00:00+00',
    false,
    et."Id",
    1,
    'Team outing! "Pirate''s Treasure" room booked.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'e5f6a7b8-3333-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Social' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Therapy Session',
    'Mindful Space Counseling',
    '2026-06-25 16:00:00+00',
    '2026-06-25 16:50:00+00',
    false,
    et."Id",
    2,
    'Regular session with Dr. Williams',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'e5f6a7b8-4444-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-20 16:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-20 16:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Health & Fitness' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Flight to NYC ✈️',
    'International Airport - Terminal 3',
    '2026-06-26 07:00:00+00',
    '2026-06-26 10:30:00+00',
    false,
    et."Id",
    3,
    'UA 247. Check-in online 24hr before. Seat 14A.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'e5f6a7b8-5555-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Travel' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Broadway Show 🎭',
    'Majestic Theatre, NYC',
    '2026-06-26 20:00:00+00',
    '2026-06-26 22:30:00+00',
    false,
    et."Id",
    2,
    'Phantom of the Opera - Orchestra seats',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'e5f6a7b8-6666-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-05 20:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-05 20:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Travel' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'NYC Exploration Day 🗽',
    'Manhattan',
    '2026-06-27 00:00:00+00',
    '2026-06-27 23:59:59+00',
    true,
    et."Id",
    1,
    'Visit MoMA, Central Park, and Brooklyn Bridge',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'e5f6a7b8-7777-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-10 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-10 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Travel' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Return Flight ✈️',
    'JFK Airport',
    '2026-06-28 18:00:00+00',
    '2026-06-28 21:30:00+00',
    false,
    et."Id",
    3,
    'UA 248. Don''t forget to grab NYC bagels!',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'e5f6a7b8-8888-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-15 18:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-15 18:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Travel' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Week 5: June 29-30
INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Monthly Budget Review',
    'Home Office',
    '2026-06-29 09:00:00+00',
    '2026-06-29 10:30:00+00',
    false,
    et."Id",
    2,
    'Review expenses, update spreadsheets, check savings goals',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'f6a7b8c9-1111-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-25 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-25 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Finance' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

INSERT INTO "CalendarEvents" ("Subject", "Location", "StartDate", "EndDate", "AllDayEvent", "EventTypeId", "Importance", "Comment", "UserId", "LinkedBoardItemId", "SeriesUid", "IsRecurring", "RecurrenceRuleJson", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    'Garden Party 🌸',
    'Emily''s Garden',
    '2026-06-30 15:00:00+00',
    '2026-06-30 19:00:00+00',
    false,
    et."Id",
    1,
    'Potluck style! Bringing homemade lemonade.',
    '72582737-0837-450a-80ff-2e6e02720690',
    NULL,
    'f6a7b8c9-2222-4000-8000-000000000001',
    false,
    NULL,
    '2026-06-20 11:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-20 11:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "EventTypes" et
WHERE et."Name" = 'Social' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- ============================================
-- RECURRENCE SERIES & SEGMENTS
-- ============================================

-- Daily Standup Series
INSERT INTO "RecurrenceSeries" ("UserId", "CreatedAt", "SeriesUid") VALUES
('72582737-0837-450a-80ff-2e6e02720690', '2026-05-15 10:00:00', 'a1b2c3d4-1111-4000-8000-000000000001');

INSERT INTO "CalendarEventSegments" ("RecurrenceSeriesId", "EffectiveFrom", "EffectiveTo", "Subject", "Location", "Comment", "IsRecurring", "RecurrenceRuleJson", "StartDate", "EndDate", "Importance", "EventTypeId", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    rs."Id",
    '2026-06-01 00:00:00+00',
    NULL,
    'Daily Standup',
    'Zoom Meeting Room A',
    'Quick sync with the engineering team',
    true,
    '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[1,2,3,4,5]}',
    '2026-06-01 09:00:00+00',
    '2026-06-01 09:15:00+00',
    2,
    et."Id",
    '2026-05-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "RecurrenceSeries" rs
CROSS JOIN "EventTypes" et
WHERE rs."SeriesUid" = 'a1b2c3d4-1111-4000-8000-000000000001'
  AND et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Weekly Team Sync Series
INSERT INTO "RecurrenceSeries" ("UserId", "CreatedAt", "SeriesUid") VALUES
('72582737-0837-450a-80ff-2e6e02720690', '2026-05-20 14:30:00', 'a1b2c3d4-2222-4000-8000-000000000002');

INSERT INTO "CalendarEventSegments" ("RecurrenceSeriesId", "EffectiveFrom", "EffectiveTo", "Subject", "Location", "Comment", "IsRecurring", "RecurrenceRuleJson", "StartDate", "EndDate", "Importance", "EventTypeId", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    rs."Id",
    '2026-06-01 00:00:00+00',
    NULL,
    'Weekly Team Sync',
    'Conference Room B - 3rd Floor',
    'Sprint review and planning session. Bring your updates!',
    true,
    '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[3]}',
    '2026-06-03 14:00:00+00',
    '2026-06-03 15:30:00+00',
    3,
    et."Id",
    '2026-05-20 14:30:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-20 14:30:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "RecurrenceSeries" rs
CROSS JOIN "EventTypes" et
WHERE rs."SeriesUid" = 'a1b2c3d4-2222-4000-8000-000000000002'
  AND et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Morning Yoga Series
INSERT INTO "RecurrenceSeries" ("UserId", "CreatedAt", "SeriesUid") VALUES
('72582737-0837-450a-80ff-2e6e02720690', '2026-05-10 06:00:00', 'a1b2c3d4-3333-4000-8000-000000000003');

INSERT INTO "CalendarEventSegments" ("RecurrenceSeriesId", "EffectiveFrom", "EffectiveTo", "Subject", "Location", "Comment", "IsRecurring", "RecurrenceRuleJson", "StartDate", "EndDate", "Importance", "EventTypeId", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    rs."Id",
    '2026-06-01 00:00:00+00',
    NULL,
    'Morning Yoga',
    'Zen Studio Downtown',
    'Vinyasa flow with Sarah',
    true,
    '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[2,4]}',
    '2026-06-02 07:00:00+00',
    '2026-06-02 08:00:00+00',
    1,
    et."Id",
    '2026-05-10 06:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-10 06:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "RecurrenceSeries" rs
CROSS JOIN "EventTypes" et
WHERE rs."SeriesUid" = 'a1b2c3d4-3333-4000-8000-000000000003'
  AND et."Name" = 'Health & Fitness' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- Spanish Lessons Series
INSERT INTO "RecurrenceSeries" ("UserId", "CreatedAt", "SeriesUid") VALUES
('72582737-0837-450a-80ff-2e6e02720690', '2026-04-01 12:00:00', 'a1b2c3d4-4444-4000-8000-000000000004');

INSERT INTO "CalendarEventSegments" ("RecurrenceSeriesId", "EffectiveFrom", "EffectiveTo", "Subject", "Location", "Comment", "IsRecurring", "RecurrenceRuleJson", "StartDate", "EndDate", "Importance", "EventTypeId", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    rs."Id",
    '2026-06-01 00:00:00+00',
    NULL,
    'Spanish Language Practice',
    'Duolingo Live Session',
    'Intermediate conversation group',
    true,
    '{"Frequency":"WEEKLY","Interval":1,"DaysOfWeek":[1,3,5]}',
    '2026-06-01 18:00:00+00',
    '2026-06-01 18:45:00+00',
    1,
    et."Id",
    '2026-04-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-04-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "RecurrenceSeries" rs
CROSS JOIN "EventTypes" et
WHERE rs."SeriesUid" = 'a1b2c3d4-4444-4000-8000-000000000004'
  AND et."Name" = 'Education' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- 1:1 with Manager Series
INSERT INTO "RecurrenceSeries" ("UserId", "CreatedAt", "SeriesUid") VALUES
('72582737-0837-450a-80ff-2e6e02720690', '2026-05-01 09:00:00', 'a1b2c3d4-5555-4000-8000-000000000005');

INSERT INTO "CalendarEventSegments" ("RecurrenceSeriesId", "EffectiveFrom", "EffectiveTo", "Subject", "Location", "Comment", "IsRecurring", "RecurrenceRuleJson", "StartDate", "EndDate", "Importance", "EventTypeId", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    rs."Id",
    '2026-06-01 00:00:00+00',
    NULL,
    '1:1 with David (Manager)',
    'Virtual Coffee Chat',
    'Career growth and project updates',
    true,
    '{"Frequency":"WEEKLY","Interval":2,"DaysOfWeek":[1]}',
    '2026-06-01 11:00:00+00',
    '2026-06-01 11:30:00+00',
    3,
    et."Id",
    '2026-05-01 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-01 09:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "RecurrenceSeries" rs
CROSS JOIN "EventTypes" et
WHERE rs."SeriesUid" = 'a1b2c3d4-5555-4000-8000-000000000005'
  AND et."Name" = 'Work' AND et."UserId" = '72582737-0837-450a-80ff-2e6e02720690';

-- ============================================
-- REMINDERS
-- ============================================

-- Reminders for important events
INSERT INTO "CalendarReminders" ("CalendarEventId", "MinutesBeforeStart", "IsEnabled", "IsSent", "SentAt", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    ce."Id",
    minutes,
    true,
    false,
    NULL,
    '2026-06-01 00:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 00:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
CROSS JOIN (VALUES (15), (30), (60)) AS r(minutes)
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Importance" >= 2
  AND ce."AllDayEvent" = false
  AND ce."EventTypeId" IN (SELECT "Id" FROM "EventTypes" WHERE "Name" IN ('Work', 'Finance', 'Family') AND "UserId" = '72582737-0837-450a-80ff-2e6e02720690');

-- Special reminders for flights
INSERT INTO "CalendarReminders" ("CalendarEventId", "MinutesBeforeStart", "IsEnabled", "IsSent", "SentAt", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    ce."Id",
    minutes,
    true,
    false,
    NULL,
    '2026-06-01 00:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 00:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
CROSS JOIN (VALUES (120), (180)) AS r(minutes)
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" LIKE '%Flight%';

-- ============================================
-- EXCEPTION HISTORY (for the Daily Standup)
-- ============================================

INSERT INTO "CalendarEventExceptionHistories" ("CalendarEventId", "ChangeDate", "ChangeDescription", "ChangedBy", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    ce."Id",
    '2026-06-10 08:00:00',
    'Moved standup to Conference Room A for demo day',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-10 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-10 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Daily Standup'
  AND ce."IsRecurring" = true
LIMIT 1;

-- ============================================
-- ACTIVITY LOGS
-- ============================================

INSERT INTO "ActivityLogs" ("UserId", "EntityType", "EntityId", "ActionType", "MetadataJson", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'CalendarEvent',
    ce."Id",
    'EVENT_CREATED',
    '{"source":"web","importance":"high"}',
    '2026-05-15 10:00:00',
    '2026-05-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Daily Standup'
  AND ce."IsRecurring" = true
LIMIT 1;

INSERT INTO "ActivityLogs" ("UserId", "EntityType", "EntityId", "ActionType", "MetadataJson", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'CalendarEvent',
    ce."Id",
    'EVENT_CREATED',
    '{"source":"mobile","allDay":true}',
    '2026-05-10 08:00:00',
    '2026-05-10 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-10 08:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Weekend Hiking Trip 🏔️'
LIMIT 1;

INSERT INTO "ActivityLogs" ("UserId", "EntityType", "EntityId", "ActionType", "MetadataJson", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'CalendarEvent',
    ce."Id",
    'EVENT_RESCHEDULED',
    '{"PreviousStart":"2026-06-08T09:00:00+00","PreviousEnd":"2026-06-08T10:30:00+00","NewStart":"2026-06-08T10:00:00+00","NewEnd":"2026-06-08T11:30:00+00","DaysShifted":0.04}',
    '2026-06-01 08:30:00',
    '2026-06-01 08:30:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 08:30:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Client Presentation - Acme Corp'
LIMIT 1;

INSERT INTO "ActivityLogs" ("UserId", "EntityType", "EntityId", "ActionType", "MetadataJson", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'CalendarEvent',
    ce."Id",
    'EVENT_CREATED',
    '{"source":"web","allDay":true}',
    '2026-05-01 12:00:00',
    '2026-05-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-01 12:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Tech Conference 2026'
LIMIT 1;

INSERT INTO "ActivityLogs" ("UserId", "EntityType", "EntityId", "ActionType", "MetadataJson", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'CalendarEvent',
    ce."Id",
    'IMPORTANCE_CHANGED',
    '{"Previous":"1","New":"3"}',
    '2026-06-15 10:00:00',
    '2026-06-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Flight to NYC ✈️'
LIMIT 1;

-- ============================================
-- USER INTERACTION SIGNALS
-- ============================================

INSERT INTO "UserInteractionSignals" ("UserId", "SignalType", "EntityType", "EntityId", "Value", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'VIEW',
    'CalendarEvent',
    ce."Id",
    12.5,
    '2026-06-01 09:05:00',
    '2026-06-01 09:05:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 09:05:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Daily Standup'
  AND ce."IsRecurring" = true
LIMIT 1;

INSERT INTO "UserInteractionSignals" ("UserId", "SignalType", "EntityType", "EntityId", "Value", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'EDIT',
    'CalendarEvent',
    ce."Id",
    2.0,
    '2026-06-01 08:30:00',
    '2026-06-01 08:30:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 08:30:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Client Presentation - Acme Corp'
LIMIT 1;

INSERT INTO "UserInteractionSignals" ("UserId", "SignalType", "EntityType", "EntityId", "Value", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'SNOOZE',
    'CalendarEvent',
    ce."Id",
    1.0,
    '2026-06-01 07:05:00',
    '2026-06-01 07:05:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 07:05:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Morning Yoga'
  AND ce."IsRecurring" = true
LIMIT 1;

INSERT INTO "UserInteractionSignals" ("UserId", "SignalType", "EntityType", "EntityId", "Value", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'DRAG_DROP',
    'CalendarEvent',
    ce."Id",
    3.0,
    '2026-05-30 20:15:00',
    '2026-05-30 20:15:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-30 20:15:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Sarah Birthday Party 🎂'
LIMIT 1;

INSERT INTO "UserInteractionSignals" ("UserId", "SignalType", "EntityType", "EntityId", "Value", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'COMMENT',
    'CalendarEvent',
    ce."Id",
    1.0,
    '2026-05-01 13:00:00',
    '2026-05-01 13:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-01 13:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Tech Conference 2026'
LIMIT 1;

INSERT INTO "UserInteractionSignals" ("UserId", "SignalType", "EntityType", "EntityId", "Value", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'VIEW',
    'CalendarEvent',
    ce."Id",
    8.0,
    '2026-05-15 10:00:00',
    '2026-05-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-05-15 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Weekend Hiking Trip 🏔️'
LIMIT 1;

INSERT INTO "UserInteractionSignals" ("UserId", "SignalType", "EntityType", "EntityId", "Value", "Timestamp", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy") 
SELECT 
    '72582737-0837-450a-80ff-2e6e02720690',
    'VIEW',
    'CalendarEvent',
    ce."Id",
    5.0,
    '2026-06-01 10:00:00',
    '2026-06-01 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 10:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."Subject" = 'Father''s Day Brunch'
LIMIT 1;

-- ============================================
-- NOTIFICATION QUEUE (Upcoming reminders)
-- ============================================

INSERT INTO "NotificationQueues" ("UserId", "CalendarEventId", "ScheduledFor", "IsProcessed", "ProcessedAt", "Message", "DateCreated", "CreatedBy", "DateModified", "ModifiedBy")
SELECT 
    ce."UserId",
    ce."Id",
    ce."StartDate" - (cr."MinutesBeforeStart" * INTERVAL '1 minute'),
    false,
    NULL,
    'Reminder: ' || ce."Subject" || ' in ' || cr."MinutesBeforeStart" || ' minutes',
    '2026-06-01 00:00:00',
    '72582737-0837-450a-80ff-2e6e02720690',
    '2026-06-01 00:00:00',
    '72582737-0837-450a-80ff-2e6e02720690'
FROM "CalendarEvents" ce
INNER JOIN "CalendarReminders" cr ON ce."Id" = cr."CalendarEventId"
WHERE ce."UserId" = '72582737-0837-450a-80ff-2e6e02720690'
  AND ce."StartDate" > '2026-06-01 00:00:00+00'
  AND cr."IsEnabled" = true
LIMIT 50;