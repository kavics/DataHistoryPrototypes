using Microsoft.Extensions.Logging;
using SenseNet.Client;
using System.Text;

namespace DataHistoryPrototype;

public interface IDataHandler
{
    Task SaveDataAsync(BloodPressureData data, CancellationToken cancel);
    Task<IEnumerable<BloodPressure>> LoadHistoryAsync(CancellationToken cancel);
}

public class DataHandler : IDataHandler
{
    private readonly IRepositoryCollection _repositories;
    private IRepository? _repository;

    public DataHandler(IRepositoryCollection repositories)
    {
        _repositories = repositories;
    }


    private async Task<IRepository> GetRepositoryAsync(CancellationToken cancel)
    {
        if (_repository == null)
            _repository = await _repositories.GetRepositoryAsync(cancel);
        return _repository;
    }


    private static readonly string AppRootName = "Apps";
    private static readonly string AppRootPath = "/Root/Content/" + AppRootName;
    private static readonly string AppName = "BPR-V0_1";
    private static readonly string AppPath = $"{AppRootPath}/{AppName}";

    // ReSharper disable once InconsistentNaming
    private AppFolder? __appFolder;
    private async Task<AppFolder> GetAppFolder(CancellationToken cancel) => __appFolder ??= await InstallAppAsync(cancel);

    private async Task<AppFolder> InstallAppAsync(CancellationToken cancel)
    {
        var appFolder = await LoadOrCreateAppFolder(cancel);

        return appFolder;
    }
    private async Task<AppFolder> InstallAppAsync1(CancellationToken cancel)
    {
        var repository = await GetRepositoryAsync(cancel);
        var appFolder = await repository.LoadContentAsync<AppFolder>(AppPath, cancel);
        if (appFolder == null)
        {
            var needToInstallContentType = false;
            var needToAllowContentType = false;
            appFolder = repository.CreateContent<AppFolder>("/Root/Content", AppFolder.ContentTypeName, "BPR-V0_1");
            appFolder.AppName = "Blood Pressure Recorder";
            appFolder["AllowedChildTypes"] = new[] { "Folder", nameof(BloodPressure) };
            while (true)
            {
                try
                {
                    await appFolder.SaveAsync(cancel);
                    break;
                }
                catch (ClientException ex)
                {
                    var errorData = ex.ErrorData;
                    if (errorData != null)
                    {
                        var message = errorData.Message?.Value ?? string.Empty;
                        if (message.Contains("Unknown ContentType", StringComparison.InvariantCultureIgnoreCase))
                        {
                            needToInstallContentType = true;
                        }
                        else if (message.Contains("Cannot save the content", StringComparison.InvariantCultureIgnoreCase) &&
                            message.Contains("because its ancestor does not allow the type", StringComparison.InvariantCultureIgnoreCase))
                        {
                            needToAllowContentType = true;
                        }
                        else
                        {
                            // unknown error: exit from app.
                            throw;
                        }
                    }
                    else
                    {
                        // unknown error: exit from app.
                        throw;
                    }
                }

                if (needToInstallContentType)
                    await InstallContentTypesAsync(cancel);
                if (needToAllowContentType)
                    await AllowContentTypesAsync(cancel);
            }
        }

        return appFolder;
    }

    private async Task<AppFolder> LoadOrCreateAppFolder(CancellationToken cancel)
    {
        var repository = await GetRepositoryAsync(cancel).ConfigureAwait(false);
        var appFolder = await repository.LoadContentAsync<AppFolder>(AppPath, cancel).ConfigureAwait(false);
        if (appFolder == null)
        {
            await EnsureFolderAsync(AppRootPath, repository, cancel).ConfigureAwait(false);
            await EnsureContentTypesAsync(repository, cancel);
            await EnsureAllowedChildTypesOnRootWorkspaceAsync(repository, cancel);
            appFolder = repository.CreateContent<AppFolder>(AppRootPath, null, AppName);
            appFolder.AppName = "Blood Pressure Recorder";
            appFolder["AllowedChildTypes"] = new[] { "Folder", nameof(BloodPressure) };
            await appFolder.SaveAsync(cancel);
        }
        return appFolder;
    }

