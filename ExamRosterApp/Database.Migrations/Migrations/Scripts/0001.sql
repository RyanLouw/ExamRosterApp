IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'tr')
    EXEC('CREATE SCHEMA tr');


/* =========================
   ADMIN AUDIT LOG
   ========================= */
IF OBJECT_ID('tr.AdminAuditLog','U') IS NULL
BEGIN
    CREATE TABLE tr.AdminAuditLog
    (
        AuditId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EntityName VARCHAR(100) NOT NULL,
        EntityId VARCHAR(50) NOT NULL,
        Action VARCHAR(20) NOT NULL,
        ChangedOn DATETIME2(0) NOT NULL,
        ChangedBy VARCHAR(100) NOT NULL,
        Summary VARCHAR(400) NULL,
        OldValues VARCHAR(MAX) NULL,
        NewValues VARCHAR(MAX) NULL
    );
END

IF OBJECT_ID('tr.Teacher','U') IS NOT NULL
AND COL_LENGTH('tr.Teacher','TeacherGroupId') IS NULL
BEGIN
    ALTER TABLE tr.Teacher
    ADD TeacherGroupId INT NOT NULL
        CONSTRAINT DF_Teacher_TeacherGroupId DEFAULT ((1));
END

IF OBJECT_ID('tr.Teacher','U') IS NOT NULL
AND COL_LENGTH('tr.Teacher','MinDutyMinutes') IS NULL
BEGIN
    ALTER TABLE tr.Teacher
    ADD MinDutyMinutes INT NULL;
END

IF OBJECT_ID('tr.Teacher','U') IS NOT NULL
AND COL_LENGTH('tr.Teacher','MaxDutyMinutes') IS NULL
BEGIN
    ALTER TABLE tr.Teacher
    ADD MaxDutyMinutes INT NULL;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.AdminAuditLog')
      AND c.name = 'ChangedOn'
)
BEGIN
    ALTER TABLE tr.AdminAuditLog
    ADD CONSTRAINT DF_AdminAuditLog_ChangedOn
    DEFAULT (SYSUTCDATETIME()) FOR ChangedOn;
END

IF OBJECT_ID('tr.Teacher','U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Teacher_TeacherGroupId'
      AND parent_object_id = OBJECT_ID('tr.Teacher')
)
BEGIN
    ALTER TABLE tr.Teacher
    ADD CONSTRAINT CK_Teacher_TeacherGroupId
    CHECK (TeacherGroupId IN (1, 2, 3));
END

IF OBJECT_ID('tr.Teacher','U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Teacher_MinDutyMinutes'
      AND parent_object_id = OBJECT_ID('tr.Teacher')
)
BEGIN
    ALTER TABLE tr.Teacher
    ADD CONSTRAINT CK_Teacher_MinDutyMinutes
    CHECK (MinDutyMinutes IS NULL OR MinDutyMinutes >= 0);
END

IF OBJECT_ID('tr.Teacher','U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Teacher_MaxDutyMinutes'
      AND parent_object_id = OBJECT_ID('tr.Teacher')
)
BEGIN
    ALTER TABLE tr.Teacher
    ADD CONSTRAINT CK_Teacher_MaxDutyMinutes
    CHECK (MaxDutyMinutes IS NULL OR MaxDutyMinutes >= 0);
END

IF OBJECT_ID('tr.Teacher','U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Teacher_MinMaxDutyMinutes'
      AND parent_object_id = OBJECT_ID('tr.Teacher')
)
BEGIN
    ALTER TABLE tr.Teacher
    ADD CONSTRAINT CK_Teacher_MinMaxDutyMinutes
    CHECK (MinDutyMinutes IS NULL OR MaxDutyMinutes IS NULL OR MaxDutyMinutes >= MinDutyMinutes);
END


/* =========================
   TEACHER
   Matches:
   public class Teacher
   ========================= */
IF OBJECT_ID('tr.Teacher','U') IS NULL
BEGIN
    CREATE TABLE tr.Teacher
    (
        TeacherId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        FullName VARCHAR(200) NOT NULL,

        CanWorkMorning BIT NOT NULL,
        CanWorkAfternoon BIT NOT NULL,
        TeacherGroupId INT NOT NULL,
        MinDutyMinutes INT NULL,
        MaxDutyMinutes INT NULL,

        IsActive BIT NOT NULL,

        CreatedOn DATETIME2(0) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,

        UpdatedOn DATETIME2(0) NULL,
        UpdatedBy VARCHAR(100) NULL
    );
