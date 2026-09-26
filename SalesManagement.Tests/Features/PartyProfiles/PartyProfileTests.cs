using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Tests.Features.PartyProfiles
{
    public class PartyProfileTests
    {
        private static PartyProfile Make(
            int partyRef = 7823,
            string? userName = "Mahdi",
            string? aboutMe = null,
            string? address = null,
            string? geolocation = null,
            Guid? id = null)
            => PartyProfile.Create(id, partyRef, userName, aboutMe, address, geolocation);

        [Fact]
        public void No_id_is_not_explicit()
        {
            Assert.False(Make().HasExplicitId);
            Assert.Null(Make().Id);
        }

        [Fact]
        public void Empty_guid_id_is_not_explicit()
        {
            Assert.False(Make(id: Guid.Empty).HasExplicitId);
            Assert.Null(Make(id: Guid.Empty).Id);
        }

        [Fact]
        public void Real_id_is_explicit()
        {
            var id = Guid.NewGuid();
            var p = Make(id: id);
            Assert.True(p.HasExplicitId);
            Assert.Equal(id, p.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PartyRef_not_positive_throws(int partyRef)
        {
            Assert.Throws<DomainException>(() => Make(partyRef: partyRef));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_userName_throws(string? userName)
        {
            Assert.Throws<DomainException>(() => Make(userName: userName));
        }

        [Fact]
        public void UserName_is_trimmed()
        {
            Assert.Equal("Mahdi", Make(userName: "  Mahdi  ").UserName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_aboutMe_becomes_null(string? aboutMe)
        {
            Assert.Null(Make(aboutMe: aboutMe).AboutMe);
        }

        [Fact]
        public void AboutMe_is_trimmed_when_provided()
        {
            Assert.Equal("hi there", Make(aboutMe: "  hi there  ").AboutMe);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_address_becomes_null(string? address)
        {
            Assert.Null(Make(address: address).Address);
        }

        [Fact]
        public void Address_is_trimmed_when_provided()
        {
            Assert.Equal("Tehran, Valiasr", Make(address: "  Tehran, Valiasr  ").Address);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_geolocation_gives_null_lat_and_long(string? geo)
        {
            var p = Make(geolocation: geo);
            Assert.Null(p.Latitude);
            Assert.Null(p.Longitude);
        }

        [Fact]
        public void Valid_geolocation_is_parsed()
        {
            var p = Make(geolocation: "35.7,51.4");
            Assert.Equal(35.7m, p.Latitude);
            Assert.Equal(51.4m, p.Longitude);
        }

        [Fact]
        public void Geolocation_with_spaces_is_parsed()
        {
            var p = Make(geolocation: "  35.7 , 51.4  ");
            Assert.Equal(35.7m, p.Latitude);
            Assert.Equal(51.4m, p.Longitude);
        }

        [Theory]
        [InlineData("35.7")]
        [InlineData("35.7,51.4,10")]
        [InlineData("abc,51.4")]
        [InlineData("35.7,xyz")]
        [InlineData(",")]
        public void Invalid_geolocation_throws(string geo)
        {
            Assert.Throws<DomainException>(() => Make(geolocation: geo));
        }

        [Fact]
        public void Photos_default_to_null_main_and_empty_gallery()
        {
            var p = Make();
            Assert.Null(p.ProfilePhotoFileName);
            Assert.Empty(p.PhotoFileNames);
        }

        [Fact]
        public void WithProfilePhoto_sets_main_photo()
        {
            var p = Make().WithProfilePhoto("main.png");
            Assert.Equal("main.png", p.ProfilePhotoFileName);
        }

        [Fact]
        public void WithPhotos_sets_gallery()
        {
            var p = Make().WithPhotos(new[] { "a.png", "b.png" });
            Assert.Equal(new[] { "a.png", "b.png" }, p.PhotoFileNames);
        }
    }
}
