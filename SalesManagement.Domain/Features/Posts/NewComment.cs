using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.Posts
{
    public sealed record NewComment
    {
        public Guid? Id { get; }
        public Guid PostRef { get; }
        public Guid ProfileRef { get; }
        public string Text { get; }

        public bool IsUpdate => Id is not null;

        private NewComment(Guid? id, Guid postRef, Guid profileRef, string text)
        {
            Id = id;
            PostRef = postRef;
            ProfileRef = profileRef;
            Text = text;
        }

        public static NewComment Create(Guid? id, Guid postRef, Guid profileRef, string? text)
        {
            if (postRef == Guid.Empty) throw new DomainException("شناسه‌ی پست نامعتبر است");
            if (profileRef == Guid.Empty) throw new DomainException("شناسه‌ی پروفایل نامعتبر است");
            if (string.IsNullOrWhiteSpace(text)) throw new DomainException("متن کامنت را وارد کنید");

            var normalizedId = id is null || id == Guid.Empty ? (Guid?)null : id;

            return new NewComment(normalizedId, postRef, profileRef, text.Trim());
        }
    }
}