END

IF OBJECT_ID('tr.ExamDutySlot','U') IS NOT NULL
AND COL_LENGTH('tr.ExamDutySlot','LearnerCount') IS NULL
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD LearnerCount INT NULL;
END

IF OBJECT_ID('tr.ExamDutySlot','U') IS NOT NULL
AND COL_LENGTH('tr.ExamDutySlot','LearnersPerInvigilator') IS NULL
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD LearnersPerInvigilator INT NULL;
END

IF COL_LENGTH('tr.Teacher','CanWorkMorning') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.Teacher')
      AND c.name = 'CanWorkMorning'
)
BEGIN
    ALTER TABLE tr.Teacher
    ADD CONSTRAINT DF_Teacher_CanWorkMorning
    DEFAULT ((1)) FOR CanWorkMorning;
END

IF OBJECT_ID('tr.ExamDutySlot','U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ExamDutySlot_LearnerCount'
      AND parent_object_id = OBJECT_ID('tr.ExamDutySlot')
)
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD CONSTRAINT CK_ExamDutySlot_LearnerCount
    CHECK (LearnerCount IS NULL OR LearnerCount > 0);
END

IF OBJECT_ID('tr.ExamDutySlot','U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ExamDutySlot_LearnersPerInvigilator'
      AND parent_object_id = OBJECT_ID('tr.ExamDutySlot')
)
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD CONSTRAINT CK_ExamDutySlot_LearnersPerInvigilator
    CHECK (LearnersPerInvigilator IS NULL OR LearnersPerInvigilator > 0);
END

IF COL_LENGTH('tr.Teacher','CanWorkAfternoon') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.Teacher')
      AND c.name = 'CanWorkAfternoon'
)
BEGIN
    ALTER TABLE tr.Teacher
    ADD CONSTRAINT DF_Teacher_CanWorkAfternoon
    DEFAULT ((1)) FOR CanWorkAfternoon;
END

IF COL_LENGTH('tr.Teacher','IsActive') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.Teacher')
      AND c.name = 'IsActive'
)
BEGIN
    ALTER TABLE tr.Teacher
    ADD CONSTRAINT DF_Teacher_IsActive
    DEFAULT ((1)) FOR IsActive;
END

IF COL_LENGTH('tr.Teacher','CreatedOn') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.Teacher')
      AND c.name = 'CreatedOn'
)
BEGIN
    ALTER TABLE tr.Teacher
    ADD CONSTRAINT DF_Teacher_CreatedOn
    DEFAULT (SYSUTCDATETIME()) FOR CreatedOn;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Teacher_FullName'
      AND object_id = OBJECT_ID('tr.Teacher')
)
BEGIN
    CREATE INDEX IX_Teacher_FullName
    ON tr.Teacher(FullName);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Teacher_IsActive'
      AND object_id = OBJECT_ID('tr.Teacher')
)
BEGIN
    CREATE INDEX IX_Teacher_IsActive
    ON tr.Teacher(IsActive);
END


/* =========================
   UNAVAILABLE SLOT
   Matches:
   public List<UnavailableSlot> UnavailableSlots
   ========================= */
IF OBJECT_ID('tr.UnavailableSlot','U') IS NULL
BEGIN
    CREATE TABLE tr.UnavailableSlot
    (
        UnavailableSlotId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        TeacherId INT NOT NULL,

        [Date] DATE NOT NULL,
        StartTime TIME(0) NOT NULL,
        EndTime TIME(0) NOT NULL,

        Reason VARCHAR(500) NULL,

        CreatedOn DATETIME2(0) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL
    );
END

