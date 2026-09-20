using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.Professors
{
    public sealed record NewProfessor
    {
        public Guid? Id { get; }
        public string Name { get; }
        public string LastName { get; }
        public string? Specialization { get; }
        public bool IsOk { get; }

        public bool IsUpdate => Id is not null;

        private NewProfessor(Guid? id, string name, string lastName, string? specialization, bool isOk)
        {
            Id = id;
            Name = name;
            LastName = lastName;
            Specialization = specialization;
            IsOk = isOk;
        }

        public static NewProfessor Create(Guid? id, string? name, string? lastName, string? specialization, bool isOk)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new DomainException("نام را وارد کنید");
            if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("نام خانوادگی را وارد کنید");

            var n = name.Trim();
            var l = lastName.Trim();
            var s = string.IsNullOrWhiteSpace(specialization) ? null : specialization.Trim();

            if (n.Length > 50) throw new DomainException("نام نباید بیشتر از ۵۰ کاراکتر باشد");
            if (l.Length > 50) throw new DomainException("نام خانوادگی نباید بیشتر از ۵۰ کاراکتر باشد");
            if (s is { Length: > 500 }) throw new DomainException("تخصص نباید بیشتر از ۵۰۰ کاراکتر باشد");

            var normalizedId = id is null || id == Guid.Empty ? (Guid?)null : id;

            return new NewProfessor(normalizedId, n, l, s, isOk);
        }
    }
}
