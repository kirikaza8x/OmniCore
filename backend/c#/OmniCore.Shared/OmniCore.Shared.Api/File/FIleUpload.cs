namespace OmniCore.Shared.Api.File;

using Microsoft.AspNetCore.Http;
using OmniCore.Shared.Application.Abstractions.Storage;

public sealed class FormFileUpload : IFileUpload
{
    private readonly IFormFile _file;

    public FormFileUpload(IFormFile file) => _file = file;

    public string FileName => _file.FileName;
    public string ContentType => _file.ContentType;
    public long Length => _file.Length;
    public Stream OpenReadStream() => _file.OpenReadStream();
}