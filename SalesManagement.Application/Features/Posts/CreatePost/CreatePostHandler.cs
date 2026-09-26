using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.PartyProfiles;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Application.Features.Posts.CreatePost
{
    public class CreatePostHandler(
        IPostRepository postRepository,
        IPostNotificationRepository postNotificationRepository,
        IFileStorage fileStorage,
        IMediaUrlBuilder mediaUrlBuilder,
        ILogger<CreatePostHandler> logger)
    {
        private const string PostFileFolder = "PostFile";

        public async Task<CreatePostResult> Handle(CreatePostCommand cmd, CancellationToken cancellationToken = default)
        {
            var newPost = NewPost.Create(cmd.Id, cmd.ProfileRef, cmd.Title, cmd.Post, cmd.Type);

            var savedFileNames = new List<string>();
            foreach (var (content, extension) in cmd.Images)
                ProfilePhotoRules.Validate(extension, content.Length);

            foreach (var (content, extension) in cmd.Images)
            {
                var savedFileName = await fileStorage.SaveAsync(
                    content, PostFileFolder, extension ?? ".jpg", cancellationToken);
                savedFileNames.Add(savedFileName);
            }
            newPost = newPost.WithPhotos(savedFileNames);

            try
            {
                var outcome = await postRepository.CreateAsync(newPost, cancellationToken);

                foreach (var fileName in outcome.DeletedFileNames)
                    await fileStorage.DeleteAsync(PostFileFolder, fileName, CancellationToken.None);

                logger.LogInformation(
                    "پست {Mode} برای پروفایل {ProfileRef}",
                    newPost.IsUpdate ? "به‌روزرسانی شد" : "ثبت شد", cmd.ProfileRef);

                if (!newPost.IsUpdate && newPost.Type == TypePost.Urgent)
                {
                    try
                    {
                        await postNotificationRepository.NotifyProvinceOfUrgentPostAsync(
                            newPost.ProfileRef, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "ارسال نوتیفیکیشن پست فوری برای پروفایل {ProfileRef} ناموفق بود", newPost.ProfileRef);
                    }
                }

                return new CreatePostResult(
                    outcome.PostId, newPost.IsUpdate,
                    outcome.Photos.Select(p => new PostPhotoResult(p.Id, mediaUrlBuilder.Build(p.Path))).ToList());
            }
            catch
            {
                foreach (var fileName in savedFileNames)
                    await fileStorage.DeleteAsync(PostFileFolder, fileName, CancellationToken.None);
                throw;
            }
        }
    }
}