IF COL_LENGTH('tr.UnavailableSlot','CreatedOn') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.UnavailableSlot')
      AND c.name = 'CreatedOn'
)
BEGIN
    ALTER TABLE tr.UnavailableSlot
    ADD CONSTRAINT DF_UnavailableSlot_CreatedOn
    DEFAULT (SYSUTCDATETIME()) FOR CreatedOn;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_UnavailableSlot_Teacher'
)
BEGIN
    ALTER TABLE tr.UnavailableSlot
    ADD CONSTRAINT FK_UnavailableSlot_Teacher
    FOREIGN KEY (TeacherId) REFERENCES tr.Teacher(TeacherId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_UnavailableSlot_Time'
      AND parent_object_id = OBJECT_ID('tr.UnavailableSlot')
)
BEGIN
    ALTER TABLE tr.UnavailableSlot
    ADD CONSTRAINT CK_UnavailableSlot_Time
    CHECK (EndTime > StartTime);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UnavailableSlot_TeacherId'
      AND object_id = OBJECT_ID('tr.UnavailableSlot')
)
BEGIN
    CREATE INDEX IX_UnavailableSlot_TeacherId
    ON tr.UnavailableSlot(TeacherId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UnavailableSlot_Date_Time'
      AND object_id = OBJECT_ID('tr.UnavailableSlot')
)
BEGIN
    CREATE INDEX IX_UnavailableSlot_Date_Time
    ON tr.UnavailableSlot([Date], StartTime, EndTime);
END


/* =========================
   EXAM DUTY SLOT
   Matches:
   public class ExamDutySlot
   ========================= */
IF OBJECT_ID('tr.ExamDutySlot','U') IS NULL
BEGIN
    CREATE TABLE tr.ExamDutySlot
    (
        ExamDutySlotId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        [Date] DATE NOT NULL,
        StartTime TIME(0) NOT NULL,
        EndTime TIME(0) NOT NULL,

        Grade VARCHAR(50) NOT NULL,
        [Subject] VARCHAR(200) NOT NULL,
        Venue VARCHAR(200) NOT NULL,

        ShiftTypeId INT NOT NULL,

        TeachersRequired INT NOT NULL,
        LearnerCount INT NULL,
        LearnersPerInvigilator INT NULL,

        IsActive BIT NOT NULL,

        CreatedOn DATETIME2(0) NOT NULL,
        CreatedBy VARCHAR(100) NOT NULL,

        UpdatedOn DATETIME2(0) NULL,
        UpdatedBy VARCHAR(100) NULL
    );
END

IF COL_LENGTH('tr.ExamDutySlot','IsActive') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.ExamDutySlot')
      AND c.name = 'IsActive'
)
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD CONSTRAINT DF_ExamDutySlot_IsActive
    DEFAULT ((1)) FOR IsActive;
END

IF COL_LENGTH('tr.ExamDutySlot','CreatedOn') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.ExamDutySlot')
      AND c.name = 'CreatedOn'
)
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD CONSTRAINT DF_ExamDutySlot_CreatedOn
    DEFAULT (SYSUTCDATETIME()) FOR CreatedOn;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ExamDutySlot_ShiftTypeId'
      AND parent_object_id = OBJECT_ID('tr.ExamDutySlot')
)
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD CONSTRAINT CK_ExamDutySlot_ShiftTypeId
    CHECK (ShiftTypeId IN (1, 2));
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ExamDutySlot_TeachersRequired'
      AND parent_object_id = OBJECT_ID('tr.ExamDutySlot')
)
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD CONSTRAINT CK_ExamDutySlot_TeachersRequired
    CHECK (TeachersRequired > 0);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ExamDutySlot_Time'
      AND parent_object_id = OBJECT_ID('tr.ExamDutySlot')
)
BEGIN
    ALTER TABLE tr.ExamDutySlot
    ADD CONSTRAINT CK_ExamDutySlot_Time
    CHECK (EndTime > StartTime);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ExamDutySlot_Date_Time'
      AND object_id = OBJECT_ID('tr.ExamDutySlot')
)
BEGIN
    CREATE INDEX IX_ExamDutySlot_Date_Time
    ON tr.ExamDutySlot([Date], StartTime, EndTime);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ExamDutySlot_ShiftTypeId'
      AND object_id = OBJECT_ID('tr.ExamDutySlot')
)
BEGIN
    CREATE INDEX IX_ExamDutySlot_ShiftTypeId
    ON tr.ExamDutySlot(ShiftTypeId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ExamDutySlot_IsActive'
      AND object_id = OBJECT_ID('tr.ExamDutySlot')
)
BEGIN
    CREATE INDEX IX_ExamDutySlot_IsActive
    ON tr.ExamDutySlot(IsActive);
END


/* =========================
   ROSTER RUN
   Not in your first class list, but strongly recommended.
   This lets you save multiple generated rosters.
   ========================= */
