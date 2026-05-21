IF OBJECT_ID('tr.ExamPaper','U') IS NULL
BEGIN
    CREATE TABLE tr.ExamPaper
    (
        ExamPaperId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        [Date] DATE NOT NULL,

        Grade VARCHAR(50) NOT NULL,
        [Subject] NVARCHAR(300) NOT NULL,

        SessionNumber INT NOT NULL,
        ShiftTypeId INT NOT NULL,

        StartTime TIME(0) NOT NULL,
        EndTime TIME(0) NOT NULL,

        DurationMinutes INT NULL,

        IsPractical BIT NOT NULL
            CONSTRAINT DF_ExamPaper_IsPractical DEFAULT ((0)),

        IsStudyDay BIT NOT NULL
            CONSTRAINT DF_ExamPaper_IsStudyDay DEFAULT ((0)),

        IsSchoolHoliday BIT NOT NULL
            CONSTRAINT DF_ExamPaper_IsSchoolHoliday DEFAULT ((0)),

        Notes NVARCHAR(1000) NULL,

        IsActive BIT NOT NULL
            CONSTRAINT DF_ExamPaper_IsActive DEFAULT ((1)),

        CreatedOn DATETIME2(0) NOT NULL
            CONSTRAINT DF_ExamPaper_CreatedOn DEFAULT (SYSUTCDATETIME()),

        CreatedBy VARCHAR(100) NOT NULL
    );
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ExamPaper_SessionNumber'
      AND parent_object_id = OBJECT_ID('tr.ExamPaper')
)
BEGIN
    ALTER TABLE tr.ExamPaper
    ADD CONSTRAINT CK_ExamPaper_SessionNumber
    CHECK (SessionNumber IN (1, 2));
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ExamPaper_ShiftTypeId'
      AND parent_object_id = OBJECT_ID('tr.ExamPaper')
)
BEGIN
    ALTER TABLE tr.ExamPaper
    ADD CONSTRAINT CK_ExamPaper_ShiftTypeId
    CHECK (ShiftTypeId IN (1, 2));
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ExamPaper_Time'
      AND parent_object_id = OBJECT_ID('tr.ExamPaper')
)
BEGIN
    ALTER TABLE tr.ExamPaper
    ADD CONSTRAINT CK_ExamPaper_Time
    CHECK (EndTime > StartTime);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ExamPaper_Date_Grade'
      AND object_id = OBJECT_ID('tr.ExamPaper')
)
BEGIN
    CREATE INDEX IX_ExamPaper_Date_Grade
    ON tr.ExamPaper([Date], Grade);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ExamPaper_Date_Session'
      AND object_id = OBJECT_ID('tr.ExamPaper')
)
BEGIN
    CREATE INDEX IX_ExamPaper_Date_Session
    ON tr.ExamPaper([Date], SessionNumber);
END;



IF NOT EXISTS (SELECT 1 FROM tr.ExamPaper)
BEGIN
    INSERT INTO tr.ExamPaper
    (
        [Date],
        Grade,
        [Subject],
        SessionNumber,
        ShiftTypeId,
        StartTime,
        EndTime,
        DurationMinutes,
        IsPractical,
        IsStudyDay,
        IsSchoolHoliday,
        Notes,
        IsActive,
        CreatedOn,
        CreatedBy
    )
    VALUES

    /* =========================
       18 May 2026
       ========================= */

    ('2026-05-18', 'Grade 8',  N'EBW Besig',              1, 1, '07:30', '11:00',  60, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-18', 'Grade 9',  N'KK',                     1, 1, '07:30', '11:00',  60, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-18', 'Grade 10', N'Eng FAL P2',             1, 1, '07:30', '11:00', 120, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-18', 'Grade 11', N'Besigheidstudies V1',    1, 1, '07:30', '11:00', 120, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-18', 'Grade 12', N'RTT V1',                 1, 1, '08:00', '12:00', 180, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),

    /* =========================
       19 May 2026
       ========================= */

    ('2026-05-19', 'Grade 8',  N'Studiedag',              1, 1, '07:30', '11:00', NULL, 0, 1, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-19', 'Grade 9',  N'Studiedag',              1, 1, '07:30', '11:00', NULL, 0, 1, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-19', 'Grade 10', N'Kuns / Ontwerp Prakties',1, 1, '07:30', '14:00', 420, 1, 0, 0, N'Practical paper uses special practical times.', 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-19', 'Grade 11', N'RTT V1',                 1, 1, '07:30', '11:00', 120, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-19', 'Grade 12', N'Afrikaans V2',           1, 1, '08:00', '12:00', 150, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-19', 'Grade 12', N'Toerisme PAT Mediation', 1, 1, '08:00', '12:00', 120, 1, 0, 0, N'PAT mediation shown on Grade 12 timetable.', 1, SYSUTCDATETIME(), 'exam-pdf-seed'),

    /* =========================
       20 May 2026
       ========================= */

    ('2026-05-20', 'Grade 8',  N'LO',                     1, 1, '07:30', '11:00',  60, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-20', 'Grade 9',  N'EBW Rek',                1, 1, '07:30', '11:00',  60, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-20', 'Grade 10', N'RTT V1',                 1, 1, '07:30', '11:00', 120, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-20', 'Grade 11', N'Afrikaans V2',           1, 1, '07:30', '11:00', 150, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-20', 'Grade 12', N'Toerisme PAT',           1, 1, '08:00', '12:00', 240, 1, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),

    /* =========================
       21 May 2026
       ========================= */

    ('2026-05-21', 'Grade 8',  N'Studiedag',              1, 1, '07:30', '11:00', NULL, 0, 1, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-21', 'Grade 9',  N'Studiedag',              1, 1, '07:30', '11:00', NULL, 0, 1, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-21', 'Grade 10', N'IT V1 / Kuns / Ontwerp Prakties', 1, 1, '07:30', '14:00', NULL, 1, 0, 0, N'Multiple practical papers shown in same cell.', 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-21', 'Grade 11', N'IT V2 / Gasvry - Vaardigheid',    1, 1, '07:30', '14:00', NULL, 1, 0, 0, N'Multiple practical papers shown in same cell.', 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-21', 'Grade 12', N'Toerisme PAT',           1, 1, '08:00', '12:00', 240, 1, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),

    /* =========================
       22 May 2026
       ========================= */

    ('2026-05-22', 'Grade 8',  N'Teg / Gasv',             1, 1, '07:30', '11:00',  90, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-22', 'Grade 9',  N'LO',                     1, 1, '07:30', '11:00',  60, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-22', 'Grade 10', N'LW / Ekonomie V1',       1, 1, '07:30', '11:00', NULL, 0, 0, 0, N'LW 2.5h / Ekonomie V1 1.5h.', 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-22', 'Grade 11', N'Wiskunde V1 / Gelet V1', 1, 1, '07:30', '11:00', NULL, 0, 0, 0, N'Wiskunde V1 2h / Gelet V1 1.5h.', 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-22', 'Grade 11', N'IGO V1',                 2, 2, '11:30', '14:30', 150, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-22', 'Grade 12', N'Afrikaans V1',           1, 1, '08:00', '12:00', 120, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-22', 'Grade 12', N'IT V1',                  1, 1, '08:00', '12:00', 180, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed'),
    ('2026-05-22', 'Grade 12', N'Toerisme',               2, 2, '13:00', '17:00', 180, 0, 0, 0, NULL, 1, SYSUTCDATETIME(), 'exam-pdf-seed');
END