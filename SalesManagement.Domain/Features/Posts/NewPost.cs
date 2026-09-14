using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;

namespace SalesManagement.Domain.Features.Posts
{
    public sealed record NewPost
    {
        public Guid? Id { get; }
        public Guid ProfileRef { get; }
        public string Title { get; }
        public string Body { get; }
        public TypePost Type { get; }
        public IReadOnlyList<string> PhotoFileNames { get; private init; } = [];

        public bool IsUpdate => Id is not null;

        private NewPost(Guid? id, Guid profileRef, string title, string body, TypePost type)
        {
            Id = id;
            ProfileRef = profileRef;
            Title = title;
            Body = body;
            Type = type;
        }

        public static NewPost Create(
            Guid? id, Guid profileRef, string? title, string? body, TypePost? type)
        {
            if (profileRef == Guid.Empty) throw new DomainException("شناسه‌ی پروفایل نامعتبر است");
            if (string.IsNullOrWhiteSpace(title)) throw new DomainException("عنوان پست را وارد کنید");
            if (string.IsNullOrWhiteSpace(body)) throw new DomainException("متن پست را وارد کنید");

            var normalizedId = id is null || id == Guid.Empty ? (Guid?)null : id;

            return new NewPost(normalizedId, profileRef, title.Trim(), body.Trim(), type ?? TypePost.Post);
        }

        public NewPost WithPhotos(IReadOnlyList<string> fileNames) => this with { PhotoFileNames = fileNames };
    }
}