IF OBJECT_ID('tr.RosterRun','U') IS NULL
BEGIN
    CREATE TABLE tr.RosterRun
    (
        RosterRunId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        [Name] VARCHAR(200) NOT NULL,

        GeneratedOn DATETIME2(0) NOT NULL,
        GeneratedBy VARCHAR(100) NOT NULL,

        IsActive BIT NOT NULL,

        Notes VARCHAR(1000) NULL
    );
END

IF COL_LENGTH('tr.RosterRun','GeneratedOn') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.RosterRun')
      AND c.name = 'GeneratedOn'
)
BEGIN
    ALTER TABLE tr.RosterRun
    ADD CONSTRAINT DF_RosterRun_GeneratedOn
    DEFAULT (SYSUTCDATETIME()) FOR GeneratedOn;
END

IF COL_LENGTH('tr.RosterRun','IsActive') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.RosterRun')
      AND c.name = 'IsActive'
)
BEGIN
    ALTER TABLE tr.RosterRun
    ADD CONSTRAINT DF_RosterRun_IsActive
    DEFAULT ((1)) FOR IsActive;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_RosterRun_GeneratedOn'
      AND object_id = OBJECT_ID('tr.RosterRun')
)
BEGIN
    CREATE INDEX IX_RosterRun_GeneratedOn
    ON tr.RosterRun(GeneratedOn);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_RosterRun_IsActive'
      AND object_id = OBJECT_ID('tr.RosterRun')
)
BEGIN
    CREATE INDEX IX_RosterRun_IsActive
    ON tr.RosterRun(IsActive);
END


/* =========================
   DUTY ASSIGNMENT
   Matches:
   public class DutyAssignment
   ========================= */
IF OBJECT_ID('tr.DutyAssignment','U') IS NULL
BEGIN
    CREATE TABLE tr.DutyAssignment
    (
        DutyAssignmentId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        RosterRunId INT NOT NULL,

        TeacherId INT NOT NULL,
        ExamDutySlotId INT NOT NULL,

        AssignedOn DATETIME2(0) NOT NULL,

        IsManualOverride BIT NOT NULL,
        ManualOverrideReason VARCHAR(1000) NULL
    );
END

IF COL_LENGTH('tr.DutyAssignment','AssignedOn') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.DutyAssignment')
      AND c.name = 'AssignedOn'
)
BEGIN
    ALTER TABLE tr.DutyAssignment
    ADD CONSTRAINT DF_DutyAssignment_AssignedOn
    DEFAULT (SYSUTCDATETIME()) FOR AssignedOn;
END

IF COL_LENGTH('tr.DutyAssignment','IsManualOverride') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.DutyAssignment')
      AND c.name = 'IsManualOverride'
)
BEGIN
    ALTER TABLE tr.DutyAssignment
    ADD CONSTRAINT DF_DutyAssignment_IsManualOverride
    DEFAULT ((0)) FOR IsManualOverride;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_DutyAssignment_RosterRun'
)
BEGIN
    ALTER TABLE tr.DutyAssignment
    ADD CONSTRAINT FK_DutyAssignment_RosterRun
    FOREIGN KEY (RosterRunId) REFERENCES tr.RosterRun(RosterRunId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_DutyAssignment_Teacher'
)
BEGIN
    ALTER TABLE tr.DutyAssignment
    ADD CONSTRAINT FK_DutyAssignment_Teacher
    FOREIGN KEY (TeacherId) REFERENCES tr.Teacher(TeacherId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_DutyAssignment_ExamDutySlot'
)
BEGIN
    ALTER TABLE tr.DutyAssignment
    ADD CONSTRAINT FK_DutyAssignment_ExamDutySlot
    FOREIGN KEY (ExamDutySlotId) REFERENCES tr.ExamDutySlot(ExamDutySlotId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = 'UQ_DutyAssignment_RosterRun_Teacher_ExamDutySlot'
      AND parent_object_id = OBJECT_ID('tr.DutyAssignment')
)
BEGIN
    ALTER TABLE tr.DutyAssignment
    ADD CONSTRAINT UQ_DutyAssignment_RosterRun_Teacher_ExamDutySlot
    UNIQUE (RosterRunId, TeacherId, ExamDutySlotId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DutyAssignment_RosterRunId'
      AND object_id = OBJECT_ID('tr.DutyAssignment')
)
BEGIN
    CREATE INDEX IX_DutyAssignment_RosterRunId
    ON tr.DutyAssignment(RosterRunId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DutyAssignment_TeacherId'
      AND object_id = OBJECT_ID('tr.DutyAssignment')
)
BEGIN
    CREATE INDEX IX_DutyAssignment_TeacherId
    ON tr.DutyAssignment(TeacherId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_DutyAssignment_ExamDutySlotId'
      AND object_id = OBJECT_ID('tr.DutyAssignment')
)
BEGIN
    CREATE INDEX IX_DutyAssignment_ExamDutySlotId
    ON tr.DutyAssignment(ExamDutySlotId);
