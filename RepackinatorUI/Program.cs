using RepackinatorUI;

try
{
    var version = "v1.1.0 Beta";
    var application = new Application(version);
    application.Run();
}
catch (Exception ex)
{
    var now = DateTime.Now.ToString("MMddyyyyHHmmss");
    File.WriteAllText($"Crashlog-{now}.txt", ex.ToString());
}