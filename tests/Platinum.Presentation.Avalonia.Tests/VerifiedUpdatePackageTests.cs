using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using Platinum.Presentation.Avalonia.Updates;
using Xunit;

namespace Platinum.Presentation.Avalonia.Tests;

public sealed class VerifiedUpdatePackageTests
{
    private sealed class AssetHandler(byte[] archive,string checksum) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            HttpContent content=request.RequestUri!.AbsolutePath.EndsWith(".sha256",StringComparison.Ordinal)
                ? new StringContent(checksum)
                : new ByteArrayContent(archive);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=content});
        }
    }
    private static byte[] MakeZip(string file,string content)
    {
        using var memory=new MemoryStream();
        using(var zip=new ZipArchive(memory,ZipArchiveMode.Create,true))
        using(var writer=new StreamWriter(zip.CreateEntry(file).Open())) writer.Write(content);
        return memory.ToArray();
    }

    [Fact]
    public async Task Verified_Package_Is_Installed_Atomically()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var archive=MakeZip("app.txt","new build");
        var hash=Convert.ToHexString(SHA256.HashData(archive)).ToLowerInvariant();
        using var http=new HttpClient(new AssetHandler(archive,hash+"  Platinum.zip"));
        using var updater=new VerifiedUpdatePackageService(http);
        var root=Path.Combine(Path.GetTempPath(),"Platinum-update-"+Guid.NewGuid().ToString("N"));
        var install=Path.Combine(root,"app");
        try {
            Directory.CreateDirectory(install);
            await File.WriteAllTextAsync(Path.Combine(install,"old.txt"),"old build",cancellationToken);
            await updater.StageAndInstallAsync(new Uri("https://github.com/org/repo/releases/download/v1/Platinum.zip"),new Uri("https://github.com/org/repo/releases/download/v1/Platinum.zip.sha256"),install,cancellationToken);
            Assert.Equal("new build",await File.ReadAllTextAsync(Path.Combine(install,"app.txt"),cancellationToken));
            Assert.False(File.Exists(Path.Combine(install,"old.txt")));
            Assert.False(Directory.Exists(install+".backup"));
        } finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }

    [Fact]
    public async Task Hash_Mismatch_Leaves_Existing_Install_Untouched()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var archive=MakeZip("app.txt","untrusted");
        using var http=new HttpClient(new AssetHandler(archive,new string('0',64)+"  Platinum.zip"));
        using var updater=new VerifiedUpdatePackageService(http);
        var root=Path.Combine(Path.GetTempPath(),"Platinum-update-"+Guid.NewGuid().ToString("N"));
        var install=Path.Combine(root,"app");
        try {
            Directory.CreateDirectory(install);
            await File.WriteAllTextAsync(Path.Combine(install,"stable.txt"),"stable",cancellationToken);
            await Assert.ThrowsAsync<InvalidDataException>(()=>updater.StageAndInstallAsync(new Uri("https://github.com/org/repo/Platinum.zip"),new Uri("https://github.com/org/repo/Platinum.zip.sha256"),install,cancellationToken));
            Assert.Equal("stable",await File.ReadAllTextAsync(Path.Combine(install,"stable.txt"),cancellationToken));
        } finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }

    [Fact]
    public void Recovery_Restores_Backup_When_Promotion_Was_Interrupted()
    {
        var root=Path.Combine(Path.GetTempPath(),"Platinum-recovery-"+Guid.NewGuid().ToString("N"));
        var install=Path.Combine(root,"app");
        var backup=install+".backup";
        try {
            Directory.CreateDirectory(backup);
            File.WriteAllText(Path.Combine(backup,"stable.txt"),"rollback");
            VerifiedUpdatePackageService.Recover(install);
            Assert.Equal("rollback",File.ReadAllText(Path.Combine(install,"stable.txt")));
        } finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }
}
