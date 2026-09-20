USE [ZanisServices]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProfileParty](
	[Id] [uniqueidentifier] NOT NULL,
	[PartyRef] [int] NOT NULL,
	[UserName] [nvarchar](50) COLLATE Arabic_CI_AI NOT NULL,
	[AboutMe] [nvarchar](500) COLLATE Arabic_CI_AI NULL,
	[IsBlackList] [bit] NULL,
	[Rate] [int] NULL,
 CONSTRAINT [PK_ProfileParty] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Professors](
	[Id] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Name] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[LastName] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[Specialization] [nvarchar](500) COLLATE Arabic_CI_AI NULL,
	[IsOk] [bit] NULL,
 CONSTRAINT [PK_CourseTeachers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CourseBuyers](
	[Id] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Name] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[PartyRef] [int] NULL,
	[Date] [date] NULL,
	[Amount] [decimal](18, 0) NULL,
	[ReferrerName] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[IdentificationCode] [nvarchar](40) COLLATE Arabic_CI_AI NULL,
	[PhoneNumber] [varchar](20) COLLATE Arabic_CI_AI NULL,
	[IsCourseApproved] [bit] NULL,
	[IsFullyPaid] [bit] NULL,
 CONSTRAINT [PK_CourseBuyers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Course](
	[Id] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Title] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[Description] [nvarchar](max) COLLATE Arabic_CI_AI NULL,
	[Price] [decimal](18, 0) NULL,
	[PaymentTerms] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[StartDate] [date] NULL,
	[EndDate] [date] NULL,
	[CourseLevel] [int] NULL,
	[CategoryRef] [uniqueidentifier] NULL,
	[CityRef] [int] NULL,
	[Duration] [date] NULL,
	[IsBlackList] [bit] NULL,
	[CourseCapacity] [int] NULL,
	[RemainingCapacity] [int] NULL,
	[IsPaid] [bit] NULL,
 CONSTRAINT [PK_Course] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CourseProfessors](
	[Id] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[ProfessorRef] [uniqueidentifier] NOT NULL,
	[CourseRef] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_CourseProfessors] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CoursePlan](
	[Id] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[CourseRef] [uniqueidentifier] NOT NULL,
	[PaymentType] [int] NULL,
	[NumberAmountPaid] [int] NULL,
	[Discount] [decimal](18, 0) NULL,
 CONSTRAINT [PK_CoursePlan] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PostProfile](
	[Id] [uniqueidentifier] NOT NULL,
	[ProfileRef] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](50) COLLATE Arabic_CI_AI NOT NULL,
	[Post] [nvarchar](500) COLLATE Arabic_CI_AI NOT NULL,
	[PostPhotoRef] [uniqueidentifier] NULL,
	[LikePost] [bigint] NULL,
	[CreationDate] [date] NULL,
	[Type] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
 CONSTRAINT [PK_PostProfile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PartyFile](
	[Id] [uniqueidentifier] NOT NULL,
	[PartyRef] [int] NOT NULL,
	[FileName] [nvarchar](250) COLLATE Arabic_CI_AI NULL,
	[FileExtensions] [nvarchar](5) COLLATE Arabic_CI_AI NULL,
	[IsOk] [bit] NULL,
	[IsMain] [bit] NULL,
	[Tables] [nvarchar](80) COLLATE Arabic_CI_AI NULL,
	[Path]  AS ((('/media/images/'+[Tables])+'/')+[FileName]),
	[ProfileRef] [uniqueidentifier] NULL,
 CONSTRAINT [PK_PartyFile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PartyComment](
	[Id] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[PostRef] [uniqueidentifier] NOT NULL,
	[ProfileRef] [uniqueidentifier] NOT NULL,
	[Comment] [nvarchar](500) COLLATE Arabic_CI_AI NOT NULL,
	[CreationDate] [date] NULL,
 CONSTRAINT [PK_PartyComment] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExtraPartyData](
	[Id] [uniqueidentifier] NOT NULL,
	[DateBirth] [date] NULL,
	[Gender] [bit] NULL,
	[HistoryElectricalActivity] [nvarchar](500) COLLATE Arabic_CI_AI NULL,
	[ShopOperationalHistory] [nvarchar](500) COLLATE Arabic_CI_AI NULL,
	[StoreName] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[TypeOwnership] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[AreaSpecialization] [nvarchar](50) COLLATE Arabic_CI_AI NULL,
	[ImageBusinessLicenseRef] [uniqueidentifier] NULL,
	[PartyRef] [int] NOT NULL,
 CONSTRAINT [PK_ExtraPartyData] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PostFile](
	[Id] [uniqueidentifier] NOT NULL,
	[ProfileRef] [uniqueidentifier] NOT NULL,
	[FileName] [nvarchar](250) COLLATE Arabic_CI_AI NULL,
	[FileExtensions] [nvarchar](5) COLLATE Arabic_CI_AI NULL,
	[IsOk] [bit] NULL,
	[IsMain] [bit] NULL,
	[Tables] [nvarchar](80) COLLATE Arabic_CI_AI NULL,
	[Path]  AS ((('/media/images/'+[Tables])+'/')+[FileName]),
	[PostRef] [uniqueidentifier] NULL,
 CONSTRAINT [PK_PostFile] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CourseBuyersItem](
	[Id] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[CourseBuyerRef] [uniqueidentifier] NOT NULL,
	[CourseRef] [uniqueidentifier] NOT NULL,
	[PaymentTypeRef] [uniqueidentifier] NOT NULL,
	[Price] [decimal](18, 0) NULL,
	[Rate] [int] NULL,
	[Discount] [decimal](18, 0) NULL,
	[TotalPaidAmount] [decimal](18, 0) NULL,
	[RemainingAmount] [decimal](18, 0) NULL,
 CONSTRAINT [PK_CourseBuyersItem] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
CREATE NONCLUSTERED INDEX [IX_PartyFile_ProfileRef] ON [dbo].[PartyFile]
(
	[ProfileRef] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_PostFile_PostRef] ON [dbo].[PostFile]
(
	[PostRef] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Professors] ADD  CONSTRAINT [DF_CourseTeachers_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[CourseBuyers] ADD  CONSTRAINT [DF_CourseBuyers_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Course] ADD  CONSTRAINT [DF_Course_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[CourseProfessors] ADD  CONSTRAINT [DF_CourseProfessors_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[CoursePlan] ADD  CONSTRAINT [DF_CoursePlan_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[PartyComment] ADD  CONSTRAINT [DF_PartyComment_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[CourseBuyersItem] ADD  CONSTRAINT [DF_CourseBuyersItem_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[CourseProfessors]  WITH CHECK ADD  CONSTRAINT [FK_CourseProfessors_Course] FOREIGN KEY([ProfessorRef])
REFERENCES [dbo].[Course] ([Id])
GO
ALTER TABLE [dbo].[CourseProfessors] CHECK CONSTRAINT [FK_CourseProfessors_Course]
GO
ALTER TABLE [dbo].[CourseProfessors]  WITH CHECK ADD  CONSTRAINT [FK_CourseProfessors_CourseProfessors] FOREIGN KEY([Id])
REFERENCES [dbo].[Professors] ([Id])
GO
ALTER TABLE [dbo].[CourseProfessors] CHECK CONSTRAINT [FK_CourseProfessors_CourseProfessors]
GO
ALTER TABLE [dbo].[CoursePlan]  WITH CHECK ADD  CONSTRAINT [FK_CoursePlan_Course] FOREIGN KEY([CourseRef])
REFERENCES [dbo].[Course] ([Id])
GO
ALTER TABLE [dbo].[CoursePlan] CHECK CONSTRAINT [FK_CoursePlan_Course]
GO
ALTER TABLE [dbo].[PostProfile]  WITH CHECK ADD  CONSTRAINT [FK_PostProfile_ProfileParty] FOREIGN KEY([ProfileRef])
REFERENCES [dbo].[ProfileParty] ([Id])
GO
ALTER TABLE [dbo].[PostProfile] CHECK CONSTRAINT [FK_PostProfile_ProfileParty]
GO
ALTER TABLE [dbo].[PartyFile]  WITH CHECK ADD  CONSTRAINT [FK_PartyFile_ProfileParty] FOREIGN KEY([ProfileRef])
REFERENCES [dbo].[ProfileParty] ([Id])
GO
ALTER TABLE [dbo].[PartyFile] CHECK CONSTRAINT [FK_PartyFile_ProfileParty]
GO
ALTER TABLE [dbo].[PartyComment]  WITH CHECK ADD  CONSTRAINT [FK_PartyComment_PostProfile] FOREIGN KEY([PostRef])
REFERENCES [dbo].[PostProfile] ([Id])
GO
ALTER TABLE [dbo].[PartyComment] CHECK CONSTRAINT [FK_PartyComment_PostProfile]
GO
ALTER TABLE [dbo].[PartyComment]  WITH CHECK ADD  CONSTRAINT [FK_PartyComment_ProfileParty] FOREIGN KEY([ProfileRef])
REFERENCES [dbo].[ProfileParty] ([Id])
GO
ALTER TABLE [dbo].[PartyComment] CHECK CONSTRAINT [FK_PartyComment_ProfileParty]
GO
ALTER TABLE [dbo].[ExtraPartyData]  WITH CHECK ADD  CONSTRAINT [FK_ExtraPartyData_PartyFile] FOREIGN KEY([ImageBusinessLicenseRef])
REFERENCES [dbo].[PartyFile] ([Id])
GO
ALTER TABLE [dbo].[ExtraPartyData] CHECK CONSTRAINT [FK_ExtraPartyData_PartyFile]
GO
ALTER TABLE [dbo].[PostFile]  WITH CHECK ADD  CONSTRAINT [FK_PostFile_PostProfile] FOREIGN KEY([PostRef])
REFERENCES [dbo].[PostProfile] ([Id])
GO
ALTER TABLE [dbo].[PostFile] CHECK CONSTRAINT [FK_PostFile_PostProfile]
GO
ALTER TABLE [dbo].[PostFile]  WITH CHECK ADD  CONSTRAINT [FK_PostFile_ProfileParty] FOREIGN KEY([ProfileRef])
REFERENCES [dbo].[ProfileParty] ([Id])
GO
ALTER TABLE [dbo].[PostFile] CHECK CONSTRAINT [FK_PostFile_ProfileParty]
GO
ALTER TABLE [dbo].[CourseBuyersItem]  WITH CHECK ADD  CONSTRAINT [FK_CourseBuyersItem_Course] FOREIGN KEY([CourseRef])
REFERENCES [dbo].[Course] ([Id])
GO
ALTER TABLE [dbo].[CourseBuyersItem] CHECK CONSTRAINT [FK_CourseBuyersItem_Course]
GO
ALTER TABLE [dbo].[CourseBuyersItem]  WITH CHECK ADD  CONSTRAINT [FK_CourseBuyersItem_CourseBuyers] FOREIGN KEY([CourseBuyerRef])
REFERENCES [dbo].[CourseBuyers] ([Id])
GO
ALTER TABLE [dbo].[CourseBuyersItem] CHECK CONSTRAINT [FK_CourseBuyersItem_CourseBuyers]
GO
ALTER TABLE [dbo].[CourseBuyersItem]  WITH CHECK ADD  CONSTRAINT [FK_CourseBuyersItem_CoursePlan] FOREIGN KEY([PaymentTypeRef])
REFERENCES [dbo].[CoursePlan] ([Id])
GO
ALTER TABLE [dbo].[CourseBuyersItem] CHECK CONSTRAINT [FK_CourseBuyersItem_CoursePlan]
GO

IF OBJECT_ID(N'dbo.RevokedTokens') IS NULL
    CREATE TABLE dbo.RevokedTokens (Jti nvarchar(50) NOT NULL CONSTRAINT PK_RevokedTokens PRIMARY KEY, ExpiresAt datetime NOT NULL);
GO
