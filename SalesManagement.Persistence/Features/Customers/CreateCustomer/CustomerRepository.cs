using System.Data;
using SalesManagement.Persistence.ConnectionFactories;
using static SalesManagement.Persistence.Common.SepidarDb;
using System.Data.Common;
using System.IO;
using Dapper;
using Microsoft.Data.SqlClient;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Customers;

namespace SalesManagement.Persistence.Features.Customers.CreateCustomer
{
    public class CustomerRepository(
        ISepidarConnectionFactory sepidarFactory,
        IZanisServicesConnectionFactory hesabFactory,
        ISepidarConfigurationProvider configProvider) : ICustomerRepository
    {
        private const string SqlContext = """
            SELECT
                COALESCE(
                    (SELECT TOP (1) u.UserID FROM FMK.[User] AS u WHERE u.UserName LIKE @VisitorCode),
                    NULLIF(@DefaultUserRef, 0),
                    53) AS Creator,
                (SELECT g.GroupingID FROM GNR.Grouping AS g WHERE g.Code = @VisitorCode) AS CustomerGroupingRef;
            """;
        private const string SqlDlCode = """
            SELECT MAX(CAST(d.Code AS DECIMAL) + 1) AS Code
            FROM ACC.DL AS d
                INNER JOIN FMK.NumberedEntity AS n
                    ON d.Code BETWEEN ISNULL(n.StartValue, 1) AND ISNULL(n.FinishValue, 9999999)
            WHERE d.Type = @Type
                AND n.EntityFullName = 'SG.General.PartyManagement.Common.PartyRow';
            """;

        private const string SqlInsertDl = """
            INSERT INTO ACC.DL
                (DLId, Code, Title, Title_En, Type, Version, Creator, CreationDate, LastModifier, LastModificationDate, IsActive)
            VALUES
                (@DLId, @Code, @Title, @Title_En, @Type, @Version, @Creator, @CreationDate, @LastModifier, @LastModificationDate, @IsActive);
            """;

        private const string SqlInsertParty = """
            INSERT INTO GNR.Party
                (PartyId, Type, SubType, Name, LastName, Name_En, LastName_En, IdentificationCode, DLRef,
                 IsInBlacklist, IsVendor, IsBroker, IsEmployee, IsCustomer, CustomerCategoryForTax,
                 Version, Creator, CreationDate, LastModifier, LastModificationDate,
                 HasCredit, CreditCheckingType, DiscountRate, CommissionRate, BrokerOpeningBalance,
                 BrokerOpeningBalanceType, Email, CustomerGroupingRef)
            VALUES
                (@PartyId, @Type, @SubType, @Name, @LastName, @Name_En, @LastName_En, @IdentificationCode, @DLRef,
                 @IsInBlacklist, @IsVendor, @IsBroker, @IsEmployee, @IsCustomer, @CustomerCategoryForTax,
                 @Version, @Creator, @CreationDate, @LastModifier, @LastModificationDate,
                 @HasCredit, @CreditCheckingType, @DiscountRate, @CommissionRate, @BrokerOpeningBalance,
                 @BrokerOpeningBalanceType, @Email, @CustomerGroupingRef);
            """;

        private const string SqlInsertPhone = """
            INSERT INTO GNR.PartyPhone (PartyPhoneId, PartyRef, IsMain, Type, Phone, Version)
            VALUES (@PartyPhoneId, @PartyRef, @IsMain, @Type, @Phone, @Version);
            """;

        private const string SqlInsertAddress = """
            INSERT INTO GNR.PartyAddress
                (PartyAddressId, PartyRef, IsMain, Type, Address, LocationRef, ZipCode, Adress_En, Version, Latitude, Longitude, Title)
            VALUES
                (@PartyAddressId, @PartyRef, @IsMain, @Type, @Address, @LocationRef, @ZipCode, @Adress_En, @Version, @Latitude, @Longitude, @Title);
            """;

        private const string SqlInsertPartyFile = """
            INSERT INTO dbo.PartyFile (Id, PartyRef, FileName, FileExtensions, IsOk, IsMain, Tables)
            VALUES (@Id, @PartyRef, @FileName, @FileExtensions, @IsOk, @IsMain, @Tables);
            """;

        private const string SqlInsertExtra = """
            INSERT INTO dbo.ExtraPartyData
                (Id, DateBirth, Gender, HistoryElectricalActivity, ShopOperationalHistory,
                 StoreName, TypeOwnership, AreaSpecialization, ImageBusinessLicenseRef, PartyRef)
            VALUES
                (@Id, @DateBirth, @Gender, @HistoryElectricalActivity, @ShopOperationalHistory,
                 @StoreName, @TypeOwnership, @AreaSpecialization, @ImageBusinessLicenseRef, @PartyRef);
            """;

        private const string SqlSelectPartyForUpdate = """
            SELECT TOP (1) p.PartyId, p.DLRef
            FROM GNR.Party AS p
            WHERE p.PartyId = @PartyId AND p.IsCustomer = 1;
            """;

        private const string SqlUpdateDl = """
            UPDATE ACC.DL
            SET Title = @Title, Title_En = @Title_En, LastModifier = @LastModifier, LastModificationDate = @LastModificationDate
            WHERE DLId = @DLId;
            """;

        private const string SqlUpdateParty = """
            UPDATE GNR.Party
            SET Name = @Name, LastName = @LastName, Name_En = @Name_En, LastName_En = @LastName_En,
                IdentificationCode = @IdentificationCode, LastModifier = @LastModifier, LastModificationDate = @LastModificationDate
            WHERE PartyId = @PartyId;
            """;

        private const string SqlUpdatePhone = """
            UPDATE GNR.PartyPhone SET Phone = @Phone
            WHERE PartyRef = @PartyRef AND Type = @Type AND IsMain = @IsMain;
            """;

        private const string SqlUpdateAddress = """
            UPDATE GNR.PartyAddress
            SET Address = @Address, LocationRef = @LocationRef, ZipCode = @ZipCode, Adress_En = @Adress_En,
                Latitude = @Latitude, Longitude = @Longitude
            WHERE PartyRef = @PartyRef AND IsMain = 1;
            """;

        private const string SqlSelectLicenseFile = """
            SELECT TOP (1) Id, FileName FROM dbo.PartyFile WHERE PartyRef = @PartyRef AND Tables = 'Party';
            """;

        private const string SqlDeleteLicenseFile = """
            DELETE FROM dbo.PartyFile WHERE Id = @Id;
            """;

        private const string SqlUpdateExtra = """
            UPDATE dbo.ExtraPartyData
            SET DateBirth = @DateBirth, Gender = @Gender, HistoryElectricalActivity = @HistoryElectricalActivity,
                ShopOperationalHistory = @ShopOperationalHistory, StoreName = @StoreName,
                TypeOwnership = @TypeOwnership, AreaSpecialization = @AreaSpecialization,
                ImageBusinessLicenseRef = COALESCE(@NewImageBusinessLicenseRef, ImageBusinessLicenseRef)
            WHERE PartyRef = @PartyRef;
            """;

        public async Task<int> CreateAsync(
            NewCustomer c, string? visitorCode, CancellationToken cancellationToken = default)
        {
            var defaultUserRef = configProvider.Get().DefaultUserRef;

            await using var sepidar = (DbConnection)sepidarFactory.CreateConnection();
            await using var hesab = (DbConnection)hesabFactory.CreateConnection();
            await sepidar.OpenAsync(cancellationToken);
            await hesab.OpenAsync(cancellationToken);

            await using var tx = await sepidar.BeginTransactionAsync(cancellationToken);
            await using var txHesab = await hesab.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.Now;

                var ctx = await sepidar.QuerySingleAsync<RegistrationContext>(
                    SqlContext, new { VisitorCode = visitorCode, DefaultUserRef = defaultUserRef }, tx);

                var locationRef = c.LocationRef > 0 ? c.LocationRef : configProvider.Get().DefaultLocationRef;
                var locationExists = await sepidar.ExecuteScalarAsync<int?>(
                    "SELECT 1 FROM GNR.Location WHERE LocationId = @LocationRef;",
                    new { LocationRef = locationRef }, tx);
                if (locationExists is null)
                    throw new DomainException("شهر/استان انتخاب‌شده معتبر نیست");

                var dlId = await NextIdAsync(sepidar, tx, "ACC.DL");
                var partyId = await NextIdAsync(sepidar, tx, "GNR.Party");
                var addressId = await NextIdAsync(sepidar, tx, "GNR.PartyAddress");
                var phone1 = await NextIdAsync(sepidar, tx, "GNR.PartyPhone");
                var phone2 = await NextIdAsync(sepidar, tx, "GNR.PartyPhone");
                var phone3 = await NextIdAsync(sepidar, tx, "GNR.PartyPhone");

                var dlCode = (await sepidar.ExecuteScalarAsync<int?>(SqlDlCode, new { Type = 2 }, tx) ?? 10000).ToString();

                var title = $"{c.LastName} {c.FirstName}".Trim();

                var pDl = new DynamicParameters();
                pDl.Add("@DLId", dlId);
                pDl.Add("@Code", dlCode);
                pDl.Add("@Title", title);
                pDl.Add("@Title_En", title);
                pDl.Add("@Type", 2);
                pDl.Add("@Version", 1);
                pDl.Add("@Creator", ctx.Creator);
                pDl.Add("@CreationDate", now);
                pDl.Add("@LastModifier", ctx.Creator);
                pDl.Add("@LastModificationDate", now);
                pDl.Add("@IsActive", 1);
                await sepidar.ExecuteAsync(SqlInsertDl, pDl, tx);

                var pParty = new DynamicParameters();
                pParty.Add("@PartyId", partyId);
                pParty.Add("@Type", 1);
                pParty.Add("@SubType", 7);
                pParty.Add("@Name", c.FirstName);
                pParty.Add("@LastName", c.LastName);
                pParty.Add("@Name_En", c.FirstName);
                pParty.Add("@LastName_En", c.LastName);
                pParty.Add("@IdentificationCode", c.NationalCode);
                pParty.Add("@DLRef", dlId);
                pParty.Add("@IsInBlacklist", 1);
                pParty.Add("@IsVendor", 0);
                pParty.Add("@IsBroker", 0);
                pParty.Add("@IsEmployee", 0);
                pParty.Add("@IsCustomer", 1);
                pParty.Add("@CustomerCategoryForTax", 2);
                pParty.Add("@Version", 1);
                pParty.Add("@Creator", ctx.Creator);
                pParty.Add("@CreationDate", now);
                pParty.Add("@LastModifier", ctx.Creator);
                pParty.Add("@LastModificationDate", now);
                pParty.Add("@HasCredit", 0);
                pParty.Add("@CreditCheckingType", 3);
                pParty.Add("@DiscountRate", 0);
                pParty.Add("@CommissionRate", 0);
                pParty.Add("@BrokerOpeningBalance", null);
                pParty.Add("@BrokerOpeningBalanceType", 2);
                pParty.Add("@Email", null);
                pParty.Add("@CustomerGroupingRef", ctx.CustomerGroupingRef);
                await sepidar.ExecuteAsync(SqlInsertParty, pParty, tx);

                await InsertPhone(sepidar, tx, phone1, partyId, isMain: 0, type: 1, phone: c.LandlinePhone);
                await InsertPhone(sepidar, tx, phone2, partyId, isMain: 1, type: 2, phone: c.MobilePhone);
                await InsertPhone(sepidar, tx, phone3, partyId, isMain: 0, type: 2, phone: c.MessengerPhone);

                var pAddr = new DynamicParameters();
                pAddr.Add("@PartyAddressId", addressId);
                pAddr.Add("@PartyRef", partyId);
                pAddr.Add("@IsMain", 1);
                pAddr.Add("@Type", 2);
                pAddr.Add("@Address", c.Address);
                pAddr.Add("@LocationRef", locationRef);
                pAddr.Add("@ZipCode", c.ZipCode);
                pAddr.Add("@Adress_En", c.Address);
                pAddr.Add("@Version", 1);
                pAddr.Add("@Latitude", c.Latitude);
                pAddr.Add("@Longitude", c.Longitude);
                pAddr.Add("@Title", "این آدرس هنگام ثبت‌نام وارد شده");
                await sepidar.ExecuteAsync(SqlInsertAddress, pAddr, tx);

                Guid? licenseFileId = null;
                if (c.LicenseFileName is not null)
                {
                    licenseFileId = Guid.NewGuid();
                    var pFile = new DynamicParameters();
                    pFile.Add("@Id", licenseFileId);
                    pFile.Add("@PartyRef", partyId);
                    pFile.Add("@FileName", c.LicenseFileName);
                    pFile.Add("@FileExtensions", Path.GetExtension(c.LicenseFileName));
                    pFile.Add("@IsOk", 1);
                    pFile.Add("@IsMain", 0);
                    pFile.Add("@Tables", "Party");
                    await hesab.ExecuteAsync(SqlInsertPartyFile, pFile, txHesab);
                }

                var pExtra = new DynamicParameters();
                pExtra.Add("@Id", Guid.NewGuid());
                pExtra.Add("@DateBirth", c.Extra.DateBirth);
                pExtra.Add("@Gender", c.Extra.Gender);
                pExtra.Add("@HistoryElectricalActivity", c.Extra.HistoryElectricalActivity);
                pExtra.Add("@ShopOperationalHistory", c.Extra.ShopOperationalHistory);
                pExtra.Add("@StoreName", c.Extra.StoreName);
                pExtra.Add("@TypeOwnership", c.Extra.TypeOwnership?.GetDescription());
                pExtra.Add("@AreaSpecialization", c.Extra.AreaSpecialization?.GetDescription());
                pExtra.Add("@ImageBusinessLicenseRef", licenseFileId);
                pExtra.Add("@PartyRef", partyId);
                await hesab.ExecuteAsync(SqlInsertExtra, pExtra, txHesab);

                await txHesab.CommitAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                return partyId;
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627 && ex.Message.Contains("UIX_DL_Title"))
            {
                await tx.RollbackAsync(cancellationToken);
                await txHesab.RollbackAsync(cancellationToken);
                throw new DomainException("این نام قبلاً برای یک طرف حساب ثبت شده است");
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627 && IsIdCollision(ex))
            {
                await tx.RollbackAsync(cancellationToken);
                await txHesab.RollbackAsync(cancellationToken);
                throw new IdGenerationCollisionException(
                    "لطفا کمی بعد دوباره تلاش کنید");
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                await txHesab.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<IReadOnlyList<string>> UpdateAsync(
            NewCustomer c, string? visitorCode, CancellationToken cancellationToken = default)
        {
            var partyId = c.PartyId ?? throw new DomainException("شناسه مشتری نامعتبر است");
            var defaultUserRef = configProvider.Get().DefaultUserRef;

            await using var sepidar = (DbConnection)sepidarFactory.CreateConnection();
            await using var hesab = (DbConnection)hesabFactory.CreateConnection();
            await sepidar.OpenAsync(cancellationToken);
            await hesab.OpenAsync(cancellationToken);

            await using var tx = await sepidar.BeginTransactionAsync(cancellationToken);
            await using var txHesab = await hesab.BeginTransactionAsync(cancellationToken);

            try
            {
                var now = DateTime.Now;

                var ctx = await sepidar.QuerySingleAsync<RegistrationContext>(
                    SqlContext, new { VisitorCode = visitorCode, DefaultUserRef = defaultUserRef }, tx);

                var party = await sepidar.QueryFirstOrDefaultAsync<PartyRow>(
                    SqlSelectPartyForUpdate, new { PartyId = partyId }, tx);
                if (party is null)
                    throw new DomainException("مشتری پیدا نشد");

                var locationRef = c.LocationRef > 0 ? c.LocationRef : configProvider.Get().DefaultLocationRef;
                var locationExists = await sepidar.ExecuteScalarAsync<int?>(
                    "SELECT 1 FROM GNR.Location WHERE LocationId = @LocationRef;",
                    new { LocationRef = locationRef }, tx);
                if (locationExists is null)
                    throw new DomainException("شهر/استان انتخاب‌شده معتبر نیست");

                var title = $"{c.LastName} {c.FirstName}".Trim();

                var pDl = new DynamicParameters();
                pDl.Add("@DLId", party.DLRef);
                pDl.Add("@Title", title);
                pDl.Add("@Title_En", title);
                pDl.Add("@LastModifier", ctx.Creator);
                pDl.Add("@LastModificationDate", now);
                await sepidar.ExecuteAsync(SqlUpdateDl, pDl, tx);

                var pParty = new DynamicParameters();
                pParty.Add("@PartyId", partyId);
                pParty.Add("@Name", c.FirstName);
                pParty.Add("@LastName", c.LastName);
                pParty.Add("@Name_En", c.FirstName);
                pParty.Add("@LastName_En", c.LastName);
                pParty.Add("@IdentificationCode", c.NationalCode);
                pParty.Add("@LastModifier", ctx.Creator);
                pParty.Add("@LastModificationDate", now);
                await sepidar.ExecuteAsync(SqlUpdateParty, pParty, tx);

                await UpdatePhone(sepidar, tx, partyId, isMain: 0, type: 1, phone: c.LandlinePhone);
                await UpdatePhone(sepidar, tx, partyId, isMain: 1, type: 2, phone: c.MobilePhone);
                await UpdatePhone(sepidar, tx, partyId, isMain: 0, type: 2, phone: c.MessengerPhone);

                var pAddr = new DynamicParameters();
                pAddr.Add("@PartyRef", partyId);
                pAddr.Add("@Address", c.Address);
                pAddr.Add("@LocationRef", locationRef);
                pAddr.Add("@ZipCode", c.ZipCode);
                pAddr.Add("@Adress_En", c.Address);
                pAddr.Add("@Latitude", c.Latitude);
                pAddr.Add("@Longitude", c.Longitude);
                await sepidar.ExecuteAsync(SqlUpdateAddress, pAddr, tx);

                var deletedFileNames = new List<string>();
                Guid? newLicenseFileId = null;
                Guid? oldLicenseFileId = null;
                if (c.LicenseFileName is not null)
                {
                    var existingFile = await hesab.QueryFirstOrDefaultAsync<ExistingFileRow>(
                        SqlSelectLicenseFile, new { PartyRef = partyId }, txHesab);

                    newLicenseFileId = Guid.NewGuid();
                    var pFile = new DynamicParameters();
                    pFile.Add("@Id", newLicenseFileId);
                    pFile.Add("@PartyRef", partyId);
                    pFile.Add("@FileName", c.LicenseFileName);
                    pFile.Add("@FileExtensions", Path.GetExtension(c.LicenseFileName));
                    pFile.Add("@IsOk", 1);
                    pFile.Add("@IsMain", 0);
                    pFile.Add("@Tables", "Party");
                    await hesab.ExecuteAsync(SqlInsertPartyFile, pFile, txHesab);

                    if (existingFile is not null)
                    {
                        oldLicenseFileId = existingFile.Id;
                        deletedFileNames.Add(existingFile.FileName);
                    }
                }

                var pExtra = new DynamicParameters();
                pExtra.Add("@PartyRef", partyId);
                pExtra.Add("@DateBirth", c.Extra.DateBirth);
                pExtra.Add("@Gender", c.Extra.Gender);
                pExtra.Add("@HistoryElectricalActivity", c.Extra.HistoryElectricalActivity);
                pExtra.Add("@ShopOperationalHistory", c.Extra.ShopOperationalHistory);
                pExtra.Add("@StoreName", c.Extra.StoreName);
                pExtra.Add("@TypeOwnership", c.Extra.TypeOwnership?.GetDescription());
                pExtra.Add("@AreaSpecialization", c.Extra.AreaSpecialization?.GetDescription());
                pExtra.Add("@NewImageBusinessLicenseRef", newLicenseFileId);
                await hesab.ExecuteAsync(SqlUpdateExtra, pExtra, txHesab);

                // ExtraPartyData.ImageBusinessLicenseRef must point elsewhere before the old file row can be deleted (FK)
                if (oldLicenseFileId is not null)
                    await hesab.ExecuteAsync(SqlDeleteLicenseFile, new { Id = oldLicenseFileId }, txHesab);

                await txHesab.CommitAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                return deletedFileNames;
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627 && ex.Message.Contains("UIX_DL_Title"))
            {
                await tx.RollbackAsync(cancellationToken);
                await txHesab.RollbackAsync(cancellationToken);
                throw new DomainException("این نام قبلاً برای یک طرف حساب ثبت شده است");
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                await txHesab.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static Task UpdatePhone(
            DbConnection con, DbTransaction tx, int partyId, int isMain, int type, string phone)
        {
            var p = new DynamicParameters();
            p.Add("@PartyRef", partyId);
            p.Add("@Type", type);
            p.Add("@IsMain", isMain);
            p.Add("@Phone", phone);
            return con.ExecuteAsync(SqlUpdatePhone, p, tx);
        }

        private static bool IsIdCollision(SqlException ex) =>
            ex.Message.Contains("PK_Party") || ex.Message.Contains("PK_DL") ||
            ex.Message.Contains("PK_PartyAddress") || ex.Message.Contains("PK_PartyPhone") ||
            ex.Message.Contains("UIX_DL_Code");

        private static Task InsertPhone(
            DbConnection con, DbTransaction tx, int id, int partyId, int isMain, int type, string phone)
        {
            var p = new DynamicParameters();
            p.Add("@PartyPhoneId", id);
            p.Add("@PartyRef", partyId);
            p.Add("@IsMain", isMain);
            p.Add("@Type", type);
            p.Add("@Phone", phone);
            p.Add("@Version", 1);
            return con.ExecuteAsync(SqlInsertPhone, p, tx);
        }

        private sealed class RegistrationContext
        {
            public int Creator { get; set; }
            public int? CustomerGroupingRef { get; set; }
        }

        private sealed class PartyRow
        {
            public int PartyId { get; set; }
            public int DLRef { get; set; }
        }

        private sealed class ExistingFileRow
        {
            public Guid Id { get; set; }
            public string FileName { get; set; } = "";
        }
    }
}