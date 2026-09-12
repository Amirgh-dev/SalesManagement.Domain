using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.Posts
{
    public sealed record NewPost
    {
        public Guid? Id { get; }
        public Guid ProfileRef { get; }
        public string Title { get; }
        public string Body { get; }
        public IReadOnlyList<Guid> RemovePhotoIds { get; }
        public IReadOnlyList<string> PhotoFileNames { get; private init; } = [];

        public bool IsUpdate => Id is not null;

        private NewPost(Guid? id, Guid profileRef, string title, string body, IReadOnlyList<Guid> removePhotoIds)
        {
            Id = id;
            ProfileRef = profileRef;
            Title = title;
            Body = body;
            RemovePhotoIds = removePhotoIds;
        }

        public static NewPost Create(
            Guid? id, Guid profileRef, string? title, string? body, IReadOnlyList<Guid>? removePhotoIds)
        {
            if (profileRef == Guid.Empty) throw new DomainException("شناسه‌ی پروفایل نامعتبر است");
            if (string.IsNullOrWhiteSpace(title)) throw new DomainException("عنوان پست را وارد کنید");
            if (string.IsNullOrWhiteSpace(body)) throw new DomainException("متن پست را وارد کنید");

            var normalizedId = id is null || id == Guid.Empty ? (Guid?)null : id;

            return new NewPost(
                normalizedId, profileRef, title.Trim(), body.Trim(),
                removePhotoIds?.Where(g => g != Guid.Empty).Distinct().ToList() ?? []);
        }

        public NewPost WithPhotos(IReadOnlyList<string> fileNames) => this with { PhotoFileNames = fileNames };
    }
}
