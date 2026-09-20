using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Professors;

namespace SalesManagement.Tests.Features.Professors
{
    public class NewProfessorTests
    {
        [Fact]
        public void Create_valid_trims_and_keeps_values()
        {
            var p = NewProfessor.Create(null, "  Ali ", " Rezaei ", " Math ", true);
            Assert.Equal("Ali", p.Name);
            Assert.Equal("Rezaei", p.LastName);
            Assert.Equal("Math", p.Specialization);
            Assert.True(p.IsOk);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_without_name_throws(string? name)
            => Assert.Throws<DomainException>(() => NewProfessor.Create(null, name, "x", null, false));

        [Theory]
        [InlineData(null)]
        [InlineData("  ")]
        public void Create_without_lastName_throws(string? lastName)
            => Assert.Throws<DomainException>(() => NewProfessor.Create(null, "x", lastName, null, false));

        [Fact]
        public void Create_blank_specialization_becomes_null()
            => Assert.Null(NewProfessor.Create(null, "a", "b", "  ", false).Specialization);

        [Fact]
        public void Create_without_id_is_insert()
            => Assert.False(NewProfessor.Create(null, "a", "b", null, false).IsUpdate);

        [Fact]
        public void Create_with_empty_guid_is_insert()
        {
            var p = NewProfessor.Create(Guid.Empty, "a", "b", null, false);
            Assert.False(p.IsUpdate);
            Assert.Null(p.Id);
        }

        [Fact]
        public void Create_with_id_is_update()
        {
            var id = Guid.NewGuid();
            var p = NewProfessor.Create(id, "a", "b", null, false);
            Assert.True(p.IsUpdate);
            Assert.Equal(id, p.Id);
        }

        [Fact]
        public void Create_too_long_values_throw()
        {
            Assert.Throws<DomainException>(() => NewProfessor.Create(null, new string('a', 51), "b", null, false));
            Assert.Throws<DomainException>(() => NewProfessor.Create(null, "a", new string('b', 51), null, false));
            Assert.Throws<DomainException>(() => NewProfessor.Create(null, "a", "b", new string('c', 501), false));
        }
    }
}
