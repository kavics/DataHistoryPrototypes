using Microsoft.Extensions.Logging;
using SenseNet.Client;

namespace DataHistoryPrototype;

public class BloodPressure : Content
{
    public static readonly string ContentTypeName = nameof(BloodPressure);
    public static readonly string Ctd = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ContentType name=""{ContentTypeName}"" parentType=""GenericContent"" handler=""SenseNet.ContentRepository.GenericContent"" xmlns=""http://schemas.sensenet.com/SenseNet/ContentRepository/ContentTypeDefinition"">
  <DisplayName>{ContentTypeName}</DisplayName>
  <Icon>File</Icon>
  <Fields>
    <Field name=""Recorded"" type=""DateTime"">
      <DisplayName>Recorded</DisplayName>
    </Field>
    <Field name=""Syst"" type=""Integer"">
      <DisplayName>Syst</DisplayName>
    </Field>
    <Field name=""Dias"" type=""Integer"">
      <DisplayName>Dias</DisplayName>
    </Field>
    <Field name=""Puls"" type=""Integer"">
      <DisplayName>Puls</DisplayName>
    </Field>
  </Fields>
</ContentType>
";

    public DateTime Recorded { get; set; }
    public int Syst { get; set; }
    public int Dias { get; set; }
    public int Puls { get; set; }

    public BloodPressure(IRestCaller restCaller, ILogger<Content> logger) : base(restCaller, logger) { }
}
