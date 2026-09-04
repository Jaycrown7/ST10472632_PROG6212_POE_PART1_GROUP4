
IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END;
GO

USE RaceDayDB;
GO

-- =========================================================
-- DROP EXISTING TABLES
-- Allows the script to be safely tested again
-- =========================================================

IF OBJECT_ID('dbo.Result', 'U') IS NOT NULL
    DROP TABLE dbo.Result;
GO

IF OBJECT_ID('dbo.Enrolment', 'U') IS NOT NULL
    DROP TABLE dbo.Enrolment;
GO

IF OBJECT_ID('dbo.Category', 'U') IS NOT NULL
    DROP TABLE dbo.Category;
GO

IF OBJECT_ID('dbo.Event', 'U') IS NOT NULL
    DROP TABLE dbo.Event;
GO

IF OBJECT_ID('dbo.UserProfile', 'U') IS NOT NULL
    DROP TABLE dbo.UserProfile;
GO

IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL
    DROP TABLE dbo.[User];
GO

-- =========================================================
-- 1. USER
-- =========================================================

CREATE TABLE dbo.[User]
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20),
    City NVARCHAR(100),
    Role NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT CK_User_Role
        CHECK (Role IN ('Organiser', 'Participant'))
);
GO

-- =========================================================
-- 2. USER PROFILE
-- =========================================================

CREATE TABLE dbo.UserProfile
(
    ProfileID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    Address NVARCHAR(200),
    City NVARCHAR(100),
    DateOfBirth DATE,
    EmergencyContact NVARCHAR(100),
    ProfileImageUrl NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_UserProfile_User
        FOREIGN KEY (UserID)
        REFERENCES dbo.[User](UserID)
);
GO

-- =========================================================
-- 3. EVENT
-- =========================================================

CREATE TABLE dbo.Event
(
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    EventType NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Event_Organiser
        FOREIGN KEY (OrganiserID)
        REFERENCES dbo.[User](UserID),

    CONSTRAINT CK_Event_Distance
        CHECK (Distance > 0),

    CONSTRAINT CK_Event_Type
        CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);
GO

-- =========================================================
-- 4. CATEGORY
-- =========================================================

CREATE TABLE dbo.Category
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL DEFAULT 0,
    MaximumParticipants INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Category_Event
        FOREIGN KEY (EventID)
        REFERENCES dbo.Event(EventID),

    CONSTRAINT CK_Category_Distance
        CHECK (Distance > 0),

    CONSTRAINT CK_Category_EntryFee
        CHECK (EntryFee >= 0),

    CONSTRAINT CK_Category_MaxParticipants
        CHECK (MaximumParticipants > 0)
);
GO

-- =========================================================
-- 5. ENROLMENT
-- =========================================================

CREATE TABLE dbo.Enrolment
(
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',

    CONSTRAINT FK_Enrolment_Participant
        FOREIGN KEY (ParticipantID)
        REFERENCES dbo.[User](UserID),

    CONSTRAINT FK_Enrolment_Category
        FOREIGN KEY (CategoryID)
        REFERENCES dbo.Category(CategoryID),

    CONSTRAINT CK_Enrolment_Status
        CHECK (Status IN ('Active', 'Cancelled')),

    CONSTRAINT UQ_Enrolment_Participant_Category
        UNIQUE (ParticipantID, CategoryID)
);
GO

-- =========================================================
-- 6. RESULT
-- =========================================================

CREATE TABLE dbo.Result
(
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NOT NULL,
    Position INT NOT NULL,
    ResultStatus NVARCHAR(20) NOT NULL DEFAULT 'Completed',
    RecordedAt DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Result_Enrolment
        FOREIGN KEY (EnrolmentID)
        REFERENCES dbo.Enrolment(EnrolmentID),

    CONSTRAINT CK_Result_Position
        CHECK (Position > 0),

    CONSTRAINT CK_Result_Status
        CHECK (ResultStatus IN ('Pending', 'Completed'))
);
GO

-- =========================================================
-- SAMPLE DATA
-- =========================================================

-- =========================================================
-- 2 ORGANISERS + 2 PARTICIPANTS
-- =========================================================

