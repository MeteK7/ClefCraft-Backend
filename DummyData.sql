-- Insert Boards
INSERT INTO dbo.Boards (Title, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
('Software Development Process', GETDATE(), 'admin', GETDATE(), 'admin'),
('Music', GETDATE(), 'admin', GETDATE(), 'admin'),
('Content Creation', GETDATE(), 'admin', GETDATE(), 'admin'),
('Marketing Campaign Board', GETDATE(), 'admin', GETDATE(), 'admin'),
('Fitness Training Board', GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardColumns for Software Development Process
INSERT INTO dbo.BoardColumns (BoardId, Title, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
(1, 'To-Do', GETDATE(), 'admin', GETDATE(), 'admin'),
(1, 'Analysis', GETDATE(), 'admin', GETDATE(), 'admin'),
(1, 'In-Progress', GETDATE(), 'admin', GETDATE(), 'admin'),
(1, 'On-Hold', GETDATE(), 'admin', GETDATE(), 'admin'),
(1, 'Test', GETDATE(), 'admin', GETDATE(), 'admin'),
(1, 'Go-Live', GETDATE(), 'admin', GETDATE(), 'admin'),
(1, 'Done', GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardItems for Software Development Process
INSERT INTO dbo.BoardItems (Title, Description, Status, BoardColumnId, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
-- To-Do
('Set up project repository', 'Initialize Git repository for the project.', 'Pending', 1, GETDATE(), 'admin', GETDATE(), 'admin'),
('Define project scope', 'Determine the scope and requirements of the software.', 'Pending', 1, GETDATE(), 'admin', GETDATE(), 'admin'),
('Create wireframes', 'Design wireframes for the application.', 'Pending', 1, GETDATE(), 'admin', GETDATE(), 'admin'),
('Write user stories', 'Draft user stories based on requirements.', 'Pending', 1, GETDATE(), 'admin', GETDATE(), 'admin'),
('Prepare development environment', 'Set up local development environment.', 'Pending', 1, GETDATE(), 'admin', GETDATE(), 'admin'),
('Conduct team meeting', 'Hold a meeting to discuss project progress.', 'Pending', 1, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Analysis
('Analyze user feedback', 'Review feedback from previous software versions.', 'Pending', 2, GETDATE(), 'admin', GETDATE(), 'admin'),
('Conduct market research', 'Research similar software solutions in the market.', 'Pending', 2, GETDATE(), 'admin', GETDATE(), 'admin'),
('Identify technical risks', 'Assess potential technical challenges.', 'Pending', 2, GETDATE(), 'admin', GETDATE(), 'admin'),
('Create project roadmap', 'Outline the timeline and deliverables for the project.', 'Pending', 2, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review design proposals', 'Evaluate design proposals from the team.', 'Pending', 2, GETDATE(), 'admin', GETDATE(), 'admin'),
('Create documentation outline', 'Outline documentation for project development.', 'Pending', 2, GETDATE(), 'admin', GETDATE(), 'admin'),
-- In-Progress
('Implement authentication', 'Develop user authentication system.', 'In Progress', 3, GETDATE(), 'admin', GETDATE(), 'admin'),
('Create API endpoints', 'Develop RESTful API endpoints for the application.', 'In Progress', 3, GETDATE(), 'admin', GETDATE(), 'admin'),
('Design database schema', 'Design the database structure for the application.', 'In Progress', 3, GETDATE(), 'admin', GETDATE(), 'admin'),
('Develop user interface', 'Build the front-end components.', 'In Progress', 3, GETDATE(), 'admin', GETDATE(), 'admin'),
('Integrate third-party services', 'Integrate necessary third-party APIs.', 'In Progress', 3, GETDATE(), 'admin', GETDATE(), 'admin'),
('Write unit tests', 'Create unit tests for critical functions.', 'In Progress', 3, GETDATE(), 'admin', GETDATE(), 'admin'),
-- On-Hold
('Awaiting design approval', 'Hold until design is finalized.', 'On Hold', 4, GETDATE(), 'admin', GETDATE(), 'admin'),
('Waiting for backend completion', 'On hold until backend is ready.', 'On Hold', 4, GETDATE(), 'admin', GETDATE(), 'admin'),
('Pending user feedback', 'Hold until user feedback is received.', 'On Hold', 4, GETDATE(), 'admin', GETDATE(), 'admin'),
('Resource allocation pending', 'On hold until resources are allocated.', 'On Hold', 4, GETDATE(), 'admin', GETDATE(), 'admin'),
('Approval for third-party integrations', 'On hold until approvals are received.', 'On Hold', 4, GETDATE(), 'admin', GETDATE(), 'admin'),
('Awaiting budget approval', 'On hold until budget is approved.', 'On Hold', 4, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Test
('Conduct unit testing', 'Run tests for individual components.', 'Pending', 5, GETDATE(), 'admin', GETDATE(), 'admin'),
('Perform integration testing', 'Test interaction between different modules.', 'Pending', 5, GETDATE(), 'admin', GETDATE(), 'admin'),
('Conduct user acceptance testing', 'Gather user feedback on the software.', 'Pending', 5, GETDATE(), 'admin', GETDATE(), 'admin'),
('Bug fixing', 'Fix identified bugs from testing.', 'Pending', 5, GETDATE(), 'admin', GETDATE(), 'admin'),
('Performance testing', 'Test the application for performance issues.', 'Pending', 5, GETDATE(), 'admin', GETDATE(), 'admin'),
('Finalize test cases', 'Ensure all test cases are documented.', 'Pending', 5, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Go-Live
('Deploy to production', 'Move the application to the live environment.', 'Pending', 6, GETDATE(), 'admin', GETDATE(), 'admin'),
('Notify users of launch', 'Inform users about the software launch.', 'Pending', 6, GETDATE(), 'admin', GETDATE(), 'admin'),
('Set up monitoring', 'Implement monitoring tools for production.', 'Pending', 6, GETDATE(), 'admin', GETDATE(), 'admin'),
('Prepare support documentation', 'Document support procedures for users.', 'Pending', 6, GETDATE(), 'admin', GETDATE(), 'admin'),
('Finalize post-launch review', 'Plan a review after launch.', 'Pending', 6, GETDATE(), 'admin', GETDATE(), 'admin'),
('Set up feedback channels', 'Create channels for user feedback.', 'Pending', 6, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Done
('Complete project review', 'Conduct a final review of the project.', 'Completed', 7, GETDATE(), 'admin', GETDATE(), 'admin'),
('Archive project documentation', 'Store project documentation for future reference.', 'Completed', 7, GETDATE(), 'admin', GETDATE(), 'admin'),
('Celebrate project success', 'Celebrate the successful project launch.', 'Completed', 7, GETDATE(), 'admin', GETDATE(), 'admin'),
('Gather team feedback', 'Collect feedback from the team.', 'Completed', 7, GETDATE(), 'admin', GETDATE(), 'admin'),
('Prepare final report', 'Document the final project outcomes.', 'Completed', 7, GETDATE(), 'admin', GETDATE(), 'admin'),
('Close project', 'Officially close the project.', 'Completed', 7, GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardColumns for Music Practice Board
INSERT INTO dbo.BoardColumns (BoardId, Title, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
(2, 'Routine', GETDATE(), 'admin', GETDATE(), 'admin'),
(2, 'To-Do', GETDATE(), 'admin', GETDATE(), 'admin'),
(2, 'Sight-Reading', GETDATE(), 'admin', GETDATE(), 'admin'),
(2, 'Practicing', GETDATE(), 'admin', GETDATE(), 'admin'),
(2, 'Rehearsal', GETDATE(), 'admin', GETDATE(), 'admin'),
(2, 'Done', GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardItems for Music Practice Board
INSERT INTO dbo.BoardItems (Title, Description, Status, BoardColumnId, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
-- Routine
('Warm-up exercises', 'Daily warm-up for 10 minutes.', 'Pending', 8, GETDATE(), 'admin', GETDATE(), 'admin'),
('Breath control', 'Practice breath control techniques.', 'Pending', 8, GETDATE(), 'admin', GETDATE(), 'admin'),
('Scales practice', 'Play major and minor scales.', 'Pending', 8, GETDATE(), 'admin', GETDATE(), 'admin'),
('Interval training', 'Work on musical intervals.', 'Pending', 8, GETDATE(), 'admin', GETDATE(), 'admin'),
('Chord progressions', 'Practice common chord progressions.', 'Pending', 8, GETDATE(), 'admin', GETDATE(), 'admin'),
('Sight-reading daily', 'Read and play a new piece daily.', 'Pending', 8, GETDATE(), 'admin', GETDATE(), 'admin'),
-- To-Do
('Select pieces for recital', 'Choose music pieces for the upcoming recital.', 'Pending', 9, GETDATE(), 'admin', GETDATE(), 'admin'),
('Gather sheet music', 'Collect all sheet music for practice.', 'Pending', 9, GETDATE(), 'admin', GETDATE(), 'admin'),
('Schedule practice sessions', 'Plan practice sessions for the week.', 'Pending', 9, GETDATE(), 'admin', GETDATE(), 'admin'),
('Record practice sessions', 'Record each practice session for review.', 'Pending', 9, GETDATE(), 'admin', GETDATE(), 'admin'),
('Set goals for the month', 'Define musical goals for the month.', 'Pending', 9, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review recordings', 'Listen to recordings for self-assessment.', 'Pending', 9, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Sight-Reading
('New piece: Moonlight Sonata', 'Start sight-reading Beethoven’s Moonlight Sonata.', 'Pending', 10, GETDATE(), 'admin', GETDATE(), 'admin'),
('New piece: Canon in D', 'Sight-read Pachelbel’s Canon in D.', 'Pending', 10, GETDATE(), 'admin', GETDATE(), 'admin'),
('New piece: Gymnopédies', 'Sight-read Satie’s Gymnopédies.', 'Pending', 10, GETDATE(), 'admin', GETDATE(), 'admin'),
('New piece: Für Elise', 'Sight-read Beethoven’s Für Elise.', 'Pending', 10, GETDATE(), 'admin', GETDATE(), 'admin'),
('New piece: Clair de Lune', 'Sight-read Debussy’s Clair de Lune.', 'Pending', 10, GETDATE(), 'admin', GETDATE(), 'admin'),
('New piece: Prelude in C', 'Sight-read Bach’s Prelude in C.', 'Pending', 10, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Practicing
('Drill finger exercises', 'Focus on finger exercises for agility.', 'In Progress', 11, GETDATE(), 'admin', GETDATE(), 'admin'),
('Work on dynamics', 'Practice dynamics in pieces.', 'In Progress', 11, GETDATE(), 'admin', GETDATE(), 'admin'),
('Refine phrasing', 'Work on phrasing in a challenging piece.', 'In Progress', 11, GETDATE(), 'admin', GETDATE(), 'admin'),
('Practice ensemble pieces', 'Work on ensemble pieces with fellow musicians.', 'In Progress', 11, GETDATE(), 'admin', GETDATE(), 'admin'),
('Solo practice for recital', 'Focus on solo pieces for upcoming recital.', 'In Progress', 11, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review practice notes', 'Go over notes from previous lessons.', 'In Progress', 11, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Rehearsal
('Group rehearsal for concert', 'Plan a group rehearsal before concert.', 'Pending', 12, GETDATE(), 'admin', GETDATE(), 'admin'),
('Rehearse with accompanist', 'Schedule rehearsal with piano accompaniment.', 'Pending', 12, GETDATE(), 'admin', GETDATE(), 'admin'),
('Conduct full-band rehearsal', 'Hold full-band rehearsal to finalize pieces.', 'Pending', 12, GETDATE(), 'admin', GETDATE(), 'admin'),
('Rehearse difficult sections', 'Focus on difficult sections in group pieces.', 'Pending', 12, GETDATE(), 'admin', GETDATE(), 'admin'),
('Run through concert setlist', 'Run through the complete setlist for concert.', 'Pending', 12, GETDATE(), 'admin', GETDATE(), 'admin'),
('Feedback session after rehearsal', 'Hold feedback session post-rehearsal.', 'Pending', 12, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Done
('Mastered piece: Fur Elise', 'Successfully mastered Beethoven’s Fur Elise.', 'Completed', 13, GETDATE(), 'admin', GETDATE(), 'admin'),
('Completed scales practice', 'Consistently practiced scales for 30 days.', 'Completed', 13, GETDATE(), 'admin', GETDATE(), 'admin'),
('Performed in recital', 'Successfully performed in the annual recital.', 'Completed', 13, GETDATE(), 'admin', GETDATE(), 'admin'),
('Completed sight-reading challenge', 'Completed 30 days of sight-reading.', 'Completed', 13, GETDATE(), 'admin', GETDATE(), 'admin'),
('Achieved practice goal', 'Met the practice goal for the month.', 'Completed', 13, GETDATE(), 'admin', GETDATE(), 'admin'),
('Received positive feedback', 'Received feedback from instructor.', 'Completed', 13, GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardColumns for Content Creation Board
INSERT INTO dbo.BoardColumns (BoardId, Title, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
(3, 'Ideation', GETDATE(), 'admin', GETDATE(), 'admin'),
(3, 'Drafting', GETDATE(), 'admin', GETDATE(), 'admin'),
(3, 'Editing', GETDATE(), 'admin', GETDATE(), 'admin'),
(3, 'Review', GETDATE(), 'admin', GETDATE(), 'admin'),
(3, 'Published', GETDATE(), 'admin', GETDATE(), 'admin'),
(3, 'Archived', GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardItems for Content Creation Board
INSERT INTO dbo.BoardItems (Title, Description, Status, BoardColumnId, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
-- Ideation
('Brainstorm blog topics', 'Generate a list of potential blog topics for next month.', 'Pending', 14, GETDATE(), 'admin', GETDATE(), 'admin'),
('Research trending keywords', 'Identify keywords to target for SEO optimization.', 'Pending', 14, GETDATE(), 'admin', GETDATE(), 'admin'),
('Collect audience feedback', 'Gather input from the audience on content preferences.', 'Pending', 14, GETDATE(), 'admin', GETDATE(), 'admin'),
('Draft a content calendar', 'Create a calendar for content publication dates.', 'Pending', 14, GETDATE(), 'admin', GETDATE(), 'admin'),
('Analyze competitor content', 'Review competitor blogs and social media for ideas.', 'Pending', 14, GETDATE(), 'admin', GETDATE(), 'admin'),
('Set monthly goals', 'Establish content goals for the upcoming month.', 'Pending', 14, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Drafting
('Write blog post: "5 Tips for Remote Work"', 'Draft the first version of the blog post.', 'In Progress', 15, GETDATE(), 'admin', GETDATE(), 'admin'),
('Create script for video tutorial', 'Outline and write the script for the tutorial video.', 'In Progress', 15, GETDATE(), 'admin', GETDATE(), 'admin'),
('Draft social media posts', 'Prepare posts for Facebook and Instagram.', 'In Progress', 15, GETDATE(), 'admin', GETDATE(), 'admin'),
('Write newsletter content', 'Draft the content for this month’s newsletter.', 'In Progress', 15, GETDATE(), 'admin', GETDATE(), 'admin'),
('Develop webinar presentation', 'Create slides and notes for the upcoming webinar.', 'In Progress', 15, GETDATE(), 'admin', GETDATE(), 'admin'),
('Create email campaign copy', 'Write the content for the next email campaign.', 'In Progress', 15, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Editing
('Edit blog post: "5 Tips for Remote Work"', 'Proofread and edit for clarity and style.', 'Pending', 16, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review video tutorial', 'Watch the recorded tutorial and make necessary edits.', 'Pending', 16, GETDATE(), 'admin', GETDATE(), 'admin'),
('Proofread newsletter content', 'Check for grammar and spelling mistakes.', 'Pending', 16, GETDATE(), 'admin', GETDATE(), 'admin'),
('Revise social media posts', 'Ensure posts align with brand voice and strategy.', 'Pending', 16, GETDATE(), 'admin', GETDATE(), 'admin'),
('Edit webinar presentation', 'Make necessary adjustments to the presentation slides.', 'Pending', 16, GETDATE(), 'admin', GETDATE(), 'admin'),
('Check email campaign for errors', 'Review email content before sending.', 'Pending', 16, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Review
('Get feedback on blog post', 'Share the blog post draft with colleagues for feedback.', 'Pending', 17, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review video tutorial with team', 'Discuss edits and changes with the production team.', 'Pending', 17, GETDATE(), 'admin', GETDATE(), 'admin'),
('Collect input on newsletter', 'Gather team feedback on the newsletter draft.', 'Pending', 17, GETDATE(), 'admin', GETDATE(), 'admin'),
('Final review of social media posts', 'Ensure all posts are approved before scheduling.', 'Pending', 17, GETDATE(), 'admin', GETDATE(), 'admin'),
('Get approvals for webinar content', 'Confirm all details of the webinar with stakeholders.', 'Pending', 17, GETDATE(), 'admin', GETDATE(), 'admin'),
('Final checks on email content', 'Ensure email is ready for distribution.', 'Pending', 17, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Published
('Publish blog post: "5 Tips for Remote Work"', 'Schedule the blog post for publication.', 'Completed', 18, GETDATE(), 'admin', GETDATE(), 'admin'),
('Upload video tutorial', 'Post the tutorial video on the website and YouTube.', 'Completed', 18, GETDATE(), 'admin', GETDATE(), 'admin'),
('Send out newsletter', 'Distribute the newsletter to the mailing list.', 'Completed', 18, GETDATE(), 'admin', GETDATE(), 'admin'),
('Schedule social media posts', 'Use a scheduling tool to publish posts.', 'Completed', 18, GETDATE(), 'admin', GETDATE(), 'admin'),
('Conduct live webinar', 'Host the webinar as planned.', 'Completed', 18, GETDATE(), 'admin', GETDATE(), 'admin'),
('Launch email campaign', 'Send the email campaign to subscribers.', 'Completed', 18, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Archived
('Archive blog post: "5 Tips for Remote Work"', 'Store the blog post in the archives.', 'Completed', 19, GETDATE(), 'admin', GETDATE(), 'admin'),
('Save video tutorial to archives', 'Organize the tutorial video for future reference.', 'Completed', 19, GETDATE(), 'admin', GETDATE(), 'admin'),
('Store newsletter issue in archives', 'Save the newsletter in the archives.', 'Completed', 19, GETDATE(), 'admin', GETDATE(), 'admin'),
('File social media post reports', 'Archive analytics and reports for posts.', 'Completed', 19, GETDATE(), 'admin', GETDATE(), 'admin'),
('Keep webinar recordings', 'Store the recordings of the webinar for future use.', 'Completed', 19, GETDATE(), 'admin', GETDATE(), 'admin'),
('Maintain email campaign statistics', 'Save metrics from the email campaign.', 'Completed', 19, GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardColumns for Marketing Campaign Board
INSERT INTO dbo.BoardColumns (BoardId, Title, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
(4, 'Planning', GETDATE(), 'admin', GETDATE(), 'admin'),
(4, 'Execution', GETDATE(), 'admin', GETDATE(), 'admin'),
(4, 'Analysis', GETDATE(), 'admin', GETDATE(), 'admin'),
(4, 'Feedback', GETDATE(), 'admin', GETDATE(), 'admin'),
(4, 'Completed', GETDATE(), 'admin', GETDATE(), 'admin'),
(4, 'Archived', GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardItems for Marketing Campaign Board
INSERT INTO dbo.BoardItems (Title, Description, Status, BoardColumnId, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
-- Planning
('Define campaign objectives', 'Set clear goals for the marketing campaign.', 'Pending', 20, GETDATE(), 'admin', GETDATE(), 'admin'),
('Identify target audience', 'Determine the demographics and interests of the target audience.', 'Pending', 20, GETDATE(), 'admin', GETDATE(), 'admin'),
('Research competitors', 'Analyze competitor marketing strategies and performance.', 'Pending', 20, GETDATE(), 'admin', GETDATE(), 'admin'),
('Create a budget', 'Outline the financial resources required for the campaign.', 'Pending', 20, GETDATE(), 'admin', GETDATE(), 'admin'),
('Plan content calendar', 'Develop a timeline for content creation and distribution.', 'Pending', 20, GETDATE(), 'admin', GETDATE(), 'admin'),
('Select marketing channels', 'Decide which platforms to use for the campaign.', 'Pending', 20, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Execution
('Design campaign graphics', 'Create visual assets for social media and ads.', 'In Progress', 21, GETDATE(), 'admin', GETDATE(), 'admin'),
('Write ad copy', 'Draft compelling copy for advertisements.', 'In Progress', 21, GETDATE(), 'admin', GETDATE(), 'admin'),
('Launch social media campaign', 'Start posting on selected social media platforms.', 'In Progress', 21, GETDATE(), 'admin', GETDATE(), 'admin'),
('Distribute email newsletter', 'Send out the campaign newsletter to the mailing list.', 'In Progress', 21, GETDATE(), 'admin', GETDATE(), 'admin'),
('Host a launch event', 'Organize an event to promote the campaign.', 'In Progress', 21, GETDATE(), 'admin', GETDATE(), 'admin'),
('Run paid advertising', 'Implement paid ads on various platforms.', 'In Progress', 21, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Analysis
('Collect performance data', 'Gather metrics from various channels to analyze performance.', 'Pending', 22, GETDATE(), 'admin', GETDATE(), 'admin'),
('Analyze audience engagement', 'Review how the target audience is interacting with the campaign.', 'Pending', 22, GETDATE(), 'admin', GETDATE(), 'admin'),
('Assess ROI', 'Calculate return on investment from the campaign.', 'Pending', 22, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review feedback', 'Compile and assess feedback from the audience.', 'Pending', 22, GETDATE(), 'admin', GETDATE(), 'admin'),
('Compare against benchmarks', 'Evaluate campaign performance against industry standards.', 'Pending', 22, GETDATE(), 'admin', GETDATE(), 'admin'),
('Generate a performance report', 'Prepare a detailed report on campaign performance.', 'Pending', 22, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Feedback
('Collect team feedback', 'Discuss the campaign with the team to gather input.', 'Pending', 23, GETDATE(), 'admin', GETDATE(), 'admin'),
('Survey audience opinions', 'Send out surveys to gather audience opinions on the campaign.', 'Pending', 23, GETDATE(), 'admin', GETDATE(), 'admin'),
('Conduct a focus group', 'Organize a focus group to discuss the campaign outcomes.', 'Pending', 23, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review internal reports', 'Look over internal reports to identify improvement areas.', 'Pending', 23, GETDATE(), 'admin', GETDATE(), 'admin'),
('Gather insights from sales team', 'Ask the sales team for feedback on leads generated.', 'Pending', 23, GETDATE(), 'admin', GETDATE(), 'admin'),
('Evaluate customer service interactions', 'Analyze customer service data related to the campaign.', 'Pending', 23, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Completed
('Finalize campaign report', 'Complete the report detailing campaign performance.', 'Completed', 24, GETDATE(), 'admin', GETDATE(), 'admin'),
('Implement lessons learned', 'Apply feedback to improve future campaigns.', 'Completed', 24, GETDATE(), 'admin', GETDATE(), 'admin'),
('Update marketing materials', 'Revise materials based on campaign insights.', 'Completed', 24, GETDATE(), 'admin', GETDATE(), 'admin'),
('Communicate results to stakeholders', 'Share campaign results and insights with stakeholders.', 'Completed', 24, GETDATE(), 'admin', GETDATE(), 'admin'),
('Celebrate successes', 'Organize a team meeting to celebrate successful campaign outcomes.', 'Completed', 24, GETDATE(), 'admin', GETDATE(), 'admin'),
('Archive campaign assets', 'Store all campaign-related assets for future reference.', 'Completed', 24, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Archived
('Store campaign report', 'Keep the final campaign report in archives.', 'Completed', 25, GETDATE(), 'admin', GETDATE(), 'admin'),
('Save advertising materials', 'Archive all advertising materials for future use.', 'Completed', 25, GETDATE(), 'admin', GETDATE(), 'admin'),
('Document lessons learned', 'Write up a document summarizing lessons learned from the campaign.', 'Completed', 25, GETDATE(), 'admin', GETDATE(), 'admin'),
('Maintain audience insights', 'Save insights gathered from audience feedback for reference.', 'Completed', 25, GETDATE(), 'admin', GETDATE(), 'admin'),
('File all creative assets', 'Store all graphics and creative assets in the archive.', 'Completed', 25, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review archived campaigns', 'Set a schedule for reviewing archived campaigns for insights.', 'Completed', 25, GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardColumns for Fitness Training Board
INSERT INTO dbo.BoardColumns (BoardId, Title, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
(5, 'Goal Setting', GETDATE(), 'admin', GETDATE(), 'admin'),
(5, 'Workout Plans', GETDATE(), 'admin', GETDATE(), 'admin'),
(5, 'Nutrition', GETDATE(), 'admin', GETDATE(), 'admin'),
(5, 'Progress Tracking', GETDATE(), 'admin', GETDATE(), 'admin'),
(5, 'Feedback', GETDATE(), 'admin', GETDATE(), 'admin'),
(5, 'Completed', GETDATE(), 'admin', GETDATE(), 'admin');

-- Insert BoardItems for Fitness Training Board
INSERT INTO dbo.BoardItems (Title, Description, Status, BoardColumnId, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
-- Goal Setting
('Set weight loss goal', 'Aim to lose 10 pounds in 3 months.', 'Pending', 26, GETDATE(), 'admin', GETDATE(), 'admin'),
('Define workout frequency', 'Plan to work out 4 times a week.', 'Pending', 26, GETDATE(), 'admin', GETDATE(), 'admin'),
('Establish fitness level', 'Determine current fitness level for tailored program.', 'Pending', 26, GETDATE(), 'admin', GETDATE(), 'admin'),
('Identify preferred activities', 'Choose preferred types of exercises (e.g., running, yoga).', 'Pending', 26, GETDATE(), 'admin', GETDATE(), 'admin'),
('Set short-term milestones', 'Plan milestones to measure progress every month.', 'Pending', 26, GETDATE(), 'admin', GETDATE(), 'admin'),
('Create a motivational plan', 'Develop strategies to stay motivated throughout the program.', 'Pending', 26, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Workout Plans
('Design a strength training routine', 'Create a plan focusing on strength exercises.', 'In Progress', 27, GETDATE(), 'admin', GETDATE(), 'admin'),
('Include cardio sessions', 'Plan for two cardio sessions per week.', 'In Progress', 27, GETDATE(), 'admin', GETDATE(), 'admin'),
('Integrate flexibility workouts', 'Add yoga sessions twice a week.', 'In Progress', 27, GETDATE(), 'admin', GETDATE(), 'admin'),
('Schedule rest days', 'Designate rest days to prevent overtraining.', 'In Progress', 27, GETDATE(), 'admin', GETDATE(), 'admin'),
('Include warm-up and cool-down', 'Ensure each workout has proper warm-up and cool-down routines.', 'In Progress', 27, GETDATE(), 'admin', GETDATE(), 'admin'),
('Adjust workouts based on feedback', 'Modify workout plans according to progress and feedback.', 'In Progress', 27, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Nutrition
('Create a meal plan', 'Develop a weekly meal plan that fits fitness goals.', 'Pending', 28, GETDATE(), 'admin', GETDATE(), 'admin'),
('Track daily water intake', 'Ensure hydration goals are met daily.', 'Pending', 28, GETDATE(), 'admin', GETDATE(), 'admin'),
('Include protein sources', 'Identify high-protein foods for muscle building.', 'Pending', 28, GETDATE(), 'admin', GETDATE(), 'admin'),
('Plan pre- and post-workout meals', 'Create meal ideas to support workout routines.', 'Pending', 28, GETDATE(), 'admin', GETDATE(), 'admin'),
('Evaluate snack options', 'Identify healthy snacks to maintain energy levels.', 'Pending', 28, GETDATE(), 'admin', GETDATE(), 'admin'),
('Assess dietary supplements', 'Research supplements that may benefit the training plan.', 'Pending', 28, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Progress Tracking
('Log workouts', 'Maintain a log of workouts performed each week.', 'Pending', 29, GETDATE(), 'admin', GETDATE(), 'admin'),
('Track weight changes', 'Record weekly weight to monitor progress.', 'Pending', 29, GETDATE(), 'admin', GETDATE(), 'admin'),
('Measure body measurements', 'Take monthly measurements to track body changes.', 'Pending', 29, GETDATE(), 'admin', GETDATE(), 'admin'),
('Document fitness milestones', 'Keep a record of achieving fitness milestones.', 'Pending', 29, GETDATE(), 'admin', GETDATE(), 'admin'),
('Evaluate energy levels', 'Reflect on energy levels during workouts weekly.', 'Pending', 29, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review fitness journal', 'Assess the journal for insights and areas for improvement.', 'Pending', 29, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Feedback
('Gather feedback from trainer', 'Ask for input on workout effectiveness.', 'Pending', 30, GETDATE(), 'admin', GETDATE(), 'admin'),
('Conduct a self-assessment', 'Evaluate personal progress and areas of difficulty.', 'Pending', 30, GETDATE(), 'admin', GETDATE(), 'admin'),
('Incorporate group feedback', 'Discuss experiences with fitness group for insights.', 'Pending', 30, GETDATE(), 'admin', GETDATE(), 'admin'),
('Assess meal plan effectiveness', 'Review meal plan with a nutritionist for adjustments.', 'Pending', 30, GETDATE(), 'admin', GETDATE(), 'admin'),
('Collect feedback on motivation', 'Share strategies to maintain motivation with peers.', 'Pending', 30, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review exercise techniques', 'Ensure proper techniques are used to avoid injuries.', 'Pending', 30, GETDATE(), 'admin', GETDATE(), 'admin'),
-- Completed
('Achieve weight loss goal', 'Successfully lost 10 pounds in 3 months.', 'Completed', 31, GETDATE(), 'admin', GETDATE(), 'admin'),
('Complete fitness assessment', 'Successfully assessed fitness level improvement.', 'Completed', 31, GETDATE(), 'admin', GETDATE(), 'admin'),
('Finish 12-week workout plan', 'Successfully completed the designed workout plan.', 'Completed', 31, GETDATE(), 'admin', GETDATE(), 'admin'),
('Sustain healthy eating habits', 'Maintained healthy eating for 3 months.', 'Completed', 31, GETDATE(), 'admin', GETDATE(), 'admin'),
('Participate in group workouts', 'Engaged in community workouts for motivation.', 'Completed', 31, GETDATE(), 'admin', GETDATE(), 'admin'),
('Review and archive training materials', 'Organized training materials for future reference.', 'Completed', 31, GETDATE(), 'admin', GETDATE(), 'admin');