using Microsoft.Extensions.Logging;
using SenseNet.Client;

namespace DataHistoryPrototype;

public class AppFolder : Content
{
    public static readonly string ContentTypeName = "ClientApplicationFolderV0_1";
    public static readonly string ContentTypePath = "/Root/System/Schema/ContentTypes/GenericContent/Folder/" + ContentTypeName;
    public static readonly string Ctd = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""{ContentTypeName}"" parentType=""Folder"" handler=""SenseNet.ContentRepository.Folder"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <DisplayName>{ContentTypeName}</DisplayName>
  <Icon>Application</Icon>
  <Fields>
    <Field name=""AppName"" type=""ShortText"">
      <DisplayName>AppName</DisplayName>
    </Field>
    <Field name=""AppInfo"" type=""ShortText"">
      <DisplayName>AppInfo</DisplayName>
    </Field>
  </Fields>
</ContentType>
";

    public AppFolder(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }

    public string? AppName { get; set; }
    public string? AppInfo { get; set; }
}