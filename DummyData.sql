-- Insert dummy data into Priority table
INSERT INTO db_clefcraft.dbo.Priorities (Name, DateCreated, CreatedBy, ModifiedBy) VALUES
('Low', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Medium', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('High', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Urgent', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e');

-- Insert dummy data into Status table
INSERT INTO db_clefcraft.dbo.Statuses (Name, DateCreated, CreatedBy, ModifiedBy) VALUES
('Not Started', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('In Progress', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Completed', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('On Hold', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Cancelled', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e');

-- Insert dummy data into Tags table
INSERT INTO db_clefcraft.dbo.Tags (Name, DateCreated, CreatedBy, ModifiedBy) VALUES
('Practice', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Performance', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Technique', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Rehearsal', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Sight-Reading', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Warm-up', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Recital', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '944d0156-cb3d-466f-a1ea-5f53e3a10f8e');

-- Insert Boards
INSERT INTO db_clefcraft.dbo.Boards (Title, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES

('Software Development Process', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Music', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Content Creation', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Marketing Campaign Board', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Fitness Training Board', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e');

-- Insert BoardColumns for Software Development Process
INSERT INTO db_clefcraft.dbo.BoardColumns (Title, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
('To-Do', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Analysis', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('In-Progress', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('On-Hold', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Test', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Go-Live', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Done', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Routine', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Sight-Reading', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Practicing', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Rehearsal', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e');

-- Insert dummy data into BoardColumnMappings table
INSERT INTO db_clefcraft.dbo.BoardColumnMappings (BoardId, BoardColumnId, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
(1, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Software Development Process -> To-Do
(1, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Software Development Process -> Analysis
(1, 3, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Software Development Process -> In-Progress
(1, 4, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Software Development Process -> On-Hold
(1, 5, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Software Development Process -> Test
(1, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Software Development Process -> Go-Live
(1, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Software Development Process -> Done

(2, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Music Practice Board -> Routine
(2, 8, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Music Practice Board -> To-Do
(2, 9, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Music Practice Board -> Sight-Reading
(2, 4, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Music Practice Board -> Practicing
(2, 7, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'), -- Music Practice Board -> Rehearsal
(2, 9, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'); -- Music Practice Board -> Done

-- Continue with other board and column mappings as needed

-- Insert BoardItems for Software Development Process
INSERT INTO db_clefcraft.dbo.BoardItems (Title, Description, StatusId, PriorityId, Assignee, DueDate, EstimatedTime, TimeSpent, BoardColumnId, BoardId, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES

-- To-Do
('Set up project repository', 'Initialize Git repository for the project.', 1, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 2, 0, 1, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Define project scope', 'Determine the scope and requirements of the software.', 1, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 4, 0, 1, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Create wireframes', 'Design wireframes for the application.', 1, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 6, 0, 1, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Write user stories', 'Draft user stories based on requirements.', 1, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 3, 0, 1, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Prepare development environment', 'Set up local development environment.', 1, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 5, 0, 1, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- Analysis
('Analyze user feedback', 'Review feedback from previous software versions.', 2, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 3, 0, 2, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Conduct market research', 'Research similar software solutions in the market.', 2, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 5, 0, 2, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Identify technical risks', 'Assess potential technical challenges.', 2, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 4, 0, 2, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- In-Progress
('Implement authentication', 'Develop user authentication 944d0156-cb3d-466f-a1ea-5f53e3a10f8e.', 3, 3, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 10, 2, 3, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Create API endpoints', 'Develop RESTful API endpoints for the application.', 3, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 12, 3, 3, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Design database schema', 'Design the database structure for the application.', 3, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 8, 2, 3, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- On-Hold
('Awaiting design approval', 'Hold until design is finalized.', 4, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 0, 0, 4, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Waiting for backend completion', 'On hold until backend is ready.', 4, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 0, 0, 4, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- Test
('Conduct unit testing', 'Run tests for individual components.', 5, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 6, 2, 5, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Perform integration testing', 'Test interaction between different modules.', 5, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 8, 3, 5, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- Go-Live
('Deploy to production', 'Move the application to the live environment.', 3, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 5, 1, 6, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Notify users of launch', 'Inform users about the software launch.', 3, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 2, 1, 6, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- Done
('Complete project review', 'Conduct a final review of the project.', 2, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 3, 3, 7, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Archive project documentation', 'Store project documentation for future reference.', 2, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), 2, 1, 7, 1, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e');

-- Insert BoardColumns for Music Practice Board


-- Insert BoardItems for Music Practice Board
INSERT INTO db_clefcraft.dbo.BoardItems (Title, Description, StatusId, PriorityId, Assignee, DueDate, EstimatedTime, TimeSpent, BoardColumnId, BoardId, DateCreated, CreatedBy, DateModified, ModifiedBy) VALUES
-- Routine
('Warm-up exercises', 'Daily warm-up for 10 minutes.', 1, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 9, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Breath control', 'Practice breath control techniques.', 1, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 9, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Scales practice', 'Play major and minor scales.', 1, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 9, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Interval training', 'Work on musical intervals.', 1, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 1, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- To-Do
('Select pieces for recital', 'Choose music pieces for the upcoming recital.', 1, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 4, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Gather sheet music', 'Collect all sheet music for practice.', 1, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 4, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- Sight-Reading
('New piece: Moonlight Sonata', 'Start sight-reading Beethoven’s Moonlight Sonata.', 1, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 7, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('New piece: Canon in D', 'Sight-read Pachelbel’s Canon in D.', 1, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 7, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- Practicing
('Drill finger exercises', 'Focus on finger exercises for agility.', 2, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 4, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Work on dynamics', 'Practice dynamics in pieces.', 2, 1, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 4, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- Rehearsal
('Group rehearsal for concert', 'Plan a group rehearsal before concert.', 1, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 9, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Rehearse with accompanist', 'Schedule rehearsal with piano accompaniment.', 1, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 1, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),

-- Done
('Mastered piece: Fur Elise', 'Successfully mastered Beethoven’s Fur Elise.', 3, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 8, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e'),
('Completed scales practice', 'Consistently practiced scales for 30 days.', 3, 2, '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), NULL, NULL, 8, 2, GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e', GETDATE(), '944d0156-cb3d-466f-a1ea-5f53e3a10f8e');
-- Insert BoardColumns for Content Creation Board
