# Reminder System Evaluation and Decision Record

This is an evaluation and decision record. It contains no code changes, no implementation plan, no estimates, and no prescribed mechanisms. It records current behavior, confirmed defects, the product decisions made (D1–D10), the questions those decisions raise, architectural concerns, and future or theoretical concerns, each kept separate.

**Confidence levels:**
- **High:** verified by reading the code path end to end, or the library source in `node_modules`.
- **Medium:** strongly implied by the code but not exercised at runtime.
- **Low:** plausible, not verified.

**Issue types:** correctness bug · reliability bug · UX/UI defect · UX improvement · accessibility defect · architectural concern · product decision · future scalability · theoretical.

**Status:**
- Decisions D1–D10 are recorded in section 12.
- All clarifications in section 12a (**F1–F7**) are decided. F6 and F7 were approved as proposed.
- Section 12b lists what deliberately remains open: **D10**, plus the idle-timeout consequence noted under F1.

**Note on the current branch.** The hover pause described in section 2 (and the toast's own 12 s timer) is commit `82077ec`. It exists only on the `chore/foundation-cleanup` branch. On `test-n552vx`, the toast still uses `MatSnackBarConfig.duration: 12_000` (`app.component.ts:55`) and doesn't pause on hover.

---

## 1. Current reminder architecture

| Step | Code | Current behavior |
|---|---|---|
| Settings | `calendar-dialog.component.ts:97`, template lines 148–155 | A multi-select "Reminders" field with offsets of 0, 5, 15, 30, 60, 120 and 1440 minutes. It's always shown and always editable, for every event type and every edit scope. |
| Storage | `CreateCalendarEventCommandHandler.cs:116`, `UpdateCalendarEventCommandHandler.cs:186–207` | One `CalendarReminder` row per offset, keyed by `CalendarEventId`. A plain update deletes all rows and recreates them. |
| Scheduling | `Infrastructure/Services/Calendar/ReminderSchedulerService.cs` | For each enabled reminder it creates one `NotificationQueue` row with `ScheduledFor = event.StartDate − offset`. Past times are skipped. The text is fixed at scheduling time: `"{Subject} starts in {N} minutes"`. `RescheduleAsync` deletes the event's unprocessed rows and schedules again. It's called only from the create handler and the plain-update handler. |
| Sending | `NotificationBackgroundService.cs` | A hosted service. About once a minute it loads rows with `!IsProcessed && ScheduledFor <= now`, sends each one, then marks it `IsProcessed`/`ProcessedAt` and saves. |
| Transport | `Api/Services/NotificationHubService.cs`, `NotificationHub.cs`, `CustomUserIdProvider.cs` | `Clients.User(uid).SendAsync("ReceiveReminder", {eventId, message})` on an `[Authorize]` hub. The SignalR user is identified by the `uid` claim. |
| Client connection | `_services/notification-realtime.service.ts` | A root singleton, injected only by `AppComponent`. It builds one connection with `withAutomaticReconnect()` (default policy) and registers both handlers once, in its constructor. It starts or stops the connection by following `isAuthenticated$` (with `distinctUntilChanged`), and serializes start/stop calls. |
| Presentation | `app.component.ts` | The app shell subscribes to `reminders$` and opens `LiveReminderToastComponent` through the global `MatSnackBar`, top-right. "View event" goes to `/calendar?eventId=…`. |
| Toast | `pages/live-reminder-toast/*` | Times its own dismissal: 12 s, paused on mouse hover, resumed with the remaining time. Has a progress bar, the buttons View event / Dismiss / ✕, and `role="alertdialog"`. |
| Deep link | `calendar.component.ts:265, 477–498` | Reloads the visible range, then opens the first loaded event whose `id` matches. Otherwise it shows "Could not find that event on the calendar." |
| Session limits | `JwtSettings.cs:17,20`, `idle-session.service.ts:20` | Server: a refresh token dies after **15 idle minutes**, and every session ends after **8 hours**. Client: automatic logout after **15 minutes without user input**. |

Mentions share the hub and the toast host, but they're sent directly when a comment is saved, not through the queue.

## 2. What is working correctly
All of these are verified (high confidence):
- **App-level ownership.** The connection, the subscription and the toast host live at application level. Changing pages can't drop or duplicate a reminder, and no specific page needs to be open.
- **Auth lifecycle.** The client connects only while authenticated: after login, and at startup when a stored session exists. It stops on logout, including the idle-timeout logout. A quick logout→login can't race. The hub is `[Authorize]`, so no anonymous connections are made.
- **Token refresh.** `accessTokenFactory` gets a valid access token (refreshing it if needed) on every connect and reconnect.
- **No duplicate handlers or subscriptions.** `.on(...)` is registered once, in the singleton's constructor. `AppComponent` subscribes once, and both live for the whole app.
- **Single delivery** on one server instance (the single-instance assumption is accepted, D9). Edits remove only unprocessed rows.
- **Due-time comparison.** `ScheduledFor` and `now` are both `DateTimeOffset` values, compared correctly.
- **No avoidable work.** One small query a minute. Loading the calendar doesn't trigger reminder logic.
- **A user with no reminders** gets nothing queued and nothing sent.
- **Toast hover behavior.** Hovering pauses the timer and the progress bar, and leaving resumes with the remaining time. Covered by 6 specs. Kept by D4.
- **Placement, size and hierarchy** are appropriate, and the toast looks the same on every page.
- **Recipients:** only the event owner gets reminders. Confirmed as intended by D6.
- **Several tabs:** each tab shows the toast. Confirmed as intended by D8.
- **Mentions are real-time only.** Confirmed as intended by D1.

## 3. Confirmed correctness/reliability problems (non-recurrence)
Recurrence problems are R1–R6 in section 8. "Conflicts with Dn" means the current behavior contradicts a recorded decision.

| ID | What the code does | Why it's a problem | Confidence | Type |
|---|---|---|---|---|
| C1 | **A new toast dismisses the open one.** MatSnackBar dismisses any open snackbar before showing a new one (`snack-bar.mjs:565, 642`). One global `MatSnackBar` is shared by reminders, mentions, and page messages such as calendar and board save errors. | Several reminders that come due in the same sender cycle leave only the last one visible. A mention or any page message removes an unread reminder. **Conflicts with D7.** | High | Reliability bug |
| C2 | **Fixed message text.** It's built at scheduling time as `"… starts in {N} minutes"`. | It produces "starts in 0 minutes", "starts in 60 minutes" and "starts in 1440 minutes", and it's wrong whenever delivery is late. **Conflicts with D2**, which requires the text to be generated at delivery time from the actual current time. | High | UX/UI defect |
| C3 | **No relevance check before sending.** Once the backend is back after downtime, every overdue row is sent at once, however late. | Reminders for events that started long ago are delivered. **Conflicts with D2**, which discards reminders more than 5 minutes after the start. | High | Correctness bug |
| C4 | **The first connection is never retried.** If `start()` fails, for example because the backend is unreachable at login or app load, the error is only logged. Nothing retries it: `distinctUntilChanged` filters out the repeated `true` from token refreshes, and automatic reconnect only applies after a successful first connection. | A logged-in client stays without a realtime connection until the page is reloaded, even after the backend becomes reachable again. | High | **Reliability defect** (independent of D1) |
| C5 | **Reconnecting gives up.** The default policy is `[0, 2000, 10000, 30000, null]` (`DefaultReconnectPolicy.js:4`). After about 42 s the connection closes, and `onclose` only logs. | A backend restart or network loss lasting longer than the retry window leaves a logged-in client disconnected until reload. | High that it happens; medium on how often | **Reliability defect** (independent of D1) |
| C6 | **"View event" only searches the loaded range.** It matches against events already loaded, and the payload has no start date the calendar could navigate to. | A reminder for an event outside the visible day, week or month shows "Could not find that event". For a recurring series it opens the first loaded occurrence, which may not be the one being reminded about. | High | UX defect (conditional) |
| C7 | **Dark mode follows the OS, not the app.** The toast's dark styling uses `@media (prefers-color-scheme: dark)`, but the app's theme is the `body.dark-theme` class set by `ThemeService`. | When the app and OS themes differ, the toast shows the wrong theme. | High | UI defect |
| C8 | **Sending to an absent user.** A reminder sent while the user has no live connection is marked processed and never delivered (`SendAsync` succeeds with zero connections). | **Conflicts with D1:** an authenticated user who was temporarily away should get still-relevant reminders when they return. | High | Correctness bug (given D1) |
| C9 | **Several overdue reminders for one event are all sent.** For example, the 15-minute and 5-minute reminders after downtime, each with its own text. | **Conflicts with D2:** only the most recent relevant reminder per event or occurrence should be shown. | High (follows from the sender loop) | Correctness bug (given D2) |

**C4/C5 vs D1.** C4 and C5 are about whether a logged-in client reliably recovers its realtime connection while the backend is reachable. That's a defect regardless of D1. D1 is about what happens to reminders that came due while the client was away (C8). Fixing one doesn't fix the other.

## 4. UX/UI findings
**Is the toast pattern appropriate?** Yes, as decided. D3, D4 and D7 keep the auto-expiring toast and add queuing plus pauses for hover, focus and hidden tabs. There's no persistent card, browser notification or notification center. With D1 covering short absences, the toast is the intended sole reminder UI.

| ID | Finding | Category | Confidence |
|---|---|---|---|
| U1 | The text problems C2/C3. Resolved in intent by D2 (text generated at delivery time). | Defect, must fix | High |
| U2 | The toast's start-time line and event-color bar (`data.timeUntil`, `data.color`) are never filled, because the payload only carries `{eventId, message}`. D2's delivery-time relative text covers *when*, relatively. The actual start time and the color are still missing. | UX improvement (not decided) | High |
| U3 | Theme mismatch (C7). | Defect | High |
| U4 | **Background tab:** the 12 s countdown runs while the tab is hidden, so the toast can come and go without being seen. | **Defect, given D3/D4** (the countdown must pause while the tab is hidden) | High |
| U5 | **Keyboard timing:** the countdown pauses only on mouse hover, not while keyboard focus is inside the toast (WCAG 2.2.1). | **Accessibility defect.** D4 also requires the focus pause. | High |
| U6 | **ARIA:** `role="alertdialog"` is used on an element that never takes focus. MatSnackBar already announces the content through its own assertive live region, and the inner `aria-live="assertive"` may cause double announcements. | Accessibility, minor | Medium (not tested with a screen reader) |
| U7 | Several simultaneous reminders replace each other (C1). | Defect (D7) | High |
| U8 | "View event" navigation (C6). | Defect (conditional, not decided) | High |
| U9 | Two controls do the same thing (✕ and "Dismiss"). D4 keeps the View event and Dismiss actions; whether to keep ✕ isn't specified. | Optional polish | High |

## 5. SignalR/realtime findings

| Checked | Current behavior | Classification |
|---|---|---|
| First connection fails | Not retried (C4). | Reliability defect |
| Automatic reconnect | Recovers within about 42 s, then stops for good (C5). | Reliability defect for longer outages |
| Long outages, laptop sleep | Recover only within the retry window. | Same as C5. Absences that outlast the session end in logout (F1). |
| Login, logout, idle logout | Correct. Idle logout counts as logout for D1. | Not a problem |
| Token refresh | Correct on connect and reconnect. An established connection isn't re-checked when its token expires (`AddSignalR()` defaults). | Not a problem within one tab |
| Logout in another tab | The other tab doesn't know (no `storage` listener) and stays connected until it disconnects or its next API call fails. | Theoretical / low. Tabs of one browser share one session (F2). |
| Duplicate handlers | None. | Not a problem |
| Several tabs | Each tab connects and shows its own toast. | Intended (D8) |
| Anonymous connections | None. | Not a problem |
| Timing | Delivery is up to about 60 s after `ScheduledFor`, because the sender runs about once a minute. That fits inside D2's 5-minute grace period, so a 0-minute reminder stays relevant. | Not a problem under D2 |
| Sending to a user with no live connection | `SendAsync` succeeds without error, and the row is marked processed. | Conflicts with D1 (C8) |

**The behavior the client should provide:** while authenticated, it should end up connected whenever the backend is reachable, whether the connection was lost or the first attempt failed. Currently that's met only for short drops.

## 6. Frontend architecture findings
- **Responsibilities are separated appropriately** (high confidence). The service owns the transport and the streams, the shell decides presentation, and the toast owns its own timer. The RxJS usage is simple and suitable. There are no leaks.
- **The one real coupling problem is the shared global `MatSnackBar`** (C1). D7 requires that reminder toasts are shown one after another and are never replaced by mentions or page messages. So the single toast slot needs coordination.
- **Would a notification service or store solve a current problem?** Only that coordination (D7). D1 rules out notification history and a notification center, so a store with history or read state isn't justified.
- **Maintainability:** adequate for the two notification types.

## 7. Backend architecture findings
- **The delivery pipeline is sound** (high confidence). The queue survives restarts, the sender is a hosted service with a fresh scope per cycle, sending is behind the `INotificationHubService` abstraction, and `Clients.User` targets correctly.
- **Architectural concern (high confidence):** a queue row represents one concrete instant with fixed text. It has no occurrence identity and no event start time, and the sender can't tell delivered from sent-to-nobody. Several decided behaviors therefore can't be satisfied by the current row shape and sender:
  - D2: relevance relative to the event start, text generated at delivery, and the collapse rule
  - D1: delivery on return
  - the recurrence invariant (8.4)

  This describes the gap; it prescribes no mechanism.
- **Single-instance dependency (D9, accepted).** Single delivery (section 2) and any notion of whether a user is currently connected or active, as D1 needs, currently rely on one API instance. This must be revisited before scaling out.
- **Calendar coupling of the queue:** acceptable. Reminders are the only scheduled notification type, and mentions stay real-time only (D1).
- **`ReminderSchedulerService` in Infrastructure:** cosmetic. Not a problem.
- **Dead state:** `CalendarReminder.IsSent`/`SentAt` are only initialized and never read or updated (high). Low impact.
- **Future scalability:**
  - No index on `NotificationQueues(IsProcessed, ScheduledFor)`, and processed rows are never pruned.
  - Duplicate sending if more than one instance runs. Out of scope while D9 holds.
- **Theoretical:** a row whose send keeps throwing is retried indefinitely.

## 8. Recurring-event findings

### 8.1 Current reminder model (verified)
- **Where offsets belong:** `CalendarReminder` rows are keyed to the series' **root** `CalendarEvent`. There are no reminder rows on segments, exceptions or occurrences. So offsets are de facto series-level. **D5 confirms series-level as the intended model.** A "this and following" split creates a new segment under the same root, so every segment shares the root's offsets.
- **Do occurrences inherit the offsets?**
  - **In the UI, yes:** projected occurrences carry the root's real `Id` (`RecurringEventProjectionService.cs:225, 254`).
  - **For scheduling, no:** only the root's `StartDate`, the first occurrence, is ever scheduled (R1).
- **Does moving an occurrence move its reminder?** No.
  - Moving or cancelling an occurrence (by drag, or by the "this occurrence" scope) writes an exception and never touches the queue. If the moved occurrence is the first one, its pending reminder still fires at the original time (R4).
  - For any later occurrence, no reminder exists to move (R1).
  - "Entire series" saves send no start or end time, so they can't move occurrence times. "This and following" can, through a new segment, but the root's queued reminder doesn't follow.

### 8.2 Changing reminder offsets while editing an existing recurring event (verified)
Every edit of an existing recurring event (recurring before and after the edit) opens the scope dialog **when the user saves**, after the form has been edited (`calendar-dialog.component.ts:675–699`). Until then, the Reminders field is visible and editable regardless of scope.

| Scope chosen | Endpoint | Is `reminderMinutes` sent? | Current result | Decided (D5) |
|---|---|---|---|---|
| Only this occurrence | `PUT /occurrence` | No | Discarded without any error | Reminders read-only, with the note "Reminders apply to the entire series." |
| This and all following | `PUT /occurrence/from` | No | Discarded without any error | Same as above |
| Entire series (preserve or override exceptions) | `PUT /series/…` | No | Discarded without any error | **Editable and saved** |

The current behavior conflicts with D5 for "entire series" (changes aren't saved). For the other two scopes, the scope isn't known while the user is editing. F6 (approved, section 12a) handles that by restricting the scope choice at save time.

### 8.3 Failing scenarios (all high confidence)

| ID | Scenario | Current result | Status |
|---|---|---|---|
| R1 | 2nd and later occurrences of any series | No reminder ever becomes due. | Must fix (8.4) |
| R2 | Series whose first occurrence is already in the past | No reminders at all. | Must fix (8.4) |
| R3 | Changing offsets on an existing recurring event | Discarded in every scope. | Must fix (D5: editable for the entire series, read-only otherwise) |
| R4 | First occurrence moved or cancelled while its reminder is pending | The reminder still fires at the original time, including for a cancelled occurrence. | Must fix (8.4) |
| R5 | Series renamed while the first-occurrence reminder is pending | The text uses the old subject. | Must fix (D2 text at delivery time, and 8.4) |
| R6 | Recurrence turned off, or a plain event made recurring | Plain update, rescheduled from the root start. | **Correct** |

**What the queue represents for a recurring series today:** at most one set of rows, for the root's first occurrence, with fixed text and no occurrence identity.

### 8.4 Behavioral invariant (desired behavior)
> **For every non-cancelled occurrence of a recurring series and each configured reminder offset, exactly one reminder should become due at the occurrence's actual start time minus that offset, except where the final delivery policy (D1/D2) treats it as stale or missed.**

Under the recorded decisions, "stale or missed" means three things:
- **D2:** the reminder is discarded more than 5 minutes after the occurrence's start. When several reminders for the same occurrence are relevant at once, only the most recent is shown. A newer one is discarded while one for that occurrence is displayed (F5).
- **D1:** reminders that came due while the user was logged out are not delivered.
- **D1:** mentions aren't covered by this invariant at all.

The invariant applies to open-ended series. It implies:
- **Moved occurrences:** the reminder is due relative to the moved time.
- **Cancelled occurrences:** none becomes due.
- **Time zones and DST:** times as computed by the recurrence engine, in the segment's time zone and across DST transitions.
- **Duplicates:** never more than one due reminder per (occurrence, offset).
- **Multiple offsets:** each offset gives its own due reminder.

This is behavioral only. It implies no particular storage, queue shape or scheduling strategy.

### 8.5 Constraints the existing recurrence architecture imposes (high confidence)
- Occurrences are **virtual**. Only `RecurringEventProjectionService` (segments + exceptions + time zone, with all-day series expanded in UTC) knows actual occurrence times.
- Series can be open-ended.
- An occurrence is identified by `(SeriesUid, OccurrenceDate)`, and exceptions are keyed the same way.
- Offsets are series-level only (D5; no per-occurrence configuration).

## 9. Offline/disconnected reminder semantics (decided: D1, D2)
**The technical fact** (high confidence): today the sender can't tell "delivered" apart from "sent to nobody". `SendAsync` succeeds with zero connections, the row is marked processed, and SignalR doesn't buffer messages for disconnected clients.

| Situation | Current result | Decided behavior |
|---|---|---|
| Authenticated, briefly disconnected, reconnecting | Never shown | Delivered on return if still relevant (D1, D2). **Gap: C8.** |
| Authenticated, offline, asleep, tab or browser closed | Never shown | Delivered on return if still relevant, **while the session is still valid** (D1, D2, F1). **Gap: C8.** |
| Session expired while away (15 idle minutes, or the 8 h cap) | Never shown | Treated as logged out. Not delivered after logging in again (F1). |
| Logged out, including idle-timeout logout | Never shown | Not delivered later. Logout is **per session**: another valid session can still receive the reminder (D1, F2). How this is determined: F3. |
| Logged back in | Reminders due after login are delivered normally | Matches D1 |
| Client never connected (C4) or gave up (C5) | Nothing for the rest of the session | C4/C5 are reliability defects. Reminders due during the outage follow D1/D2 once the connection recovers. |
| Returns after the event started | Nothing resent | Delivered if within 5 minutes of the start, otherwise discarded (D2). The text is generated at delivery time. |
| Backend down when reminders come due | Burst after recovery, fixed text (C2, C3), every offset sent (C9) | Only reminders still relevant (D2), only the most recent per event or occurrence (D2), text generated at delivery time |
| Mention while disconnected | Never shown | Matches D1 (real-time only) |

## 10. Alternative UI/architecture approaches (resolved by decisions)

| Approach | Decision |
|---|---|
| Current auto-expiring toast | **Kept** (D4), with pauses for hover, keyboard focus and hidden tab |
| Queued toasts, never replaced by a mention or page message | **Chosen** (D7) |
| Persistent reminder card | **Rejected** for now (D4) |
| Browser/OS notifications | **Rejected** for now (D3) |
| Notification center / history | **Rejected** for now (D1) |
| Hybrid toast plus history | **Rejected** for now (D1) |
| Notification service/store | Only what's needed for D7 coordination. No history or read state (D1). |

## 11. Must-fix vs should-consider vs optional

**A. Must fix**: defects, and conflicts with the recorded decisions:
- **Recurrence:** R1, R2, R4 and R5 (against the invariant in 8.4). R3 (against D5).
- **Connection recovery:** C4 and C5.
- **Toasts:** C1 (D7).
- **Late and stale reminders:**
  - C2: text generated at delivery time (D2).
  - C3: discard more than 5 minutes after the start (D2).
  - C9: only the most recent relevant reminder per event or occurrence (D2).
- **Absent users:** C8, still-relevant reminders delivered to authenticated users when they return (D1).
- **Toast pauses:** U4 (hidden tab, D4) and U5 (keyboard focus, accessibility and D4).
- **Theme:** C7 (low severity).

How these are applied:
- **Decided clarifications:** C8 follows F1–F3, C2/C3 follow F4, and C9 follows F5.
- **Approved:** R3 follows F6, and C1 follows F7.

**B. Should consider**: real issues not yet decided:
- "View event" for events outside the loaded range (C6).
- Showing the actual start time and event color (U2).
- All-day reminder timing (D10, kept open on purpose).

**C. Optional**:
- ARIA role and live-region cleanup (U6). Whether to keep ✕ next to Dismiss (U9).
- Sender cadence (not needed for D2; delivery within about 60 s is inside the 5-minute grace period).
- Console logging and the SignalR log level.
- The dead `IsSent`/`SentAt` fields.
- Queue index and pruning (future scalability).
- Multi-instance safety (out of scope while D9 holds).

**D. Not a problem**:
- App-level ownership and page navigation.
- Login and logout, token refresh, no anonymous connections, no duplicate handlers.
- Single delivery (D9). Due-time comparison. A user with no reminders.
- No avoidable polling. Hover pause and resume. Toast placement and hierarchy.
- Recipients (D6). One toast per tab (D8). Mentions real-time only (D1).
- R6.
- Cross-tab logout leakage (theoretical; see F2). The sender retrying a row forever (theoretical).
- Scheduler placement, and calendar coupling of the queue.

## 12. Decisions
- **D1: Missed reminders.**
  - An authenticated user who is temporarily disconnected, offline, asleep, or has the tab or browser closed gets reminders that came due meanwhile when they return, if still relevant (D2). This holds only **while that session is still valid** (F1).
  - Reminders that came due while the user was logged out are never delivered later. Idle-timeout logout and session expiry both count as logout (F1). Logout is per session (F2).
  - Reminders due after the user logs back in are delivered normally.
  - Mentions remain real-time only.
  - No notification history or center.
- **D2: Relevance.**
  - A reminder is relevant until **5 minutes after** the event or occurrence starts, then discarded. This applies to every late delivery, including reminders waiting in the client queue (F4).
  - The wording is generated from the actual time **when the toast is about to be shown** (F4): "starts in 15 minutes", "starting now", "started 2 minutes ago".
  - When several reminders for the same event or occurrence are relevant at once, only the one with the latest due time is shown (F5).
  - The 5-minute grace period covers the sender's normal delay of up to about 60 s, so 0-minute reminders aren't dropped.
- **D3: Hidden tab.** No browser or OS notifications. The hidden-tab behavior is handled by D4 and F4.
- **D4: Toast expiry.**
  - Keep the 12 s auto-expiring toast.
  - The countdown pauses on hover, while keyboard focus is inside the toast, and while the tab is hidden.
  - When the tab becomes visible again, open and queued reminders are re-checked (F4).
  - Keep the View event and Dismiss actions. Reminders are not persistent.
- **D5: Recurring reminder settings.**
  - Reminders are series-level and can be changed only for the entire series.
  - In "this occurrence" and "this and following" they're effectively read-only, with the note "Reminders apply to the entire series."
  - No per-occurrence configuration.
  - How this is enforced in the current save-time scope dialog: F6 (approved).
- **D6: Recipients.** The event owner only (current behavior).
- **D7: Several reminders.**
  - Shown one after another.
  - A reminder never replaces another reminder, and is never lost because a mention or page message appears.
  - For the same event or occurrence, queued reminders collapse to the latest (F5). A newer one arriving while one is displayed is discarded (F5, same-occurrence rule).
  - Handling of mentions and page messages: F7 (approved).
- **D8: Several tabs.**
  - Each open tab shows the toast (current behavior).
  - Tabs in one browser share one session (F2).
- **D9: Single API instance.**
  - Accepted for now.
  - The dependencies are noted in sections 2, 7 and 11.
- **D10: All-day events.** Current behavior is unchanged and remains an **open item** to revisit separately:
  - Reminders are scheduled from local midnight in the creator's browser time zone (`calendar-dialog.component.ts:717–721`), stored unchanged.
  - "At time of event" fires at midnight, and "15 minutes before" at 23:45 the night before.
  - `TimeZoneId = "UTC"` affects recurrence expansion only.

### 12a. Clarifications

#### Decided (F1–F5)
- **F1: Session expiry. Decided.**
  - If the session expires while the user is away (15 idle minutes on the server, the 15-minute client idle logout, or the 8 h cap), the user counts as logged out.
  - Reminders missed during that time aren't delivered after the next login.
  - D1's return delivery applies only while the user's authenticated session remains valid.
  - *Accepted consequence:* the app logs out after 15 minutes without input. A user who keeps the app open but doesn't touch it for longer (for example, while practicing) is logged out and gets no reminders until they log in again.
- **F2: Multiple sessions. Decided.**
  - "Logged out" is per session, not per user. A reminder stays deliverable to any other authenticated session of the same user.
  - Tabs in one browser share stored tokens, so they're one session. Each open tab still shows reminders (D8).
- **F3: Determining "logged out at the due time". Decided.**
  - No historical authentication tracking is added. Deliverability is decided from current session and connection state, using the existing auth lifecycle.
  - A session can't come back after logout or expiry: a revoked or idle-expired refresh token can't be refreshed (`AuthService`). So a session that is valid when it returns has been valid throughout its life. A missed, still-relevant reminder is therefore deliverable to a returning session unless that session began with a **fresh login**. A fresh login means the user was logged out, so earlier reminders aren't backfilled.
  - The client already distinguishes a fresh login (the login page) from a continuing session (a reload with stored tokens, or a reconnect).
  - **Limitations documented, not solved:**
    - **The server can't tell sessions apart on a connection.** The access token carries the user (`uid`) and a per-token `jti`, but no session identifier (`AuthService.GenerateToken`). The server can't attribute a hub connection to a particular session or its login time, so per-session decisions rest on the client's knowledge of its own session.
    - **Repeats after a short disconnect.** No per-session delivery acknowledgment exists. A reminder already shown before a short disconnect may be shown again on return if it's still relevant.
    - **A new tab of an existing session** (a continuing session, not a fresh login) may show a still-relevant reminder that another tab of the same session already showed. That's consistent with D8.
    - **Single instance assumed (D9).**
- **F4: Delivery vs display time. Decided.**
  - Display time is what counts for D2's user-facing text.
  - Before a reminder is shown, it's checked for relevance (start + 5 minutes) and discarded if stale. Its wording is generated from the actual time at that moment.
  - Reminders waiting in the client queue (D7) obey the same 5-minute rule.
  - When a hidden tab becomes visible, every open or queued reminder is re-checked: discarded if no longer relevant, otherwise its text is regenerated before it's shown.
  - The text doesn't update continuously; the current toast has no such behavior.
  - *Dependencies noted:*
    - The client needs each reminder's event or occurrence start time. Today's payload `{eventId, message}` doesn't carry it (same gap as C6 and U2).
    - Relevance on the client uses the client clock. The auth code already relies on it for token expiry checks.
- **F5: "Most recent". Decided.**
  - It means the reminder with the latest due time for the same event or occurrence, normally the smallest offset.
  - If several are relevant together, only that one is shown.
  - **Same occurrence while one is displayed (F5 vs D7). Decided, option (iii):**
    - **Queued only:** if several reminders for the same event or occurrence are waiting, F5 applies and only the one with the latest due time is kept.
    - **One already displayed:** a newer reminder for the same event or occurrence that arrives while an older one is on screen is **discarded**. The displayed reminder stays according to D4. It isn't replaced, and no duplicate follows it.
    - Under F4, the displayed reminder's text is generated when it's shown and regenerated when a hidden tab becomes visible. So it already reflects the current timing, and nothing is lost.
    - Reminders that arrive after it has closed are shown normally.
    - The text still doesn't update continuously while the toast stays open in a visible tab, for example while hovered or focused (F4).
    - *Under F7:* a reminder interrupted by a page message and waiting to be shown again counts as queued. A newer reminder for the same occurrence then takes its place under F5.

#### Approved (F6, F7)
- **F6: Recurring reminder editing. Approved as proposed.**
  - *Current code (verified):*
    - The scope dialog (`RecurrenceScopeDialogComponent`) opens at save time.
    - It takes no input data today. It's self-contained, with options `this`, `thisAndFollowing` and `all`, plus `preserve`/`override` under `all`.
    - The calendar dialog has the event's original offsets (`data.eventData.reminderMinutes`) and the edited ones (form value), so "offsets were changed" can be detected there.
    - Passing that fact into the scope dialog follows the normal MatDialog data pattern.
    - **The preferred approach is therefore practical.**
  - **Decision (approved as proposed):**
    - The Reminders field stays editable in the form.
    - If the offsets were changed, the scope dialog allows only "Entire series". "Only this occurrence" and "This and all following" are disabled, and the dialog shows "Reminders apply to the entire series."
    - The existing preserve/override sub-choice and its confirmation are unchanged.
    - If the offsets weren't changed, the scope dialog behaves as today.
  - *Accepted consequence:* if a user changes reminders *and* wants another edit for only this occurrence (or this and following), they must either revert the reminder change or apply everything to the series. The two can't be combined in one save.
  - *Alternatives considered (not chosen):*
    - (a) Ask for the scope before editing: reorders the editing flow for every recurring edit, a larger UX change.
    - (b) Allow any scope and apply the reminder change to the series implicitly: contradicts D5's read-only rule for the other scopes.
    - (c) Make Reminders read-only for every recurring edit and add a separate "edit series reminders" action: a new UI element.
    - (d) Allow a non-series scope, then warn and discard only the reminder change: keeps a "discarded" outcome, which is close to today's silent loss (R3).
- **F7: Reminders vs other messages. Approved as proposed.**
  - *Current behavior (verified):* one global `MatSnackBar` shows everything.
    - Reminder and mention toasts open from `app.component.ts`.
    - Page messages are direct `snackBar.open(...)` calls in `calendar.component.ts` (4 calls: event not found, upload failure, two save failures) and `board-column.component.ts` (1 call: move failure).
    - Whatever opens last dismisses what's showing.
    - Login and registration use `ngx-toastr`. Reminders can't arrive there, because the hub connects only while authenticated.
  - **Decision (approved as proposed):**
    - **Reminders and mentions** share one first-in-first-out line in the existing toast UI. One is shown at a time, and neither replaces the other.
    - **Page messages** keep appearing immediately, because error feedback shouldn't wait behind a 12 s reminder.
    - If a page message interrupts a reminder or mention, that toast goes back to the front of the line and is shown again after the page message closes. A reminder is re-checked and its text regenerated per F4.
    - Nothing new appears while a page message is showing.
    - Page messages may still replace each other (unchanged).
    - This uses the existing `MatSnackBar` UI, with no second notification system.
  - *Approved scope note:* a page message's start and end currently aren't observable from the app shell through Material's public API, because the 5 calls open the snackbar directly. The existing page-message calls in `calendar.component.ts` and `board-column.component.ts` **may be routed through the same coordination point** as reminders and mentions. That's coordination of the existing `MatSnackBar` UI, not a second notification system.
  - *Alternatives considered (not chosen):*
    - (a) **One line for everything, page messages included:** simplest rule, but error feedback is delayed by 12 s per queued notification, and indefinitely while one is hovered, focused, or held in a hidden tab.
    - (b) **Protect reminders only:** mentions stay unqueued and can be replaced. It's narrower than D7 needs, but leaves mentions losable.
    - (c) **A separate area for reminders so they can sit beside page messages:** that's a second notification UI, excluded by F7.

### 12b. Remaining open items
No decisions are pending. Only these remain open, deliberately:
1. **D10, all-day reminder timing:** current behavior is unchanged, and it's to be revisited separately.
2. **The idle-timeout consequence under F1:** a user who keeps the app open but gives no input for more than 15 minutes is logged out and gets no reminders until they log in again. It's accepted for now and noted for future review.

## 13. Overall assessment
- **Delivery pipeline: sound and appropriately simple** (persistent queue → hosted sender → hub abstraction → app-level client → shell-hosted toast). No new real-time infrastructure is warranted, and D3/D4/D1 confirm the toast as the only reminder UI.
- **Scheduling and delivery model: doesn't meet the decided behavior.** Rows are concrete instants with fixed text, and sending doesn't know whether anyone received the message. So the recurrence invariant (8.4), D1 return delivery (C8) and the D2 relevance, text and collapse rules (C2, C3, C9) aren't met today. These are the most significant must-fix items.
- **Client connection recovery: defective** (C4, C5), independently of D1.
- **Presentation: the decided pattern with concrete defects** (C1, C7, U4, U5). D2 settles the text problem.
- **Clarifications:** F1–F5 are decided.
- **Same-occurrence rule (F5 vs D7):** decided. Queued duplicates collapse to the latest. A newer reminder that arrives while one for the same occurrence is displayed is discarded.
- **F6 and F7:** approved as proposed. Page-message calls may be routed through the same coordination point as reminders and mentions (F7).
- **Still open, deliberately:** D10, and the idle-timeout consequence under F1 (12b). No other decisions are pending.
- **Known limitations accepted under F3:**
  - A hub connection can't be attributed to a session.
  - A still-relevant reminder can be repeated after a short disconnect, or in a newly opened tab.