END


/* =========================
   ROSTER WARNING
   Stores generator warnings:
   not enough teachers, impossible slot, etc.
   ========================= */
IF OBJECT_ID('tr.RosterWarning','U') IS NULL
BEGIN
    CREATE TABLE tr.RosterWarning
    (
        RosterWarningId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        RosterRunId INT NOT NULL,
        ExamDutySlotId INT NULL,

        WarningType VARCHAR(50) NOT NULL,
        WarningMessage VARCHAR(1000) NOT NULL,

        CreatedOn DATETIME2(0) NOT NULL
    );
END

IF COL_LENGTH('tr.RosterWarning','CreatedOn') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('tr.RosterWarning')
      AND c.name = 'CreatedOn'
)
BEGIN
    ALTER TABLE tr.RosterWarning
    ADD CONSTRAINT DF_RosterWarning_CreatedOn
    DEFAULT (SYSUTCDATETIME()) FOR CreatedOn;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_RosterWarning_RosterRun'
)
BEGIN
    ALTER TABLE tr.RosterWarning
    ADD CONSTRAINT FK_RosterWarning_RosterRun
    FOREIGN KEY (RosterRunId) REFERENCES tr.RosterRun(RosterRunId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_RosterWarning_ExamDutySlot'
)
BEGIN
    ALTER TABLE tr.RosterWarning
    ADD CONSTRAINT FK_RosterWarning_ExamDutySlot
    FOREIGN KEY (ExamDutySlotId) REFERENCES tr.ExamDutySlot(ExamDutySlotId);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_RosterWarning_RosterRunId'
      AND object_id = OBJECT_ID('tr.RosterWarning')
)
BEGIN
    CREATE INDEX IX_RosterWarning_RosterRunId
    ON tr.RosterWarning(RosterRunId);
END


/* =========================
   SAMPLE DATA
   Remove later if you want empty DB
   ========================= */
IF NOT EXISTS (SELECT 1 FROM tr.Teacher)
BEGIN
    INSERT INTO tr.Teacher
    (
        FullName,
        CanWorkMorning,
        CanWorkAfternoon,
        IsActive,
        CreatedOn,
        CreatedBy
    )
    VALUES
    ('Mrs Smith', 1, 1, 1, SYSUTCDATETIME(), 'seed'),
    ('Mr Jones', 1, 0, 1, SYSUTCDATETIME(), 'seed'),
    ('Mrs Botha', 1, 1, 1, SYSUTCDATETIME(), 'seed'),
    ('Mr Naidoo', 0, 1, 1, SYSUTCDATETIME(), 'seed'),
    ('Mrs Van Wyk', 1, 1, 1, SYSUTCDATETIME(), 'seed');
END

IF NOT EXISTS (SELECT 1 FROM tr.ExamDutySlot)
BEGIN
    INSERT INTO tr.ExamDutySlot
    (
        [Date],
        StartTime,
        EndTime,
        Grade,
        [Subject],
        Venue,
        ShiftTypeId,
        TeachersRequired,
        IsActive,
        CreatedOn,
        CreatedBy
    )
    VALUES
    ('2026-06-01', '08:00', '10:00', 'Grade 8', 'Mathematics', 'Hall A', 1, 2, 1, SYSUTCDATETIME(), 'seed'),
    ('2026-06-01', '08:00', '10:00', 'Grade 9', 'English', 'Room 12', 1, 1, 1, SYSUTCDATETIME(), 'seed'),
    ('2026-06-01', '13:00', '15:00', 'Grade 10', 'Science', 'Hall B', 2, 2, 1, SYSUTCDATETIME(), 'seed'),
    ('2026-06-02', '08:00', '10:30', 'Grade 11', 'Accounting', 'Room 20', 1, 2, 1, SYSUTCDATETIME(), 'seed'),
    ('2026-06-02', '13:00', '15:30', 'Grade 12', 'History', 'Hall A', 2, 2, 1, SYSUTCDATETIME(), 'seed');
END