    private async Task EnsureAllowedChildTypesOnRootWorkspaceAsync(IRepository repository, CancellationToken cancel)
    {
        var result = await repository.GetResponseStringAsync(new ODataRequest(repository.Server)
        {
            Path = "/Root/Content",
            IsCollectionRequest = false,
            ActionName = "AddAllowedChildTypes",
            PostData = new {contentTypes = new[] {AppFolder.ContentTypeName}}
        }, HttpMethod.Post, cancel);
    }

    private async Task EnsureContentTypesAsync(IRepository repository, CancellationToken cancel)
    {
        await EnsureContentTypeAsync(AppFolder.ContentTypePath, AppFolder.Ctd, repository, cancel);
        await EnsureContentTypeAsync(BloodPressure.ContentTypePath, BloodPressure.Ctd, repository, cancel);
    }
    private async Task EnsureContentTypeAsync(string contentTypePath, string ctd, IRepository repository, CancellationToken cancel)
    {
        var contentType = await repository.LoadContentAsync(contentTypePath, cancel);
        if (contentType == null)
        {
            var parentPath = RepositoryPath.GetParentPath(contentTypePath);
            var name = RepositoryPath.GetFileName(contentTypePath);
            await repository.UploadAsync(
                new UploadRequest {ParentPath = parentPath, ContentType = "ContentType", ContentName = name},
                ctd, cancel);
        }
    }

    private async Task<Content> EnsureFolderAsync(string path, IRepository repository, CancellationToken cancel)
    {
        var folder = await repository.LoadContentAsync<Content>(path, cancel);
        if (folder != null)
            return folder;

        var parentPath = RepositoryPath.GetParentPath(path);
        var name = RepositoryPath.GetFileName(path);

        var parent = await EnsureFolderAsync(parentPath, repository, cancel);

        folder = repository.CreateContent(parentPath, "Folder", name);
        await folder.SaveAsync(cancel);

        return folder;
    }


    public async Task SaveDataAsync(BloodPressureData data, CancellationToken cancel)
    {
        try
        {
            var repository = await GetRepositoryAsync(cancel).ConfigureAwait(false);
            var appFolder = await GetAppFolder(cancel).ConfigureAwait(false);

            var recordName = data.Time.ToString("yyyy-MM-dd HH:mm:ss");
            var record = repository.CreateContent<BloodPressure>(appFolder.Path, null, recordName);
            record.Recorded = data.Time;
            record.Syst = data.Syst;
            record.Dias = data.Dias;
            record.Puls = data.Puls;
            await record.SaveAsync(cancel);
        }
        catch (Exception e)
        {
            // Content not found: reinstall and retry this method.
            // unknown error: exit from app.

            throw;
        }
    }

    public async Task<IEnumerable<BloodPressure>> LoadHistoryAsync(CancellationToken cancel)
    {
        var repository = await GetRepositoryAsync(cancel).ConfigureAwait(false);

        return await repository.QueryAsync<BloodPressure>(new QueryContentRequest
        {
            ContentQuery = $"+InTree:'{AppPath}' +TypeIs:'BloodPressure'",
            OrderBy = new[] {"Recorded desc"},
            Select = new[] {"Id", "Path", "Name", "Type", "Recorded", "Syst", "Dias", "Puls"}
        }, cancel);
    }

    private async Task AllowContentTypesAsync(CancellationToken cancel)
    {
        var repository = await GetRepositoryAsync(cancel);

        var body = $@"models=[{{""contentTypes"": [""{AppFolder.ContentTypeName}""]}}]";
        var result = await RESTCaller.GetResponseStringAsync(
            "/Root/Content", "AddAllowedChildTypes", HttpMethod.Post, body, repository.Server);
        Console.WriteLine(result);
    }

    private async Task InstallContentTypesAsync(CancellationToken cancel)
    {
        var repository = await GetRepositoryAsync(cancel);

        await using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(AppFolder.Ctd));
        await Content.UploadAsync("/Root/System/Schema/ContentTypes/GenericContent", AppFolder.ContentTypeName, stream1, "ContentType", server: repository.Server);

        await using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(BloodPressure.Ctd));
        await Content.UploadAsync("/Root/System/Schema/ContentTypes/GenericContent", BloodPressure.ContentTypeName, stream2, "ContentType", server: repository.Server);
    }
}