INSERT INTO dbo.[User]
    (FirstName, LastName, Email, PasswordHash, PhoneNumber, City, Role)
VALUES
    ('Thabo', 'Mokoena', 'thabo.mokoena@raceday.co.za',
     'HASHED_PASSWORD_1', '0711111111', 'Johannesburg', 'Organiser'),

    ('Lerato', 'Mahlangu', 'lerato.mahlangu@raceday.co.za',
     'HASHED_PASSWORD_2', '0722222222', 'Pretoria', 'Organiser'),

    ('Sipho', 'Dlamini', 'sipho.dlamini@example.com',
     'HASHED_PASSWORD_3', '0733333333', 'Tembisa', 'Participant'),

    ('Amahle', 'Ndlovu', 'amahle.ndlovu@example.com',
     'HASHED_PASSWORD_4', '0744444444', 'Soweto', 'Participant');
GO

-- =========================================================
-- USER PROFILES
-- =========================================================

INSERT INTO dbo.UserProfile
    (UserID, Address, City, DateOfBirth, EmergencyContact, ProfileImageUrl)
VALUES
    (1, '12 Mandela Street', 'Johannesburg', '1988-04-15',
     'Nomsa Mokoena - 0712345678', NULL),

    (2, '45 Nelson Avenue', 'Pretoria', '1990-08-22',
     'Kabelo Mahlangu - 0723456789', NULL),

    (3, '18 Freedom Road', 'Tembisa', '2002-06-10',
     'Mpho Dlamini - 0734567890', NULL),

    (4, '27 Vilakazi Street', 'Soweto', '2001-11-03',
     'Zanele Ndlovu - 0745678901', NULL);
GO

-- =========================================================
-- 3 EVENTS
-- =========================================================

INSERT INTO dbo.Event
    (OrganiserID, EventName, Description, EventDate, Location, Distance, EventType)
VALUES
    (1, 'Tembisa Community Run',
     'A community road running event for participants of different ages.',
     '2026-11-15', 'Tembisa', 10.00, 'Run'),

    (1, 'Soweto Charity Walk',
     'A community charity walking event supporting local initiatives.',
     '2026-12-06', 'Soweto', 8.00, 'Walk'),

    (2, 'Pretoria Cycle Challenge',
     'A road cycling event through Pretoria and surrounding areas.',
     '2027-01-17', 'Pretoria', 40.00, 'Cycle');
GO

-- =========================================================
-- CATEGORIES FOR EACH EVENT
-- =========================================================

INSERT INTO dbo.Category
    (EventID, CategoryName, Distance, EntryFee, MaximumParticipants)
VALUES
    -- Tembisa Community Run
    (1, 'Under 20', 10.00, 80.00, 500),
    (1, 'Senior', 10.00, 120.00, 1000),
    (1, 'Veteran', 10.00, 100.00, 500),

    -- Soweto Charity Walk
    (2, 'Junior Walk', 8.00, 50.00, 500),
    (2, 'Open Walk', 8.00, 70.00, 1000),
    (2, 'Senior Walk', 8.00, 50.00, 500),

    -- Pretoria Cycle Challenge
    (3, 'Open Cycle', 40.00, 250.00, 1000),
    (3, 'Veteran Cycle', 40.00, 200.00, 500);
GO

-- =========================================================
-- SAMPLING ENROLMENTS
-- =========================================================

INSERT INTO dbo.Enrolment
    (ParticipantID, CategoryID, Status)
VALUES
    (3, 1, 'Active'),
    (3, 5, 'Active'),
    (4, 2, 'Active'),
    (4, 7, 'Active');
GO

-- =========================================================
-- SAMPLING RESULTS
-- =========================================================

INSERT INTO dbo.Result
    (EnrolmentID, FinishTime, Position, ResultStatus)
VALUES
    (1, '01:02:35', 14, 'Completed'),
    (2, '00:58:42', 9, 'Completed');
GO

-- =========================================================
-- VERIFYING DATA
-- =========================================================

SELECT * FROM dbo.[User];
SELECT * FROM dbo.UserProfile;
SELECT * FROM dbo.Event;
SELECT * FROM dbo.Category;
SELECT * FROM dbo.Enrolment;
SELECT * FROM dbo.Result;
GO